using Microsoft.EntityFrameworkCore;
using Cognify.Core.Interfaces;
using Cognify.Data;
using Cognify.Data.Repositories;
using Cognify.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// ---- MVC / Razor ----
builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
    
// ---- Database (SQL Server via EF Core) ----
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---- Authentication (ASP.NET Core Identity) ----
// TODO: wire up Identity<User,...> once the Identity store mapping is finalized.
// builder.Services.AddIdentity<User, IdentityRole>()...

// ---- Repositories ----
builder.Services.AddScoped<QuestionRepository>();
builder.Services.AddScoped<ExamRepository>();
builder.Services.AddScoped<AttemptRepository>();

// ---- Domain services ----
builder.Services.AddScoped<StandardScoringService>();
builder.Services.AddScoped<ConfidenceWeightedScoringService>();
builder.Services.AddScoped<FixedPoolSelectionService>();
builder.Services.AddScoped<AdaptiveSelectionService>();
builder.Services.AddScoped<IAdaptiveEngine, AdaptiveDifficultyEngine>();
builder.Services.AddScoped<IConceptAnalyzer, ConceptMapGenerator>();
builder.Services.AddScoped<ExamTimerService>();
builder.Services.AddScoped<ScoringServiceFactory>();


builder.Services.AddScoped<UserRepository>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await Cognify.Services.DbInitializer.InitializeAsync(db);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database initialization failed during startup.");
        throw;
    }
}

// ---- Middleware pipeline ----
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
