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

        [Display(Name = "Make exam public")]
        public bool IsPublic { get; set; } = true;

        [Required(ErrorMessage = "Please select a question set")]
        [Display(Name = "Question Set")]
        public int QuestionSetId { get; set; }

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

        [Display(Name = "Enable confidence-based marking")]
        public bool ConfidenceWeighted { get; set; }

        [Display(Name = "Enable adaptive IRT exam")]
        public bool Adaptive { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Adaptive exams must contain at least one question.")]
        [Display(Name = "Questions per student")]
        public int AdaptiveQuestionCount { get; set; } = 20;

        [Display(Name = "Shuffle Questions")]
        public bool ShuffleQuestions { get; set; } = true;

        public List<QuestionSetOptionVM> QuestionSets { get; set; } = new();
    }

    public class QuestionSetOptionVM
    {
        public int QuestionSetId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
        public int TotalMarks { get; set; }
        public bool IsBuiltIn { get; set; }
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
