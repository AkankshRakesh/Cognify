using System.ComponentModel.DataAnnotations;
using Cognify.Core.Enums;

namespace Cognify.Web.ViewModels
{
    public class ExamCreateVM
    {
        [Required(ErrorMessage = "Exam Title is required")]
        [StringLength(150, ErrorMessage = "Title cannot exceed 150 characters")]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Subject { get; set; } = "Computer Science";

        [Required(ErrorMessage = "Duration is required")]
        [Range(5, 360, ErrorMessage = "Duration must be between 5 and 360 minutes")]
        [Display(Name = "Duration (Minutes)")]
        public int DurationMinutes { get; set; } = 45;

        [Required(ErrorMessage = "Start time is required")]
        [Display(Name = "Start Time")]
        public DateTime StartTime { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "End time is required")]
        [Display(Name = "End Time")]
        public DateTime EndTime { get; set; } = DateTime.Now.AddDays(7);

        [Display(Name = "Enable Negative Marking")]
        public bool NegativeMarking { get; set; }

        [Display(Name = "Shuffle Questions")]
        public bool ShuffleQuestions { get; set; } = true;

        // Scoring Modes
        [Display(Name = "Confidence-Weighted Scoring")]
        public bool EnableConfidenceWeighted { get; set; }

        [Display(Name = "Adaptive Difficulty Engine")]
        public bool EnableAdaptive { get; set; }

        public ScoringMode ResolveScoringMode()
        {
            var mode = ScoringMode.Standard;
            if (EnableConfidenceWeighted) mode |= ScoringMode.ConfidenceWeighted;
            if (EnableAdaptive) mode |= ScoringMode.Adaptive;
            return mode;
        }

        // Populated if EnableAdaptive is true
        public List<ConceptPoolVM> ConceptPools { get; set; } = new();

        // Selected question IDs for Fixed/Standard selection
        public List<int> SelectedQuestionIds { get; set; } = new();

        // Available question catalog for selection UI
        public List<QuestionSelectVM> AvailableQuestions { get; set; } = new();
    }

    public class ConceptPoolVM
    {
        public int ConceptTagId { get; set; }
        public string ConceptName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int EasyCount { get; set; }
        public int MediumCount { get; set; }
        public int HardCount { get; set; }
        public int AvailableEasy { get; set; }
        public int AvailableMedium { get; set; }
        public int AvailableHard { get; set; }
    }

    public class QuestionSelectVM
    {
        public int QuestionId { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int ConceptTagId { get; set; }
        public string ConceptName { get; set; } = string.Empty;
        public DifficultyLevel DifficultyLevel { get; set; }
        public int Marks { get; set; }
        public bool IsSelected { get; set; }
    }
}
