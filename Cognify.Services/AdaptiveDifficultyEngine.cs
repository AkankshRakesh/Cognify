using Cognify.Core.Entities;
using Cognify.Core.Enums;
using Cognify.Core.Interfaces;
using Cognify.Data.Repositories;

namespace Cognify.Services
{
    // Mini CAT-style (Computer Adaptive Testing) engine:
    // correct answer -> next question in that concept is one level harder,
    // wrong answer -> one level easier. Falls back to nearest available
    // difficulty if the configured pool for a level is exhausted.
    public class AdaptiveDifficultyEngine : IAdaptiveEngine
    {
        private readonly QuestionRepository _questionRepository;

        public AdaptiveDifficultyEngine(QuestionRepository questionRepository)
        {
            _questionRepository = questionRepository;
        }

        public DifficultyLevel GetNextDifficulty(DifficultyLevel current, bool wasCorrect)
        {
            if (wasCorrect)
            {
                return current switch
                {
                    DifficultyLevel.Easy => DifficultyLevel.Medium,
                    DifficultyLevel.Medium => DifficultyLevel.Hard,
                    DifficultyLevel.Hard => DifficultyLevel.Hard,
                    _ => DifficultyLevel.Medium
                };
            }

            return current switch
            {
                DifficultyLevel.Hard => DifficultyLevel.Medium,
                DifficultyLevel.Medium => DifficultyLevel.Easy,
                DifficultyLevel.Easy => DifficultyLevel.Easy,
                _ => DifficultyLevel.Medium
            };
        }

        public async Task<Question?> GetNextQuestionAsync(int examId, int conceptTagId, DifficultyLevel targetDifficulty, IEnumerable<int> excludeQuestionIds)
        {
            var excluded = excludeQuestionIds.ToList();

            // Try the exact target difficulty first
            var question = await _questionRepository.GetRandomUnseenAsync(conceptTagId, targetDifficulty, excluded);
            if (question != null) return question;

            // Fall back to nearest difficulty if the pool is exhausted
            var fallbackOrder = targetDifficulty switch
            {
                DifficultyLevel.Easy => new[] { DifficultyLevel.Medium, DifficultyLevel.Hard },
                DifficultyLevel.Medium => new[] { DifficultyLevel.Easy, DifficultyLevel.Hard },
                DifficultyLevel.Hard => new[] { DifficultyLevel.Medium, DifficultyLevel.Easy },
                _ => Array.Empty<DifficultyLevel>()
            };

            foreach (var level in fallbackOrder)
            {
                question = await _questionRepository.GetRandomUnseenAsync(conceptTagId, level, excluded);
                if (question != null) return question;
            }

            return null; // pool for this concept fully exhausted
        }
    }
}
