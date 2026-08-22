using Cognify.Core.Enums;

namespace Cognify.Core.Entities
{
    public class AttemptAnswer
    {
        public int AttemptAnswerId { get; set; }
        public int AttemptId { get; set; }
        public ExamAttempt? Attempt { get; set; }
        public int QuestionId { get; set; }
        public Question? Question { get; set; }

        public string? SelectedOption { get; set; }
        public ConfidenceLevel? ConfidenceLevel { get; set; } // null if confidence mode off
        public bool IsCorrect { get; set; }
        public double MarksAwarded { get; set; }
        public int SequencePosition { get; set; }

        // Records what difficulty this question was AT THE TIME it was served,
        // since in adaptive mode a question's calibrated difficulty can drift later.
        public DifficultyLevel DifficultyAtServeTime { get; set; }
    }
}
