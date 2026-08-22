using Cognify.Core.Entities;
using Cognify.Core.Enums;

namespace Cognify.Core.Interfaces
{
    public interface IAdaptiveEngine
    {
        // Decides the next difficulty level for a concept based on whether
        // the previous answer (in that concept) was correct.
        DifficultyLevel GetNextDifficulty(DifficultyLevel current, bool wasCorrect);

        // Picks the next unseen question for the student matching (concept, difficulty),
        // falling back to the nearest available difficulty if the pool is exhausted.
        Task<Question?> GetNextQuestionAsync(int examId, int conceptTagId, DifficultyLevel targetDifficulty, IEnumerable<int> excludeQuestionIds);
    }
}
