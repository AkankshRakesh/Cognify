using Cognify.Core.Enums;

namespace Cognify.Web.ViewModels
{
    public class ExamStartVM
    {
        public string ExamToken { get; set; } = string.Empty;
        public string ExamTitle { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DurationMinutes { get; set; }
        public int QuestionCount { get; set; }
    }

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
        public bool NegativeMarking { get; set; }
        public bool IsAdaptive { get; set; }
        public int SecondsRemaining { get; set; }
        public int CurrentQuestionNumber { get; set; }
        public int TotalQuestionCount { get; set; }
        public bool IsLastQuestion { get; set; }
    }

    public class AnswerSubmitVM
    {
        public string AttemptToken { get; set; } = string.Empty;
        public string QuestionToken { get; set; } = string.Empty;
        public string? SelectedOption { get; set; }
        public ConfidenceLevel? ConfidenceLevel { get; set; }
        public bool Skip { get; set; }
    }

    public class ResultSummaryVM
    {
        public int AttemptId { get; set; }
        public string AttemptToken { get; set; } = string.Empty;
        public string ExamTitle { get; set; } = string.Empty;
        public bool IsAdaptive { get; set; }
        public double? AbilityEstimate { get; set; }
        public double? AbilityStandardError { get; set; }
        public double? ConventionalScore { get; set; }
        public double? ConventionalTotalMarks { get; set; }
        public double TotalScore { get; set; }
        public double TotalMarks { get; set; }
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public int UnattemptedCount { get; set; }
        public bool HasCalibrationReport { get; set; }
        public double OverconfidenceIndex { get; set; }
        public double UnderconfidenceIndex { get; set; }
        public double AvailableMarks { get; set; }
        public double CorrectMarks { get; set; }
        public double NegativeMarks { get; set; }
        public double ConfidenceAdjustment { get; set; }
        public double IrtPriorMean { get; set; }
        public double IrtPriorStandardDeviation { get; set; }
        public double IrtThetaMinimum { get; set; }
        public double IrtThetaMaximum { get; set; }
        public double IrtThetaStep { get; set; }
        public List<IrtReportItemVM> IrtItems { get; set; } = new();
        public List<IrtAbilityProgressionVM> IrtAbilityProgression { get; set; } = new();
    }

    public class IrtReportItemVM
    {
        public int ItemNumber { get; set; }
        public bool IsCorrect { get; set; }
        public bool IsSkipped { get; set; }
        public double Discrimination { get; set; }
        public double Difficulty { get; set; }
        public double Guessing { get; set; }
        public double ProbabilityAtEstimate { get; set; }
        public double FisherInformation { get; set; }
    }

    public class IrtAbilityProgressionVM
    {
        public int ResponseNumber { get; set; }
        public bool IsCorrect { get; set; }
        public bool IsSkipped { get; set; }
        public double ItemDifficulty { get; set; }
        public double AbilityBefore { get; set; }
        public double AbilityAfter { get; set; }
        public double StandardErrorAfter { get; set; }
    }
}
