using Cognify.Core.Entities;
using Cognify.Core.Enums;
using Cognify.Core.Interfaces;
using Cognify.Data.Repositories;

namespace Cognify.Services
{
    // Used when Exam.ScoringMode does NOT include Adaptive: serves questions
    // from the pre-selected fixed ExamQuestions list in order.
    public class FixedPoolSelectionService : IQuestionSelectionService
    {
        private readonly ExamRepository _examRepository;
        public FixedPoolSelectionService(ExamRepository examRepository) => _examRepository = examRepository;

        public async Task<Question?> GetFirstQuestionAsync(ExamAttempt attempt)
        {
            var exam = await _examRepository.GetByIdAsync(attempt.ExamId);
            return exam?.ExamQuestions.OrderBy(eq => eq.SequenceOrder).FirstOrDefault()?.Question;
        }

        public async Task<Question?> GetNextQuestionAsync(ExamAttempt attempt, AttemptAnswer lastAnswer)
        {
            var exam = await _examRepository.GetByIdAsync(attempt.ExamId);
            if (exam == null) return null;

            var ordered = exam.ExamQuestions.OrderBy(eq => eq.SequenceOrder).ToList();
            var currentIndex = ordered.FindIndex(eq => eq.QuestionId == lastAnswer.QuestionId);
            return currentIndex >= 0 && currentIndex + 1 < ordered.Count
                ? ordered[currentIndex + 1].Question
                : null; // no more questions
        }
    }

    // Used when Exam.ScoringMode includes Adaptive: delegates difficulty
    // decisions to IAdaptiveEngine and draws from ExamQuestionPool per concept.
    public class AdaptiveSelectionService : IQuestionSelectionService
    {
        private readonly IAdaptiveEngine _adaptiveEngine;
        private readonly ExamRepository _examRepository;

        public AdaptiveSelectionService(IAdaptiveEngine adaptiveEngine, ExamRepository examRepository)
        {
            _adaptiveEngine = adaptiveEngine;
            _examRepository = examRepository;
        }

        public async Task<Question?> GetFirstQuestionAsync(ExamAttempt attempt)
        {
            var exam = await _examRepository.GetByIdAsync(attempt.ExamId);
            var firstConcept = exam?.QuestionPools.FirstOrDefault();
            if (firstConcept == null) return null;

            // Every student starts at Medium difficulty for the first concept
            return await _adaptiveEngine.GetNextQuestionAsync(
                attempt.ExamId, firstConcept.ConceptTagId, DifficultyLevel.Medium, Enumerable.Empty<int>());
        }

        public async Task<Question?> GetNextQuestionAsync(ExamAttempt attempt, AttemptAnswer lastAnswer)
        {
            if (lastAnswer.Question == null) return null;

            var nextDifficulty = _adaptiveEngine.GetNextDifficulty(lastAnswer.DifficultyAtServeTime, lastAnswer.IsCorrect);
            var seenIds = attempt.Answers.Select(a => a.QuestionId).ToList();

            return await _adaptiveEngine.GetNextQuestionAsync(
                attempt.ExamId, lastAnswer.Question.ConceptTagId, nextDifficulty, seenIds);
        }
    }
}
