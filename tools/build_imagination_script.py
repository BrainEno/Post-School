#!/usr/bin/env python3
"""Generates the imagination script for B, the biology teacher.

Why a generator and not a hand-written JSON: the graph is 60-odd nodes with four tiers of choices, and
the properties that must hold are relationships between nodes, not facts about any one node --- every
choice has to land somewhere, every imagined tier has to end, and the escape route has to exist from
every tier at the same index. Hand-writing that and then checking it is two files to keep in sync. This
writes the graph and asserts the properties over it in one place.

The text here is a rehearsal script, not the owner's writing. It is stamped
`status: awaiting_author_review` and every node that will need real prose says so in `note`, exactly
the way the placeholder convention in AGENTS.md asks for.

Run: python3 tools/build_imagination_script.py
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/Resources/Dialogue/imagination_biology_teacher.json"

SCENE_ID = "imagination_biology_teacher"
STATUS = "awaiting_author_review"

# The five attributes, and the one this scene resolves against. Imagination is the ability that
# conjures B and the only one that can make the figure stop believing it is in a classroom.
ATTRIBUTE = "Imagination"

# Targets are on CharacterBuild.BaseValue..AttributeCap (1..5). A character can reach 5 in imagination
# at creation (1 base + 4 of 7 free points), so the decisive check has to be reachable but not free.
# Holding four tiers of the fantasy together against her requires the cap; the tier-by-tier checks are
# lower so a mid-imagination character still wins some exchanges.
TIER_TARGETS = {1: 2, 2: 3, 3: 4, 4: 5}

NOT_AUTHORED = "rehearsal text: not the owner's writing, needs a pass"


def node(node_id, kind, presentation, speaker, text, *, next=None, choices=None, effects=None, note=None):
    entry = {
        "id": node_id,
        "kind": kind,
        "presentation": presentation,
        "speaker": speaker,
        "text": text,
    }
    if next is not None:
        entry["next"] = next
    if choices is not None:
        entry["choices"] = choices
    entry["effects"] = effects or []
    if note:
        entry["note"] = note
    return entry


def choice(text, next_id, *, effects=None, check=None, minimum=0, failure=None):
    entry = {"text": text, "next": next_id, "effects": effects or []}
    if check:
        entry["checkAttribute"] = check
        entry["minimumValue"] = minimum
        entry["failureNext"] = failure
    return entry


def tier_chains():
    """Four tiers of the imagined lesson, each with four answers that land the same way.

    A = the student's answer: she is on solid ground, and she presses harder each tier.
    B = the adult's answer: she refuses the premise, and the refusal costs the protagonist the only
        position that had any leverage in it.
    C = the absurd answer: a check, resolved against imagination. Passing unsettles her; failing
        angers her, and the two are different failures, not one failure with two labels.
    D = stop imagining: available from every tier at the same index, and the only exit that is not a
        verdict from her.
    """
    return {
        1: dict(
            q_text="课本。你带了没有？",
            q_note="opening demand of the imagined lesson",
            a_text="……没带。我忘在家里了。",
            a_result="那你现在用眼睛看什么？看我？我脸上印着字吗。站到前面来。",
            b_text="我不需要带。我是成年人。",
            b_result="成年人。成年人坐在这张桌子后面，听我讲细胞膜。坐下。",
            c_text="课本在梦里，老师。梦里的书包是空的，这很合理。",
            c_win="……你说什么？",
            c_win_result="她把眼镜往鼻梁上推了一格，没有立刻接话。这间教室有东西松了。",
            c_lose="你跟我耍嘴皮子？",
            c_lose_result="她把三角尺从讲台上拿起来，在指间转了一圈。她知道自己在这里，她也知道你知道。",
        ),
        2: dict(
            q_text="站到前面来。让全班看看没带课本的人长什么样。",
            q_note="tier 2: she escalates, having been given ground in tier 1",
            a_text="……好。",
            a_result="转身。手举起来。告诉他们，你为什么不带课本。",
            b_text="我不会站到前面去。我不是这个班的学生。",
            b_result="你不是这个班的学生。那你为什么坐在这里，为什么穿着这件衣服，为什么怕我？",
            c_text="老师，如果我站到前面去，全班就得看着我——可这个班里没有人，老师。",
            c_win="……有人。一直有人。",
            c_win_result="她的声音在\"有人\"两个字上裂了一下。她把视线移开，看窗户。",
            c_lose="闭嘴。把手伸出来。",
            c_lose_result="她不再试图讲道理了。从现在开始，她只想证明自己还站在讲台后面。",
        ),
        3: dict(
            q_text="把手伸出来。",
            q_note="tier 3: the ruler, which the opening already told the player to expect",
            a_text="（我伸出手。）",
            a_result="打完之后她没有让我回去。她等着看我哭，或者等着看我装作不疼。我两样都没给她。",
            b_text="你可以打。打完了我也不会变成你的学生。",
            b_result="那我就打到你觉得你是为止。",
            c_text="老师，你打的是梦。疼会醒的。",
            c_win="……那你要不要试试看它会不会醒？",
            c_win_result="她笑了。这是她第一次笑，而这一笑让她整张脸像是画在纸上的。",
            c_lose="（她的手落下来。）你连疼都不肯承认，你还敢跟我讲故事。",
            c_lose_result="她打得很准。这说明她在这里比在教室里更清楚自己是谁。",
        ),
        4: dict(
            q_text="……你还不肯认。",
            q_note="tier 4: the last chance to resolve it either way",
            a_text="我认。我没带课本，我该罚。",
            a_result="她愣住了。她准备了一整套对付不肯认的人的词，没有一套是给认了的。她让我回座位。",
            b_text="我不是你的学生。你也不是我的老师。",
            b_result="（她张了张嘴。什么都没说出来。）讲台后面那个人，忽然不知道自己该把手放在哪里。",
            c_text="老师——如果我承认我是你的学生，你能不能承认你不在了？",
            c_win="（她把眼镜摘下来。没有眼镜，她的脸几乎看不清了。）",
            c_win_result="整间教室开始退色，像一张被水泡过的纸。她还在讲台上，但她已经不在讲什么了。",
            c_lose="你不许这么说。你不许。",
            c_lose_result="这句话把她彻底激怒了，也把她彻底暴露了：一个不在了的人，不该这么在意别人说不说。",
        ),
    }


def build():
    nodes = []

    # ---------------------------------------------------------------- entry, from Q's description
    # Q describes her. This is the "回忆起源" the brief asks for: the imagining has to be visibly built
    # out of what he says, piece by piece, in his voice --- so the player watches a figure assemble from
    # a classmate's throwaway description. Everything here is rehearsal text.
    nodes.append(node(
        "n_q_1", "line", "speech", "q",
        "B老师啊？你没见过她？就那个小老太太，个子还没我高。",
        next="n_q_2", note=NOT_AUTHORED,
    ))
    nodes.append(node(
        "n_q_2", "line", "speech", "q",
        "背总是驼着的，肩膀倒是耸得老高，走路的时候脖子往前探，像只站在电线上的鸟。",
        next="n_q_3", note=NOT_AUTHORED,
    ))
    nodes.append(node(
        "n_q_3", "line", "speech", "q",
        "鼻子是勾的，你从侧面看就知道了，尖得能挂东西。眼睛小，还挨得近，戴那种带帽檐的眼镜，帽檐一翻下来，你根本看不见她的眼睛。",
        next="n_q_4", note=NOT_AUTHORED,
    ))
    nodes.append(node(
        "n_q_4", "line", "speech", "q",
        "白头发的，全梳到后面盘成一个团，用一根发卡别着。她讲课的时候那个团就在后面一晃一晃的。",
        next="n_enter", note=NOT_AUTHORED,
    ))

    # The imagining starts. The narration is the protagonist building her out of those four pieces, in
    # order, and finishing on the one thing Q did not describe --- which is what the imagination fills in
    # on its own, and the reason the figure is wrong.
    nodes.append(node(
        "n_enter", "line", "narration", "",
        "Q还在说话。我一边听，一边在脑子里把那间教室盖了起来。",
        next="n_b_desc_a", note=NOT_AUTHORED,
    ))
    nodes.append(node(
        "n_b_desc_a", "line", "imagination", "protagonist",
        "个子不高。驼着。肩膀耸着。",
        next="n_b_desc_b", note=NOT_AUTHORED,
    ))
    nodes.append(node(
        "n_b_desc_b", "line", "imagination", "protagonist",
        "勾鼻子。小眼睛。带帽檐的眼镜，白头发在脑后盘成一个团。",
        next="n_b_desc_c", note=NOT_AUTHORED,
    ))
    nodes.append(node(
        "n_b_desc_c", "line", "imagination", "protagonist",
        "她站在讲台后面，比讲台高不了多少。",
        next="n_b_desc_d", note=NOT_AUTHORED,
    ))
    nodes.append(node(
        "n_b_desc_d", "line", "imagination", "protagonist",
        "Q没有说她的脸长什么样。所以我想象出来的那张脸，是我自己填的。",
        next="n_r1_q", note=NOT_AUTHORED,
    ))

    # ---------------------------------------------------------------- the four tiers
    chains = tier_chains()
    for tier in (1, 2, 3, 4):
        spec = chains[tier]
        target = TIER_TARGETS[tier]

        # Her demand, then the player's four answers.
        nodes.append(node(
            f"n_r{tier}_q", "choice", "imagination", "teacher_b", spec["q_text"],
            choices=[
                choice(spec["a_text"], f"n_r{tier}_a"),
                choice(spec["b_text"], f"n_r{tier}_b"),
                choice(spec["c_text"], f"n_r{tier}_c_win",
                       check=ATTRIBUTE, minimum=target, failure=f"n_r{tier}_c_lose"),
                choice("（不再想下去了。）", "n_escape", effects=["recollection:b_escape"]),
            ],
            note=spec["q_note"],
        ))

        # A: she is right, and being right makes her worse.
        nodes.append(node(
            f"n_r{tier}_a", "line", "imagination", "teacher_b", spec["a_result"],
            next=f"n_r{tier}_a_next", effects=[f"recollection:b_tier{tier}_student"], note=NOT_AUTHORED,
        ))
        nodes.append(node(
            f"n_r{tier}_a_next", "line", "narration", "",
            "（她按住了我。）",
            next=_next_tier(tier), note=NOT_AUTHORED,
        ))

        # B: refusing the premise leaks that she is not sure of it either.
        nodes.append(node(
            f"n_r{tier}_b", "line", "imagination", "teacher_b", spec["b_result"],
            next=_next_tier(tier), effects=[f"recollection:b_tier{tier}_adult"], note=NOT_AUTHORED,
        ))

        # C: two different failures, because passing and failing a check must not read the same.
        nodes.append(node(
            f"n_r{tier}_c_win", "line", "imagination", "teacher_b", spec["c_win"],
            next=f"n_r{tier}_c_win_result", note=NOT_AUTHORED,
        ))
        nodes.append(node(
            f"n_r{tier}_c_win_result", "line", "narration", "",
            spec["c_win_result"],
            next=_next_tier(tier),
            effects=[f"recollection:b_tier{tier}_absurd_held", "insight:Imagination:1"],
            note=NOT_AUTHORED,
        ))
        nodes.append(node(
            f"n_r{tier}_c_lose", "line", "imagination", "teacher_b", spec["c_lose"],
            next=f"n_r{tier}_c_lose_result", note=NOT_AUTHORED,
        ))
        nodes.append(node(
            f"n_r{tier}_c_lose_result", "line", "narration", "",
            spec["c_lose_result"],
            next=_next_tier(tier), effects=[f"recollection:b_tier{tier}_absurd_failed"], note=NOT_AUTHORED,
        ))

    # ---------------------------------------------------------------- which ending
    # The ending is the last check the scene makes, and it is made on the answer that was actually
    # given rather than on a tally: holding the whole fantasy together is an imagination check at the
    # cap, offered at the one moment the player has had every chance to build up to it.
    nodes.append(node(
        "n_resolve_route", "line", "narration", "",
        "她抬起手。我知道她要说什么了，而且我知道，是她自己也不知道的部分在替她说。",
        next="n_resolve_choice", note=NOT_AUTHORED,
    ))
    nodes.append(node(
        "n_resolve_choice", "choice", "imagination", "protagonist",
        "（这是我能记得这是我编的最后一次机会。）",
        choices=[
            choice("（我记得。这是我的教室。）", "n_resolve_held",
                   check=ATTRIBUTE, minimum=TIER_TARGETS[4], failure="n_resolve_broken"),
            choice("（我等着她说下去。）", "n_resolve_broken"),
        ],
        note=NOT_AUTHORED,
    ))

    # ---------------------------------------------------------------- the two endings of the lesson
    # Insight is the reward currency, not points: three units make a point, so the numbers below are
    # "how much of a point this outcome is worth". Holding the whole fantasy together is worth a full
    # point plus change; breaking it is worth something, but less, and stopping early forfeits most of
    # the tier rewards already booked.
    #
    # Held every exchange: the fantasy gave way on its own, which is the only good outcome.
    nodes.append(node(
        "n_resolve_held", "line", "imagination", "protagonist",
        "她还在讲台上。但教室已经散了——桌子、黑板、窗外的树，全都在往后退，像有人把镜头拉远了。",
        next="n_resolve_held_2",
        effects=["recollection:b_held_together", "insight:Imagination:4", "insight:Perception:1"],
        note=NOT_AUTHORED,
    ))
    nodes.append(node(
        "n_resolve_held_2", "line", "imagination", "protagonist",
        "我没有赢。我只是从头到尾都记得，这是我编的。",
        next="n_return", note=NOT_AUTHORED,
    ))

    # Lost her temper at some point: she keeps the room, and the room keeps me.
    nodes.append(node(
        "n_resolve_broken", "line", "imagination", "teacher_b",
        "坐下。",
        next="n_resolve_broken_2", effects=["recollection:b_broken", "insight:Imagination:1"],
        note=NOT_AUTHORED,
    ))
    nodes.append(node(
        "n_resolve_broken_2", "line", "narration", "",
        "（我坐下了。）我想不起来我是什么时候开始坐下的，也想不起来我刚才本来要做什么。",
        next="n_return", note=NOT_AUTHORED,
    ))

    # ---------------------------------------------------------------- the escape
    nodes.append(node(
        "n_escape", "line", "narration", "",
        "我不想下去了。这个念头一起，教室就没了——不是散掉，是像没被画完一样，从边上开始不见了。",
        next="n_escape_2",
        # Leaving early forfeits the insight the exchanges were worth. It does not become a debt: the
        # ledger floors at zero, so a player who leaves immediately simply gains nothing.
        effects=["recollection:b_chose_to_stop", "insight:Imagination:-3"],
        note=NOT_AUTHORED,
    ))
    nodes.append(node(
        "n_escape_2", "line", "narration", "",
        "（我还没来得及知道她要说的是什么。）",
        next="n_return", note=NOT_AUTHORED,
    ))

    # ---------------------------------------------------------------- back to the room
    nodes.append(node(
        "n_return", "line", "narration", "",
        "Q还在说。他的嘴一张一合，声音过了半拍才跟上来。",
        next="n_end", effects=["recollection:b_imagining_done"], note=NOT_AUTHORED,
    ))
    nodes.append(node("n_end", "end", "", "", ""))

    _check(nodes, chains)
    return {
        "id": SCENE_ID,
        "status": STATUS,
        "startNode": "n_q_1",
        "note": (
            "B老师的想象场景。全部台词为排演稿，非作者原文，待作者逐句审定后替换。"
            "开头四句是 Q 口述 B 的外形（回忆起源），随后是主角据此构图的四句独白，再进入四轮对峙。"
            "四轮对话，每轮四种回应：A 学生式顺从 / B 声称成年 / C 荒诞调侃（对想象属性做检定）"
            " / D 跳出想象。检定目标 2/3/4/5，属性上限为 5。"
            "奖励走 insight: 前缀（3 点 = 1 属性点），不设 objective 标记。"
        ),
        "nodes": nodes,
    }


def _next_tier(tier):
    """Each tier's ordinary exits go to the next tier; only the last picks an ending."""
    if tier < 4:
        return f"n_r{tier + 1}_q"
    # Tier four is the last chance: whether the fantasy holds or breaks is decided by which answer
    # carried it, so the ending is chosen from the effects recorded along the way.
    return "n_resolve_route"


