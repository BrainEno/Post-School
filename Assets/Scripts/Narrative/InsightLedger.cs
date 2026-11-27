using System;
using System.Collections.Generic;
using PostSchool.Character;

namespace PostSchool.Narrative
{
    /// <summary>
    /// Attribute growth earned during play, kept separate from the creation budget.
    ///
    /// <see cref="CharacterBuild.TryAdjust"/> spends the seven creation points and refuses to raise an
    /// attribute once they are gone --- which is correct there, but it also means that method can never
    /// reward anything, because by the time the game starts the budget is always empty. Earned growth
    /// is a different currency: it is not spent, it is accumulated, and it cannot be traded back. Keeping
    /// the two apart is what stops a scene from quietly handing the player creation points.
    ///
    /// Insight accrues per attribute. <see cref="GrantEarnedPoints"/> converts whole thresholds into
    /// points, so a scene can award one, two or three units and the character only changes when the
    /// total justifies it.
    /// </summary>
    public sealed class InsightLedger
    {
        /// <summary>Insight needed in one attribute before it becomes a point.</summary>
        public const int PointsPerInsight = 3;

        private readonly Dictionary<CoreAttribute, int> _insight = new Dictionary<CoreAttribute, int>();

        /// <summary>Raised once per attribute point actually gained, with the attribute and its new value.</summary>
        public event Action<CoreAttribute, int> PointGained;

        /// <summary>Insight accumulated in an attribute, including any not yet worth a point.</summary>
        public int InsightFor(CoreAttribute attribute)
        {
            return _insight.TryGetValue(attribute, out int value) ? value : 0;
        }

        /// <summary>Whole points sitting in the ledger, not yet converted.</summary>
        public int WholePointsFor(CoreAttribute attribute)
        {
            return InsightFor(attribute) / PointsPerInsight;
        }

        /// <summary>
        /// Adds insight. Negative amounts forfeit, and the total never drops below zero: the point of a
        /// forfeit is that the work done in a scene does not count, not that it becomes a debt.
        /// </summary>
        public void Award(CoreAttribute attribute, int insight)
        {
            int total = InsightFor(attribute) + insight;
            _insight[attribute] = Math.Max(0, total);
        }

        /// <summary>
        /// Converts whole thresholds into attribute points. Returns how many points were gained, which is
        /// fewer than the ledger holds when the attribute is already at its ceiling --- those insight
        /// units stay banked rather than disappearing, so an attribute capped today can still pay out
        /// later if the cap is ever raised.
        /// </summary>
        public int GrantEarnedPoints(CharacterBuild build)
        {
            if (build == null) throw new ArgumentNullException(nameof(build));

            int granted = 0;
            foreach (CoreAttribute attribute in (CoreAttribute[])Enum.GetValues(typeof(CoreAttribute)))
            {
                while (WholePointsFor(attribute) > 0 && build.TryRaiseEarned(attribute))
                {
                    Award(attribute, -PointsPerInsight);
                    granted++;
                    PointGained?.Invoke(attribute, build.GetValue(attribute));
                }
            }

            return granted;
        }

        public void Reset()
        {
            _insight.Clear();
        }
    }
}
