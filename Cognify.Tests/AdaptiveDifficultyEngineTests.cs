using Cognify.Core.Enums;
using Cognify.Services;
using Xunit;

namespace Cognify.Tests
{
    // Tests only the pure difficulty-transition logic (GetNextDifficulty),
    // which needs no database. GetNextQuestionAsync depends on QuestionRepository
    // and is better covered with an integration test against a real/in-memory DbContext.
    public class AdaptiveDifficultyEngineTests
    {
        private readonly AdaptiveDifficultyEngine _engine = new(null!);

        [Theory]
        [InlineData(DifficultyLevel.Easy, true, DifficultyLevel.Medium)]
        [InlineData(DifficultyLevel.Medium, true, DifficultyLevel.Hard)]
        [InlineData(DifficultyLevel.Hard, true, DifficultyLevel.Hard)] // caps at Hard
        public void CorrectAnswer_IncreasesDifficulty(DifficultyLevel current, bool wasCorrect, DifficultyLevel expected)
        {
            var result = _engine.GetNextDifficulty(current, wasCorrect);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(DifficultyLevel.Hard, false, DifficultyLevel.Medium)]
        [InlineData(DifficultyLevel.Medium, false, DifficultyLevel.Easy)]
        [InlineData(DifficultyLevel.Easy, false, DifficultyLevel.Easy)] // floors at Easy
        public void WrongAnswer_DecreasesDifficulty(DifficultyLevel current, bool wasCorrect, DifficultyLevel expected)
        {
            var result = _engine.GetNextDifficulty(current, wasCorrect);
            Assert.Equal(expected, result);
        }
    }
}
