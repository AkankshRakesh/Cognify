namespace Cognify.Web.ViewModels;

public class DashboardVM
{
    public string FullName { get; set; } = "";
    public int Level { get; set; } = 1;
    public int Xp { get; set; }
    public int XpToNext { get; set; } = 100;
    public int StreakDays { get; set; }

    public int ExamsTaken { get; set; }
    public double AverageScore { get; set; }          // 0-100
    public double CalibrationScore { get; set; }      // 0-100, higher = better calibrated

    public List<QuestVM> UpcomingExams { get; set; } = new();
    public List<ResultVM> RecentResults { get; set; } = new();
    public List<ConceptVM> Concepts { get; set; } = new();
    public List<BadgeVM> Badges { get; set; } = new();
}

public class QuestVM
{
    public int ExamId { get; set; }
    public string ExamToken { get; set; } = "";
    public string Title { get; set; } = "";
    public string Subject { get; set; } = "";
    public DateTime StartTime { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsAdaptive { get; set; }
    public bool IsConfidenceWeighted { get; set; }
    public int XpReward { get; set; } = 50;
    public bool IsOpenNow => StartTime <= DateTime.Now;
}

public class ResultVM
{
    public int AttemptId { get; set; }
    public string AttemptToken { get; set; } = "";
    public int ExamId { get; set; }
    public string ExamToken { get; set; } = "";
    public string Title { get; set; } = "";
    public DateTime SubmittedOn { get; set; }
    public int Score { get; set; }
    public int TotalMarks { get; set; }
    public int Percent => TotalMarks == 0 ? 0 : (int)Math.Round(Score * 100.0 / TotalMarks);
    public int Stars => Percent >= 85 ? 3 : Percent >= 60 ? 2 : Percent >= 35 ? 1 : 0;
}

public class ConceptVM
{
    public string Name { get; set; } = "";
    public int Mastery { get; set; }                  // 0-100
}

public class BadgeVM
{
    public string Name { get; set; } = "";
    public string Hint { get; set; } = "";
    public string Glyph { get; set; } = "★";
    public bool Unlocked { get; set; }
}
