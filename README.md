# Cognify

## Online Examination and Auto-Evaluation System

Cognify is an ASP.NET Core MVC application for creating and attempting
objective (MCQ) examinations. It supports fixed-question exams, confidence
weighted scoring, and adaptive exams using a simplified three-parameter Item
Response Theory (3PL IRT) model.

## Deployment

The application is deployed at:

**http://cognify.runasp.net/**

## Technology Stack

- .NET 8 / ASP.NET Core MVC
- C#
- Razor Views
- Entity Framework Core 8
- SQL Server / LocalDB
- Cookie authentication
- ASP.NET Core Data Protection
- Bootstrap, custom CSS, and JavaScript

## Solution Architecture

```text
Cognify.sln
|
├── Cognify.Web
|   ASP.NET Core MVC application
|   Controllers, Razor Views, ViewModels, authentication, startup
|
├── Cognify.Core
|   Domain entities, enums, and interfaces
|   Has no project dependencies
|
├── Cognify.Data
|   EF Core AppDbContext and repositories
|   SQL Server persistence
|
└── Cognify.Services
    Business logic and application services
    Scoring, question selection, adaptive testing, timing,
    password hashing, initialization, and analytics
```

The main request flow is:

```text
Browser
  -> ASP.NET Core middleware
  -> MVC controller
  -> repository / AppDbContext
  -> domain service
  -> SQL Server
  -> Razor view
  -> HTML response
```

## Implemented Features

### Accounts and security

- Student and teacher registration
- Custom cookie-based authentication
- Login, logout, and Remember Me support
- PBKDF2 password hashing with a random salt
- Role-aware student, teacher, and admin workflows
- ASP.NET Core Data Protection for exam, attempt, question, and question-set URL
  tokens
- Anti-forgery validation on POST forms
- Local return URL validation after login

ASP.NET Core Identity packages are referenced, but full Identity is not wired
up. The current implementation uses the application's custom `User` entity,
cookie authentication, claims, and `PasswordHasher`.

### Question banks

- Teacher-created question sets
- Built-in read-only question set
- Questions with four options, correct option, subject, concept, difficulty,
  and marks
- Concept tags
- Easy, Medium, and Hard difficulty levels
- Question editing and removal
- Question attempt and correctness statistics

### Exams

- Exam title, description, schedule, duration, and total marks
- Public/private exam flag
- Fixed-question exams
- Adaptive exams
- Optional negative marking
- Optional confidence-weighted scoring
- Teacher submission and result views
- One attempt per student per exam

### Exam attempts

- Server-created attempt records
- Server-authoritative deadline
- Immediate persistence of every answer
- Fixed or adaptive question delivery
- Skip support
- Duplicate-answer prevention
- Server-side answer and question-order validation
- Automatic completion when time expires or the question limit is reached
- Result summaries and answer analysis

### Adaptive and analytical features

- 3PL IRT adaptive question selection
- Bayesian ability estimation
- Fisher-information-based question selection
- Ability standard error
- Theta-to-0-100 score conversion
- Confidence calibration analysis
- Overconfidence and underconfidence indexes
- Per-concept performance summaries
- Teacher analytics and student dashboards

## Exam Modes

The `ScoringMode` flags enum supports these combinations:

| Mode | Question delivery | Scoring |
|---|---|---|
| Standard | Fixed question list | Normal marks with optional negative marking |
| ConfidenceWeighted | Fixed question list | Confidence-based marks |
| Adaptive | Runtime adaptive selection | IRT ability score |
| Both | Runtime adaptive selection | IRT score, with confidence used for calibration |

### Fixed-question exams

Fixed exams use `ExamQuestion` records. Questions are served according to
`SequenceOrder`.

The current fixed selector reads the stored order. Although `Exam` contains a
`ShuffleQuestions` property, the current selection service does not yet
randomize the order per student.

### Adaptive exams

New adaptive exams use the configured `QuestionSet` and
`AdaptiveQuestionCount`. The adaptive service:

1. Excludes questions already answered.
2. Estimates the student's current ability (`theta`).
3. Calculates Fisher information for remaining questions.
4. Selects the question with maximum information.
5. Uses a deterministic seed to resolve equal-information ties.

Adaptive exams currently use provisional item parameters:

- Discrimination `a = 1`
- Difficulty `b = -1` for Easy, `0` for Medium, and `1` for Hard
- Guessing `c = 1 / populated option count`, with a minimum denominator of 2

These are defaults, not empirically calibrated item parameters.

## Exam-Taking Flow

```text
1. Student opens a protected exam link.
2. The token, identity, role, exam schedule, and existing attempts are checked.
3. The student confirms the start.
4. An ExamAttempt is created and its start time is persisted.
5. The next fixed or adaptive question is selected.
6. The question is rendered by a Razor view.
7. The student submits an option, confidence level, or skip.
8. The server validates ownership, timing, question order, and option validity.
9. Correctness is calculated on the server.
10. The selected scoring strategy calculates marks.
11. AttemptAnswer is saved immediately.
12. The next question is selected.
13. On completion or expiry, the attempt is finalized.
14. Score, calibration, concept performance, and result data are saved.
15. The student is redirected to the result page.
```

The deadline is the earlier of:

```text
Exam.EndTime
Attempt.StartTime + Exam.DurationMinutes
```

The browser countdown is only a user-interface aid. The server recalculates
the deadline for every relevant request.

