using Cognify.Core.Enums;

namespace Cognify.Web.ViewModels
{
    public class ExamCreateVM
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DurationMinutes { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool NegativeMarking { get; set; }
        public bool ShuffleQuestions { get; set; } = true;

        // The unique feature toggle screen: teacher can pick either/both/none
        public bool EnableConfidenceWeighted { get; set; }
        public bool EnableAdaptive { get; set; }

        public ScoringMode ResolveScoringMode()
        {
            var mode = ScoringMode.Standard;
            if (EnableConfidenceWeighted) mode |= ScoringMode.ConfidenceWeighted;
            if (EnableAdaptive) mode |= ScoringMode.Adaptive;
            return mode;
        }

        // Populated only if EnableAdaptive is true
        public List<ConceptPoolVM> ConceptPools { get; set; } = new();

        // Populated only if EnableAdaptive is false (manual/random fixed selection)
        public List<int> SelectedQuestionIds { get; set; } = new();
    }

    public class ConceptPoolVM
    {
        public int ConceptTagId { get; set; }
        public string ConceptName { get; set; } = string.Empty;
        public int EasyCount { get; set; }
        public int MediumCount { get; set; }
        public int HardCount { get; set; }
        public int AvailableEasy { get; set; }
        public int AvailableMedium { get; set; }
        public int AvailableHard { get; set; }
    }
}
