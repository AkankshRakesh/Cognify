using Cognify.Core.Entities;
using Cognify.Core.Enums;
using Cognify.Core.Interfaces;

namespace Cognify.Services
{
    // Rewards calibrated confidence: correct+confident scores higher,
    // wrong+confident is penalized harder. Multipliers are configurable
    // (here as constants, but intended to be read from an ExamConfig table
    // or appsettings.json in a full build so teachers can tune per exam).
    public class ConfidenceWeightedScoringService : IScoringService
    {
        private static readonly Dictionary<(bool isCorrect, ConfidenceLevel confidence), double> Multipliers = new()
        {
            { (true,  Core.Enums.ConfidenceLevel.High),   1.5 },
            { (true,  Core.Enums.ConfidenceLevel.Medium), 1.0 },
            { (true,  Core.Enums.ConfidenceLevel.Low),    0.75 },
            { (false, Core.Enums.ConfidenceLevel.High),  -1.0 },
            { (false, Core.Enums.ConfidenceLevel.Medium),-0.5 },
            { (false, Core.Enums.ConfidenceLevel.Low),   -0.25 },
        };

        public double CalculateMarks(Question question, AttemptAnswer answer, bool negativeMarking)
        {
            if (string.IsNullOrEmpty(answer.SelectedOption))
                return 0; // unattempted - no confidence was given either

            if (!answer.IsCorrect && !negativeMarking)
                return 0;

            var confidence = answer.ConfidenceLevel ?? Core.Enums.ConfidenceLevel.Medium;
            var multiplier = Multipliers[(answer.IsCorrect, confidence)];

            return Math.Min(question.Marks, Math.Round(question.Marks * multiplier, 2));
        }
    }
}