def _check(nodes, chains):
    """The properties that make the graph playable, asserted at generation time."""
    ids = [n["id"] for n in nodes]
    assert len(ids) == len(set(ids)), "duplicate node id"

    known = set(ids)
    for n in nodes:
        if n["kind"] == "line":
            assert n.get("next") in known, f"{n['id']}: next '{n.get('next')}' does not exist"
        if n["kind"] == "choice":
            expected = 2 if n["id"] == "n_resolve_choice" else 4
            assert expected == len(n["choices"]), f"{n['id']}: expected {expected} answers, got {len(n['choices'])}"
            for c in n["choices"]:
                assert c["next"] in known, f"{n['id']}: choice next '{c['next']}' does not exist"
                if "checkAttribute" in c:
                    assert c["failureNext"] in known, f"{n['id']}: failureNext missing"
        for e in n["effects"]:
            # An imagined scene records what the protagonist imagined and what it was worth. It must
            # never set an objective flag: nothing in here happened.
            assert e.startswith("recollection:") or e.startswith("insight:"), \
                f"{n['id']}: an imagined scene may not set objective flags (found '{e}')"

    # The escape is the fourth answer in every tier, at the same index, so a player who wants out
    # does not have to hunt for it.
    for tier in (1, 2, 3, 4):
        n = next(x for x in nodes if x["id"] == f"n_r{tier}_q")
        assert n["choices"][3]["next"] == "n_escape", f"tier {tier}: escape is not the fourth answer"
    # And the exit is always at index 3, which is what the prologue's staging and any future UI assume.
    assert all(len([x for x in nodes if x["id"] == f"n_r{t}_q"][0]["choices"]) == 4 for t in (1, 2, 3, 4))

    # No node is orphaned and nothing loops.
    reachable = set()
    stack = ["n_q_1"]
    while stack:
        current = stack.pop()
        if current in reachable:
            continue
        reachable.add(current)
        node_obj = next(x for x in nodes if x["id"] == current)
        if node_obj["kind"] == "line":
            stack.append(node_obj["next"])
        elif node_obj["kind"] == "choice":
            for c in node_obj["choices"]:
                stack.append(c["next"])
                if "failureNext" in c:
                    stack.append(c["failureNext"])
    unreachable = known - reachable
    assert not unreachable, f"unreachable nodes: {sorted(unreachable)}"

    # Check targets must be reachable and must rise.
    targets = [TIER_TARGETS[t] for t in sorted(TIER_TARGETS)]
    assert targets == sorted(targets), "check targets must not get easier as the scene escalates"


def main() -> int:
    data = build()
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"wrote {OUT.relative_to(ROOT)}: {len(data['nodes'])} nodes")
    return 0


if __name__ == "__main__":
    sys.exit(main())