## Scoring

### Standard scoring

- Unattempted: `0`
- Correct: full question marks
- Wrong without negative marking: `0`
- Wrong with negative marking: `-25%` of question marks

### Confidence-weighted scoring

| Result | Low | Medium | High |
|---|---:|---:|---:|
| Correct | `0.75x` | `1.0x` | `1.5x` |
| Wrong with negative marking | `-0.25x` | `-0.5x` | `-1.0x` |
| Wrong without negative marking | `0` | `0` | `0` |

Positive marks are capped at the question's normal marks. In adaptive mode,
the IRT score is authoritative; confidence is collected for calibration when
the combined mode is used.

## Project Structure

### `Cognify.Web`

- `Program.cs` - application hosting, dependency injection, middleware,
  authentication, authorization, EF Core registration, and routing
- `Controllers/` - MVC request orchestration
- `ViewModels/` - form and display models
- `Views/` - Razor pages and layouts
- `Security/` - protected URL token services
- `wwwroot/` - CSS, JavaScript, and images

Important controllers:

- `AccountController`
- `DashboardController`
- `ExamController`
- `ExamAttemptController`
- `QuestionBankController`
- `ResultController`

`AdminController` and `AnalyticsController` currently exist as minimal
controllers without implemented actions.

### `Cognify.Core`

Entities include:

- `User`
- `Question`
- `QuestionSet`
- `ConceptTag`
- `Exam`
- `ExamQuestion`
- `ExamQuestionPool`
- `ExamAttempt`
- `AttemptAnswer`
- `ConceptPerformance`

Interfaces include:

- `IScoringService`
- `IQuestionSelectionService`
- `IAdaptiveEngine`
- `IConceptAnalyzer`

### `Cognify.Data`

- `AppDbContext`
- `QuestionRepository`
- `ExamRepository`
- `AttemptRepository`
- `UserRepository`

`AppDbContext` defines entity relationships, indexes, foreign keys, and delete
behavior for SQL Server.

### `Cognify.Services`

- `StandardScoringService`
- `ConfidenceWeightedScoringService`
- `ScoringServiceFactory`
- `FixedPoolSelectionService`
- `AdaptiveSelectionService`
- `AdaptiveDifficultyEngine`
- `ThreeParameterIrtService`
- `ExamTimerService`
- `ConceptMapGenerator`
- `PasswordHasher`
- `DbInitializer`
- `AdaptiveTestQuestionSetSeeder`

## Database and Initialization

The application uses SQL Server through Entity Framework Core. The default
development connection string targets SQL Server LocalDB:

```text
Server=(localdb)\mssqllocaldb;Database=CognifyDb;
Trusted_Connection=True;MultipleActiveResultSets=true
```

On startup, `DbInitializer`:

1. Ensures the database exists.
2. Applies compatibility SQL for schema additions.
3. Seeds concept tags.
4. Seeds a teacher account.
5. Seeds starter questions, question sets, and exams.
6. Seeds the built-in 120-question adaptive test bank.
7. Initializes student accounts with initial data.

The current repository uses `EnsureCreatedAsync` and compatibility SQL. It does
not currently contain EF Core migration files.

## Dependency Injection

The application uses ASP.NET Core's built-in dependency injection container.
Repositories and business services are registered mostly as scoped services,
including:

- `AppDbContext`
- Repositories
- Scoring services
- Question-selection services
- `ThreeParameterIrtService`
- `IConceptAnalyzer`
- `ScoringServiceFactory`

The factory selects scoring and question-selection strategies from the exam's
`ScoringMode`, so controllers do not implement the scoring algorithms directly.

## MVC and Razor

The project uses:

- Controllers and action methods
- Strongly typed ViewModels
- Razor views
- `_Layout.cshtml`
- `_ViewStart.cshtml`
- `_ViewImports.cshtml`
- Tag Helpers
- Model binding
- Data Annotation validation
- Server-side and client-side validation
- `TempData` for redirect messages
- `ViewBag` for small auxiliary view data

ASP.NET Core Session is not currently configured. `TempData` is used for
short-lived redirect messages, but it is not the same as session state.

## REST/Web API Status

This repository is currently an MVC/Razor application, not a full REST Web API.
It does not currently contain `[ApiController]` CRUD controllers, API
attribute routing, or Postman collections. The question bank includes a small
JSON endpoint for loading concept options, but the main application workflow
returns Razor-rendered HTML.

## Development

### Requirements

- .NET 8 SDK
- SQL Server LocalDB or a compatible SQL Server instance
- Visual Studio or VS Code with C# support

### Build

```powershell
dotnet build Cognify.sln
```

### Run

```powershell
dotnet run --project Cognify.Web
```

The connection string can be changed in:

```text
Cognify.Web/appsettings.json
```

## Current Limitations and Future Work

- Replace custom authentication with full ASP.NET Core Identity if required.
- Add EF Core migrations for production-grade schema evolution.
- Use `ExamTimerService` directly from the controller to centralize timing
  logic.
- Implement per-student fixed-question shuffling.
- Complete admin and analytics controller workflows.
- Replace landing-page placeholder statistics with live database data.
- Add a dedicated ASP.NET Core Web API layer and Postman collection.
- Add SQL views and stored procedures if required by the database syllabus.
- Calibrate adaptive item parameters using empirical response data.
