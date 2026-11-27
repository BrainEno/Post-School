namespace PostSchool.Prototype
{
    /// <summary>
    /// Which of the three conversations the classroom is currently playing. The prologue is always
    /// first; the two N scenes are chosen afterwards from the story state, so this enum is what keeps
    /// the presenter, the runner and the handover bookkeeping pointed at the same script.
    /// </summary>
    public enum QuizScene
    {
        /// <summary>The waking prologue, with Q. Played once, before anything is remembered.</summary>
        ClassroomOpening,

        /// <summary>N's first class, once the prologue has ended.</summary>
        TeacherFirstClass,

        /// <summary>Coming back to N after the first class has been heard.</summary>
        TeacherRevisit,
    }
}
