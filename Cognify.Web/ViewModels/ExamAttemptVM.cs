using Cognify.Core.Enums;

namespace Cognify.Web.ViewModels
{
    public class ExamAttemptVM
    {
        public int AttemptId { get; set; }
        public string AttemptToken { get; set; } = string.Empty;
        public int ExamId { get; set; }
        public string ExamToken { get; set; } = string.Empty;
        public string ExamTitle { get; set; } = string.Empty;
        public int QuestionId { get; set; }
        public string QuestionToken { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public string OptionA { get; set; } = string.Empty;
        public string OptionB { get; set; } = string.Empty;
        public string OptionC { get; set; } = string.Empty;
        public string OptionD { get; set; } = string.Empty;
        public bool ShowConfidenceSlider { get; set; }
        public int SecondsRemaining { get; set; }
        public int CurrentQuestionNumber { get; set; }
    }

    public class AnswerSubmitVM
    {
        public string AttemptToken { get; set; } = string.Empty;
        public string QuestionToken { get; set; } = string.Empty;
        public string? SelectedOption { get; set; }
        public ConfidenceLevel? ConfidenceLevel { get; set; }
    }

    public class ResultSummaryVM
    {
        public int AttemptId { get; set; }
        public string AttemptToken { get; set; } = string.Empty;
        public string ExamTitle { get; set; } = string.Empty;
        public double TotalScore { get; set; }
        public double TotalMarks { get; set; }
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public int UnattemptedCount { get; set; }
        public bool HasCalibrationReport { get; set; }
        public double OverconfidenceIndex { get; set; }
        public double UnderconfidenceIndex { get; set; }
        public List<ConceptMapEntryVM> ConceptMap { get; set; } = new();
    }

    public class ConceptMapEntryVM
    {
        public string ConceptName { get; set; } = string.Empty;
        public double AccuracyPercent { get; set; }
    }
}
