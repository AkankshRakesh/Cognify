using Cognify.Core.Entities;
using Cognify.Core.Interfaces;

namespace Cognify.Services
{
    // Resolves the correct IScoringService and IQuestionSelectionService
    // combination for an exam's ScoringMode flags (None / Confidence / Adaptive / Both)
    // so controllers never need if-else branching on the mode themselves.
    public class ScoringServiceFactory
    {
        private readonly StandardScoringService _standard;
        private readonly ConfidenceWeightedScoringService _confidenceWeighted;
        private readonly FixedPoolSelectionService _fixedPool;
        private readonly AdaptiveSelectionService _adaptive;

        public ScoringServiceFactory(
            StandardScoringService standard,
            ConfidenceWeightedScoringService confidenceWeighted,
            FixedPoolSelectionService fixedPool,
            AdaptiveSelectionService adaptive)
        {
            _standard = standard;
            _confidenceWeighted = confidenceWeighted;
            _fixedPool = fixedPool;
            _adaptive = adaptive;
        }

        public IScoringService GetScoringService(Exam exam) =>
            exam.IsConfidenceWeighted ? _confidenceWeighted : _standard;

        public IQuestionSelectionService GetSelectionService(Exam exam) =>
            exam.IsAdaptive ? _adaptive : _fixedPool;
    }
}
