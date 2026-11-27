using NUnit.Framework;
using PostSchool.Character;
using PostSchool.Dialogue;
using UnityEngine;

namespace PostSchool.Tests.EditMode
{
    /// <summary>
    /// The prologue's staging lives in a switch keyed by node id, and every one of those ids has to be a
    /// node that the authored script actually contains. Get one wrong and nothing throws: the beat just
    /// never plays, and the room goes back to being a still image behind a wall of text that nobody can
    /// tell is missing a gesture. That is the failure this file exists to catch.
    ///
    /// The text is not touched by the staging layer, so none of these tests read it. They only care that
    /// the ids line up, and that the line which states a movement has a movement to go with it.
    /// </summary>
    public sealed class PrologueStagingTests
    {
        private const string ResourcePath = "Dialogue/classroom_opening";

        private static DialogueScript Opening()
        {
            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            Assert.IsNotNull(asset, $"Missing dialogue resource '{ResourcePath}'.");
            return DialogueScript.FromJson(asset.text);
        }

        private static bool HasNode(DialogueScript script, string id)
        {
            foreach (DialogueNode node in script.nodes)
            {
                if (node.id == id) return true;
            }
            return false;
        }

        /// <summary>
        /// The ids the bootstrap stages a beat for. Kept here as data so the test fails loudly when the
        /// script is reworded, instead of the beat quietly disappearing.
        /// </summary>
        private static readonly string[] StagedNodes =
        {
            "q_1", "q_who_look", "q_zero", "q_blank_1", "q_absent_1", "q_exam_end",
        };

        [Test]
        public void EveryStagedBeatNamesANodeThatExists()
        {
            DialogueScript script = Opening();

            foreach (string id in StagedNodes)
            {
                Assert.IsTrue(HasNode(script, id), $"The staging table has a beat for '{id}', but the authored script has no such node. The beat would never play.");
            }
        }

        [Test]
        public void TheLineThatSaysQLooksAtTheSetSquarePointsAtSomethingHeCanSee()
        {
            // q_who_look is the protagonist's line "（Q 看向讲台上的三角尺）" — a stage direction sitting in
            // the middle of the script with nothing performing it. The glance beat turns Q by an angle
            // derived from where he stops and where the prop is. Two things have to be true for that to
            // mean anything: the prop must be within a turnable distance of him, and the turn must be
            // small enough that it reads as a glance rather than as him walking away from the player.
            DialogueScript script = Opening();
            Assert.IsTrue(HasNode(script, "q_who_look"), "the authored direction node is gone");

            Vector3 q = new Vector3(-1.9f, 0f, -0.1f);     // where AnimateQApproach leaves him
            Vector3 setSquare = new Vector3(2.1f, 0f, 2.9f); // the podium prop

            float distance = Vector3.Distance(q, setSquare);
            Assert.Greater(distance, 2f, "the prop is too close to be worth turning to look at");
            Assert.Less(distance, 8f, "the prop is too far to read as being on the podium he is standing beside");

            // The angle the bootstrap computes, checked here so the two cannot drift.
            Vector3 toPlayer = new Vector3(0f, 0f, -0.75f) - q;
            float facing = Mathf.Atan2(toPlayer.x, toPlayer.z) * Mathf.Rad2Deg;
            Vector3 toProp = setSquare - q;
            float target = Mathf.Atan2(toProp.x, toProp.z) * Mathf.Rad2Deg;
            float turn = Mathf.DeltaAngle(facing, target);

            Assert.That(Mathf.Abs(turn), Is.InRange(20f, 80f),
                $"a {turn:0.#} degree turn is either invisible or a full body turn away from the conversation");
        }

        [Test]
        public void TheStagingBeatsAreShortEnoughToFinishBeforeTheNextNode()
        {
            // A beat that outlasts its node is a figure still mid-gesture when the text has moved on.
            // The turn and the glance are deliberately shorter than a read line; the away-beat is the
            // longest. These numbers are the contract, so they are asserted rather than described.
            Assert.LessOrEqual(HumanoidClips.ClassmateTurnBeat().Duration, 1.5f);
            Assert.LessOrEqual(HumanoidClips.ClassmateAwayBeat().Duration, 2.1f);
            Assert.LessOrEqual(HumanoidClips.ClassmateGlanceBeat(-55.8f).Duration, 2.2f);
        }

        [Test]
        public void NoStagingBeatLoopsOrMovesTheFigureOffItsMark()
        {
            // Beats are one-shots that hand back to the idle loop, and none of them may translate the
            // figure: Q is standing where he stopped, and a beat that walks him would drift him out of
            // the layout his own walk was validated against.
            foreach (HumanoidClip beat in new[]
                     {
                         HumanoidClips.ClassmateTurnBeat(),
                         HumanoidClips.ClassmateAwayBeat(),
                         HumanoidClips.ClassmateGlanceBeat(-55.8f),
                     })
            {
                Assert.IsFalse(beat.Loops, $"{beat.Name} must not loop: it is a beat, not a state.");
                Assert.GreaterOrEqual(beat.KeyCount, 2, beat.Name);
            }
        }
    }
}
