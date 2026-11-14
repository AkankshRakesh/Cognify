using Cognify.Core.Enums;

namespace Cognify.Web.ViewModels
{
    public class QuestionBankIndexVM
    {
        public List<QuestionSetCatalogVM> QuestionSets { get; set; } = new();
        public List<QuestionItemVM> Questions { get; set; } = new();
        public List<ConceptTagOptionVM> ConceptTags { get; set; } = new();
        public int TotalQuestions { get; set; }
        public int EasyCount { get; set; }
        public int MediumCount { get; set; }
        public int HardCount { get; set; }
        public string? SearchQuery { get; set; }
        public int? SelectedConceptId { get; set; }
        public DifficultyLevel? SelectedDifficulty { get; set; }
    }

    public class QuestionSetCatalogVM
    {
        public int QuestionSetId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Subject { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
        public int TotalMarks { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool IsBuiltIn { get; set; }
    }

    public class QuestionItemVM
    {
        public int QuestionId { get; set; }
        public string Text { get; set; } = string.Empty;
        public string OptionA { get; set; } = string.Empty;
        public string OptionB { get; set; } = string.Empty;
        public string OptionC { get; set; } = string.Empty;
        public string OptionD { get; set; } = string.Empty;
        public string CorrectOption { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int ConceptTagId { get; set; }
        public string ConceptName { get; set; } = string.Empty;
        public DifficultyLevel DifficultyLevel { get; set; }
        public int Marks { get; set; }
        public int TimesAttempted { get; set; }
        public int TimesCorrect { get; set; }
        public double ObservedAccuracy => TimesAttempted == 0 ? 0 : Math.Round((double)TimesCorrect / TimesAttempted * 100, 1);
    }

    public class ConceptTagOptionVM
    {
        public int ConceptTagId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
    }

    public class QuestionSetCreateVM
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Subject { get; set; } = "Computer Science";
        public int? ConceptTagId { get; set; }
        public string? NewConceptName { get; set; }

        public List<QuestionInputVM> Questions { get; set; } = new();
    }

    public class QuestionInputVM
    {
        public int QuestionId { get; set; }
        public string Text { get; set; } = string.Empty;
        public string OptionA { get; set; } = string.Empty;
        public string OptionB { get; set; } = string.Empty;
        public string OptionC { get; set; } = string.Empty;
        public string OptionD { get; set; } = string.Empty;
        public string CorrectOption { get; set; } = "A";
        public DifficultyLevel DifficultyLevel { get; set; } = DifficultyLevel.Medium;
        public int Marks { get; set; } = 4;
    }
}

