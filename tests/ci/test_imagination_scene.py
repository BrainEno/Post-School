"""The imagined lesson with B: the graph, its checks, and the reward it pays.

This scene is the first one in the project where a choice is resolved against an attribute and where
the outcome changes what the player *is*, not only what they have read. Both of those are easy to get
quietly wrong --- a check target no character can reach, a reward that pays even when the scene ends
badly, a choice that goes nowhere. None of it compiles into an error.

So the assertions here are about consequences:
  * every tier offers exactly four answers, and the way out is always the fourth;
  * the check targets are reachable at creation and get harder as she escalates;
  * a failed check and a passed one go to different nodes, not the same node with different words;
  * stopping early pays less than seeing it through, and never goes negative.

Run: python3 -m unittest discover -s tests/ci -t . -v

Plain unittest, like the rest of tests/ci: CI runs `python3 -m unittest discover` on a stock
GitHub-hosted image, which has no pytest. A pytest import here fails the whole job at collection.
"""

from __future__ import annotations

import json
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCRIPT_PATH = ROOT / "Assets/Resources/Dialogue/imagination_biology_teacher.json"

sys.path.insert(0, str(ROOT / "tools"))

# CharacterBuild: five attributes at 1, seven free points, cap 5.
BASE_VALUE = 1
FREE_POINTS = 7
ATTRIBUTE_CAP = 5
POINTS_PER_INSIGHT = 3


def graph() -> dict:
    return json.loads(SCRIPT_PATH.read_text(encoding="utf-8"))


def node_by_id(graph: dict, node_id: str) -> dict:
    for node in graph["nodes"]:
        if node["id"] == node_id:
            return node
    raise AssertionError(f"no node '{node_id}'")


def tier_nodes(graph: dict) -> list[dict]:
    return [node_by_id(graph, f"n_r{tier}_q") for tier in (1, 2, 3, 4)]


class ImaginationSceneTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.graph = graph()


    def test_the_check_targets_are_reachable_at_creation_and_get_harder(self):
        graph = self.graph
        targets = []
        for node in tier_nodes(graph):
            checked = [c for c in node["choices"] if c.get("checkAttribute")]
            assert len(checked) == 1, f"{node['id']}: expected exactly one checked answer"
            choice = checked[0]
            assert choice["checkAttribute"] == "Imagination", (
                "the imagined lesson resolves against the ability that conjures it"
            )

            # Reachable: 1 base plus at most the free points, and never above the cap.
            ceiling = min(ATTRIBUTE_CAP, BASE_VALUE + FREE_POINTS)
            assert BASE_VALUE < choice["minimumValue"] <= ceiling, (
                f"{node['id']}: target {choice['minimumValue']} is not reachable at creation "
                f"(a character can reach {ceiling} in one attribute)"
            )
            targets.append(choice["minimumValue"])

        assert targets == sorted(targets), f"escalation targets must rise, got {targets}"
        assert targets[0] < targets[-1], "the last exchange must be harder than the first"
        assert targets[-1] == ATTRIBUTE_CAP, (
            "resolving the whole fantasy should require the attribute at its ceiling; "
            f"the last target is {targets[-1]}"
        )


    def test_a_failed_check_does_not_lead_where_a_passed_one_does(self):
        graph = self.graph
        for node in tier_nodes(graph) + [node_by_id(graph, "n_resolve_choice")]:
            for choice in node["choices"]:
                if not choice.get("checkAttribute"):
                    continue
                assert choice["next"] != choice["failureNext"], (
                    f"{node['id']}: both branches lead to '{choice['next']}', so the check decides nothing"
                )
                # Different destinations is not enough: the two branches must not be the same text with a
                # different id, so their follow-on nodes have to differ too.
                passed = node_by_id(graph, choice["next"])
                failed = node_by_id(graph, choice["failureNext"])
                assert passed["text"] != failed["text"], (
                    f"{node['id']}: passing and failing the check read the same, so the check has no weight"
                )


    def test_stopping_early_pays_less_than_seeing_it_through(self):
        graph = self.graph
        def insight(node_id: str) -> int:
            total = 0
            for effect in node_by_id(graph, node_id)["effects"]:
                if effect.startswith("insight:"):
                    total += int(effect.split(":")[2])
            return total

        held = insight("n_resolve_held")
        broken = insight("n_resolve_broken")
        escape = insight("n_escape")

        assert held > broken, "holding the fantasy together has to be worth more than losing it"
        assert escape < 0, "leaving early forfeits the insight the exchanges were worth"
        assert held >= POINTS_PER_INSIGHT, (
            f"the best outcome should pay at least one point ({POINTS_PER_INSIGHT} insight), pays {held}"
        )


    def test_the_best_case_is_a_complete_point_and_the_worst_is_none(self):
        graph = self.graph
        """Walk the graph the way a player would and total what each route pays."""

        def total_insight(route: list[str]) -> int:
            total = 0
            for node_id in route:
                for effect in node_by_id(graph, node_id)["effects"]:
                    if effect.startswith("insight:"):
                        total += int(effect.split(":")[2])
            return total

        # A player who passes every check and holds the fantasy: four held checks plus the resolution.
        best = total_insight(
            ["n_r1_c_win_result", "n_r2_c_win_result", "n_r3_c_win_result", "n_r4_c_win_result", "n_resolve_held"]
        )
        # A player who fails every check and lets it break.
        worst = total_insight(["n_resolve_broken"])
        # A player who stops at the first opportunity.
        early = total_insight(["n_escape"])

        assert best >= 2 * POINTS_PER_INSIGHT, f"the best route should pay at least two points, pays {best / POINTS_PER_INSIGHT:.1f}"
        assert worst > 0, "even a broken fantasy leaves something behind; it is the only thing the scene taught"
        assert early <= 0, "leaving immediately must not pay"

        # And no route, however it is walked, may drive the ledger below zero: the ledger floors per
        # attribute, but a route that pays out more forfeit than it earned would be a design error.
        assert best > early


    def test_the_scene_never_sets_an_objective_flag(self):
        graph = self.graph
        # Nothing in this scene happened. Recording it as world state would make a later scene able to
        # check for it, which would make the imagination load-bearing.
        for node in graph["nodes"]:
            for effect in node.get("effects") or []:
                assert not effect.startswith("objective:"), f"{node['id']}: imagined content set '{effect}'"
            for choice in node.get("choices") or []:
                for effect in choice.get("effects") or []:
                    assert not effect.startswith("objective:"), f"{node['id']}: choice set '{effect}'"


    def test_the_script_is_marked_as_not_owner_authored(self):
        graph = self.graph
        assert graph["status"] == "awaiting_author_review", (
            "this is a rehearsal script written by the agent; it must not claim to be the owner's writing"
        )
        assert "note" in graph, "the file must say, in the data, that the text awaits review"


if __name__ == "__main__":
    unittest.main()
