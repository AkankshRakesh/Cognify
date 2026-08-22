using Cognify.Core.Enums;

namespace Cognify.Core.Entities
{
    public class Question
    {
        public int QuestionId { get; set; }
        public string Text { get; set; } = string.Empty;
        public string OptionA { get; set; } = string.Empty;
        public string OptionB { get; set; } = string.Empty;
        public string OptionC { get; set; } = string.Empty;
        public string OptionD { get; set; } = string.Empty;
        public string CorrectOption { get; set; } = string.Empty; // "A" | "B" | "C" | "D"
        public string Subject { get; set; } = string.Empty;
        public int ConceptTagId { get; set; }
        public ConceptTag? ConceptTag { get; set; }
        public DifficultyLevel DifficultyLevel { get; set; } = DifficultyLevel.Medium;
        public int Marks { get; set; } = 1;

        // Used by the adaptive engine's live difficulty calibration
        public int TimesAttempted { get; set; }
        public int TimesCorrect { get; set; }

        public double ObservedAccuracy => TimesAttempted == 0
            ? 0
            : (double)TimesCorrect / TimesAttempted * 100;
    }
}
