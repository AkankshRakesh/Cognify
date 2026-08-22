using Cognify.Core.Entities;
using Cognify.Core.Enums;
using Cognify.Services;
using Xunit;

namespace Cognify.Tests
{
    public class ConfidenceWeightedScoringServiceTests
    {
        private readonly ConfidenceWeightedScoringService _service = new();

        private static Question Q(int marks = 2) => new() { Marks = marks };

        [Fact]
        public void CorrectHighConfidence_Gets1_5xMarks()
        {
            var answer = new AttemptAnswer { SelectedOption = "A", IsCorrect = true, ConfidenceLevel = ConfidenceLevel.High };
            var result = _service.CalculateMarks(Q(2), answer, negativeMarking: true);
            Assert.Equal(3.0, result); // 2 * 1.5
        }

        [Fact]
        public void WrongHighConfidence_GetsFullNegativePenalty()
        {
            var answer = new AttemptAnswer { SelectedOption = "B", IsCorrect = false, ConfidenceLevel = ConfidenceLevel.High };
            var result = _service.CalculateMarks(Q(2), answer, negativeMarking: true);
            Assert.Equal(-2.0, result); // 2 * -1.0
        }

        [Fact]
        public void CorrectLowConfidence_ScoresLessThanCorrectHighConfidence()
        {
            var lowConfAnswer = new AttemptAnswer { SelectedOption = "A", IsCorrect = true, ConfidenceLevel = ConfidenceLevel.Low };
            var highConfAnswer = new AttemptAnswer { SelectedOption = "A", IsCorrect = true, ConfidenceLevel = ConfidenceLevel.High };

            var lowResult = _service.CalculateMarks(Q(2), lowConfAnswer, negativeMarking: true);
            var highResult = _service.CalculateMarks(Q(2), highConfAnswer, negativeMarking: true);

            Assert.True(lowResult < highResult);
        }

        [Fact]
        public void Unattempted_ScoresZero()
        {
            var answer = new AttemptAnswer { SelectedOption = null, IsCorrect = false, ConfidenceLevel = null };
            var result = _service.CalculateMarks(Q(2), answer, negativeMarking: true);
            Assert.Equal(0, result);
        }
    }

    public class StandardScoringServiceTests
    {
        private readonly StandardScoringService _service = new();
        private static Question Q(int marks = 4) => new() { Marks = marks };

        [Fact]
        public void Correct_AwardsFullMarks()
        {
            var answer = new AttemptAnswer { SelectedOption = "A", IsCorrect = true };
            Assert.Equal(4, _service.CalculateMarks(Q(4), answer, negativeMarking: false));
        }

        [Fact]
        public void Wrong_WithNegativeMarking_DeductsQuarterMark()
        {
            var answer = new AttemptAnswer { SelectedOption = "B", IsCorrect = false };
            Assert.Equal(-1.0, _service.CalculateMarks(Q(4), answer, negativeMarking: true));
        }

        [Fact]
        public void Wrong_WithoutNegativeMarking_ScoresZero()
        {
            var answer = new AttemptAnswer { SelectedOption = "B", IsCorrect = false };
            Assert.Equal(0, _service.CalculateMarks(Q(4), answer, negativeMarking: false));
        }
    }
}
