using Cognify.Core.Entities;

namespace Cognify.Core.Interfaces
{
    public interface IConceptAnalyzer
    {
        Task<IEnumerable<ConceptPerformance>> BuildConceptMapAsync(int attemptId);
        Task<(double overconfidence, double underconfidence)> CalculateCalibrationAsync(int attemptId);
    }
}
