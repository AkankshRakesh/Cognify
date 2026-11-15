using System;

namespace Cognify.Core.Enums
{
    // Flags enum so an exam can be: None, ConfidenceWeighted only,
    // Adaptive only, or Both together.
    [Flags]
    public enum ScoringMode
    {
        Standard = 0,
        ConfidenceWeighted = 1,
        Adaptive = 2,
        Both = ConfidenceWeighted | Adaptive
    }

    public enum AttemptStatus
    {
        InProgress = 0,
        Submitted = 1,
        AutoSubmitted = 2
    }

    public enum UserRole
    {
        Teacher = 1,
        Student = 2
    }
}
