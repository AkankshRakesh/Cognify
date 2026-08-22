# Cognify — Online Examination and Auto-Evaluation System

Built for SIH — Smart Education category.

An ASP.NET Core (MVC + Razor) platform for conducting objective (MCQ) exams,
with two optional diagnostic engines a teacher can toggle independently per exam:

- **Adaptive Difficulty Engine** — mini-CAT style question delivery. A correct
  answer in a concept pushes the next question in that concept one level harder;
  a wrong answer eases it one level down.
- **Confidence-Weighted Scoring** — students tag Low/Medium/High confidence per
  answer. Scoring rewards calibrated confidence and penalizes confidently-wrong
  answers more, producing a post-exam "Calibration Report."

Both are OFF by default (Standard scoring / fixed question list), so the platform
works as a normal MCQ exam tool if neither is enabled.

## Solution Layout

```
Cognify.sln
├── Cognify.Web          ASP.NET Core MVC app (Razor views, controllers)
├── Cognify.Core         Domain entities, enums, interfaces (no dependencies)
├── Cognify.Services     Business logic: scoring strategies, adaptive engine,
│                                 concept map / calibration analysis, exam timer
├── Cognify.Data         EF Core AppDbContext + repositories (SQL Server)
└── Cognify.Tests        xUnit tests for scoring & adaptive difficulty logic
```

## What's implemented in full

- All domain entities and EF Core mappings (`AppDbContext`)
- Repositories: `QuestionRepository`, `ExamRepository`, `AttemptRepository`
- Scoring strategies: `StandardScoringService`, `ConfidenceWeightedScoringService`
- Question delivery strategies: `FixedPoolSelectionService`, `AdaptiveSelectionService`
- `AdaptiveDifficultyEngine` — the difficulty ladder logic
- `ConceptMapGenerator` — weak-concept heatmap + overconfidence/underconfidence index
- `ScoringServiceFactory` — resolves the right strategy pair from `Exam.ScoringMode`
  (a `[Flags]` enum, so None / Confidence-only / Adaptive-only / Both all fall out
  naturally with no branching in controllers)
- `ExamTimerService` — server-side timing authority (never trusts the client clock)
- `Program.cs` — full DI wiring for everything above
- **`HomeController` + `Views/Home/Index.cshtml`** — fully built landing/dashboard
  page (see below)
- Unit tests for the adaptive difficulty transitions and both scoring strategies

## What's left as connecting stubs

Per project scope, these controllers/views exist (so the solution builds and
routes resolve) but contain only `// TODO` bodies — implement using the
services/repositories above:

- `AccountController` (Identity login/register)
- `ExamController` (create/configure exam, including the Adaptive pool config
  and fixed-question-selection screens described in `ExamCreateVM`)
- `ExamAttemptController` (start/attempt/submit flow using
  `IQuestionSelectionService`, `IScoringService`, `ExamTimerService`)
- `ResultController` (result summary + calibration report + concept map, using
  `IConceptAnalyzer`)
- `QuestionBankController`, `AdminController`, `AnalyticsController`

ASP.NET Core Identity itself is referenced (`Microsoft.AspNetCore.Identity.EntityFrameworkCore`
package is in the `.csproj`) but not yet wired into `Program.cs` — the `User`
entity currently stores its own `PasswordHash` field as a placeholder; swap
this for `IdentityUser<int>` when you wire up Identity properly.

## Home Page

`Views/Home/Index.cshtml` is fully built out as the flagship page:

- Hero section with a live "mock exam" card showing a question, options, and a
  confidence-pill selector, plus platform-wide stats
- A student dashboard strip (upcoming exams / recent results) shown only when
  `HomeIndexVM.IsAuthenticated` is true
- A feature grid that visually distinguishes the two "Unique" engines
  (amber = Adaptive, teal = Confidence-Weighted) from standard features
- A signature "Calibration Quadrant" visual — the same confidence-vs-correctness
  plot the real `ResultController` should eventually render from
  `IConceptAnalyzer.CalculateCalibrationAsync`
- A "How It Works" process list and role-based (Teacher / Student) entry cards
- A custom design system in `wwwroot/css/site.css` (ink navy + amber/teal accents,
  Sora/Inter typefaces) — not a default Bootstrap look

`HomeController.Index()` currently populates `HomeIndexVM` with representative
placeholder data so the page is fully demoable without a database connection.
Replace that block with real calls to `ExamRepository` / `AttemptRepository`
once the login flow and seed data are in place.

## Next Steps to Run This For Real

1. Wire ASP.NET Core Identity into `Program.cs` and adjust the `User` entity
   accordingly (or keep the custom auth and hash passwords properly, e.g. with
   `PasswordHasher<User>`).
2. Run `dotnet ef migrations add InitialCreate -p Cognify.Data -s Cognify.Web`
   then `dotnet ef database update` to create the SQL Server schema.
3. Implement `QuestionBankController` CRUD views for adding/importing questions.
4. Implement `ExamController.Create` to actually collect an `ExamCreateVM`,
   persist the `Exam` + `ExamQuestionPool`/`ExamQuestion` rows, and validate that
   the question bank has enough questions per difficulty when Adaptive is on.
5. Implement `ExamAttemptController` using `ScoringServiceFactory` to resolve
   the right `IQuestionSelectionService`, and `ExamTimerService` for server-side
   deadline enforcement.
6. Implement `ResultController` to call `IScoringService.CalculateMarks` per
   answer on submit, then `IConceptAnalyzer` to build the concept map and
   calibration indices for `ResultSummaryVM`.
7. Run `dotnet test` in `Cognify.Tests` to verify the scoring/adaptive
   logic before wiring it into controllers.
