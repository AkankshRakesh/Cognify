using Cognify.Core.Entities;
using Cognify.Core.Interfaces;

namespace Cognify.Services
{
    // Plain marks-based scoring: full marks for correct, optional negative marking for wrong.
    public class StandardScoringService : IScoringService
    {
        public double CalculateMarks(Question question, AttemptAnswer answer, bool negativeMarking)
        {
            if (string.IsNullOrEmpty(answer.SelectedOption))
                return 0; // unattempted

            if (answer.IsCorrect)
                return question.Marks;

            return negativeMarking ? -0.25 * question.Marks : 0;
        }
    }
}
