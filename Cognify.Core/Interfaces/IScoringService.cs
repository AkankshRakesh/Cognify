using Cognify.Core.Entities;

namespace Cognify.Core.Interfaces
{
    // Strategy pattern: resolved at runtime based on Exam.ScoringMode
    // so Standard / ConfidenceWeighted scoring can be swapped without
    // branching logic in the controller.
    public interface IScoringService
    {
        double CalculateMarks(Question question, AttemptAnswer answer, bool negativeMarking);
    }
}
