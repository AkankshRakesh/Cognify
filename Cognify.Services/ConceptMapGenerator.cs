using Cognify.Core.Entities;
using Cognify.Core.Enums;
using Cognify.Core.Interfaces;
using Cognify.Data.Repositories;

namespace Cognify.Services
{
    public class ConceptMapGenerator : IConceptAnalyzer
    {
        private readonly AttemptRepository _attemptRepository;

        public ConceptMapGenerator(AttemptRepository attemptRepository)
        {
            _attemptRepository = attemptRepository;
        }

        public async Task<IEnumerable<ConceptPerformance>> BuildConceptMapAsync(int attemptId)
        {
            var attempt = await _attemptRepository.GetByIdAsync(attemptId);
            if (attempt == null) return Enumerable.Empty<ConceptPerformance>();

            var grouped = attempt.Answers
                .Where(a => a.Question != null)
                .GroupBy(a => a.Question!.ConceptTagId)
                .Select(g => new ConceptPerformance
                {
                    AttemptId = attemptId,
                    ConceptTagId = g.Key,
                    TotalQuestions = g.Count(),
                    CorrectCount = g.Count(a => a.IsCorrect)
                });

            return grouped;
        }

        // Overconfidence: % of questions answered wrong despite High confidence
        // Underconfidence: % of questions answered correctly despite Low confidence
        public async Task<(double overconfidence, double underconfidence)> CalculateCalibrationAsync(int attemptId)
        {
            var attempt = await _attemptRepository.GetByIdAsync(attemptId);
            if (attempt == null || attempt.Answers.Count == 0) return (0, 0);

            var answered = attempt.Answers.Where(a => a.ConfidenceLevel.HasValue).ToList();
            if (answered.Count == 0) return (0, 0);

            var overconfident = answered.Count(a => !a.IsCorrect && a.ConfidenceLevel == ConfidenceLevel.High);
            var underconfident = answered.Count(a => a.IsCorrect && a.ConfidenceLevel == ConfidenceLevel.Low);

            double overconfidenceIndex = Math.Round((double)overconfident / answered.Count * 100, 1);
            double underconfidenceIndex = Math.Round((double)underconfident / answered.Count * 100, 1);

            return (overconfidenceIndex, underconfidenceIndex);
        }
    }
}
