using Cognify.Core.Entities;
using Cognify.Core.Enums;

namespace Cognify.Services
{
    public sealed class ThreeParameterIrtService
    {
        public const double MinimumTheta = -4;
        public const double MaximumTheta = 4;
        public const double ThetaStep = 0.05;
        public const double PriorMean = 0;
        public const double PriorStandardDeviation = 1;
        private const double MinimumProbability = 1e-12;

        public Question? SelectNextQuestion(
            IEnumerable<Question> questionPool,
            IEnumerable<AttemptAnswer> answers,
            int attemptId)
        {
            var answered = answers.ToList();
            var seenQuestionIds = answered.Select(a => a.QuestionId).ToHashSet();
            var candidates = questionPool
                .Where(q => !seenQuestionIds.Contains(q.QuestionId))
                .OrderBy(q => q.QuestionId)
                .ToList();

            if (candidates.Count == 0)
                return null;

            var theta = EstimateAbility(answered).Theta;
            var ranked = candidates
                .Select(question => new
                {
                    Question = question,
                    Information = FisherInformation(question, theta)
                })
                .ToList();

            var maximumInformation = ranked.Max(item => item.Information);
            var bestCandidates = ranked
                .Where(item => Math.Abs(item.Information - maximumInformation) < 1e-10)
                .Select(item => item.Question)
                .ToList();

            var selectionSeed = unchecked((attemptId * 397) + answered.Count);
            return bestCandidates[new Random(selectionSeed).Next(bestCandidates.Count)];
        }

        public IrtAbilityEstimate EstimateAbility(IEnumerable<AttemptAnswer> answers)
        {
            var responses = answers
                .Where(answer => answer.Question != null &&
                    !string.IsNullOrWhiteSpace(answer.SelectedOption))
                .ToList();

            var posterior = new List<(double Theta, double LogWeight)>();
            var maximumLogWeight = double.NegativeInfinity;

            for (var theta = MinimumTheta; theta <= MaximumTheta + 1e-9; theta += ThetaStep)
            {
                var logWeight = -0.5 * theta * theta;
                foreach (var answer in responses)
                {
                    var probability = ProbabilityCorrect(answer.Question!, theta);
                    logWeight += Math.Log(answer.IsCorrect
                        ? probability
                        : 1 - probability);
                }

                posterior.Add((theta, logWeight));
                maximumLogWeight = Math.Max(maximumLogWeight, logWeight);
            }

            var weights = posterior
                .Select(point => Math.Exp(point.LogWeight - maximumLogWeight))
                .ToList();
            var totalWeight = weights.Sum();
            var thetaEstimate = posterior
                .Select((point, index) => point.Theta * weights[index])
                .Sum() / totalWeight;
            var variance = posterior
                .Select((point, index) =>
                    Math.Pow(point.Theta - thetaEstimate, 2) * weights[index])
                .Sum() / totalWeight;

            return new IrtAbilityEstimate(thetaEstimate, Math.Sqrt(variance));
        }

        public double ToScaledScore(double theta)
        {
            var boundedTheta = Math.Clamp(theta, -20, 20);
            return 100 / (1 + Math.Exp(-boundedTheta));
        }

        public IrtItemReport GetItemReport(Question question, AttemptAnswer answer, double theta)
        {
            var (discrimination, difficulty, guessing) = GetParameters(question);
            var probability = ProbabilityCorrect(question, theta);
            return new IrtItemReport(
                answer.SequencePosition,
                answer.IsCorrect,
                string.IsNullOrWhiteSpace(answer.SelectedOption),
                discrimination,
                difficulty,
                guessing,
                probability,
                FisherInformation(question, theta));
        }

        private static double FisherInformation(Question question, double theta)
        {
            var discrimination = 1.0;
            var difficulty = GetDifficultyParameter(question.DifficultyLevel);
            var guessing = GetGuessingParameter(question);
            var logistic = 1 / (1 + Math.Exp(-discrimination * (theta - difficulty)));
            var probability = guessing + (1 - guessing) * logistic;
            var derivative = discrimination * (1 - guessing) * logistic * (1 - logistic);
            return derivative * derivative /
                   Math.Max(MinimumProbability, probability * (1 - probability));
        }

        private static double ProbabilityCorrect(Question question, double theta)
        {
            var discrimination = 1.0;
            var difficulty = GetDifficultyParameter(question.DifficultyLevel);
            var guessing = GetGuessingParameter(question);
            var logistic = 1 / (1 + Math.Exp(-discrimination * (theta - difficulty)));
            return Math.Clamp(guessing + (1 - guessing) * logistic,
                MinimumProbability, 1 - MinimumProbability);
        }

        private static double GetDifficultyParameter(DifficultyLevel difficulty) =>
            difficulty switch
            {
                DifficultyLevel.Easy => -1,
                DifficultyLevel.Medium => 0,
                DifficultyLevel.Hard => 1,
                _ => 0
            };

        private static (double Discrimination, double Difficulty, double Guessing) GetParameters(Question question) =>
            (1.0, GetDifficultyParameter(question.DifficultyLevel), GetGuessingParameter(question));

        private static double GetGuessingParameter(Question question)
        {
            var optionCount = new[]
            {
                question.OptionA,
                question.OptionB,
                question.OptionC,
                question.OptionD
            }.Count(option => !string.IsNullOrWhiteSpace(option));

            return 1.0 / Math.Max(2, optionCount);
        }
    }

    public sealed record IrtAbilityEstimate(double Theta, double StandardError);
    public sealed record IrtItemReport(
        int ItemNumber,
        bool IsCorrect,
        bool IsSkipped,
        double Discrimination,
        double Difficulty,
        double Guessing,
        double ProbabilityAtEstimate,
        double FisherInformation);
}
