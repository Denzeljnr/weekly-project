using Microsoft.EntityFrameworkCore;
using WriteAngle.Components;
using WriteAngle.Data;
using WriteAngle.Interfaces;
using WriteAngle.Services;
using WriteAngle.Services.Agents;
using WriteAngle.Services.Export;
using WriteAngle.Services.Gemini;
using WriteAngle.Services.Search;

var builder = WebApplication.CreateBuilder(args);

// --- Default Blazor Web App (Interactive Server) template services ---
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// --- WriteAngle additions (all registrations go BEFORE builder.Build()) ---

// EF Core / Postgres
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("WriteAngle")));

// Gemini HTTP client
builder.Services.AddHttpClient<IGeminiTextClient, GeminiTextClient>();

// Search + agents
builder.Services.AddScoped<ISearchClient, GeminiSearchClient>();
builder.Services.AddScoped<IResearchAgent, ResearchAgent>();
builder.Services.AddScoped<ICriticAgent, CriticAgent>();
builder.Services.AddScoped<IWriterAgent, WriterAgent>();

// Orchestration + persistence + export
builder.Services.AddScoped<IReportOrchestrator, ReportOrchestrator>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IPdfExportService, PdfExportService>();

var app = builder.Build();

// --- Default Blazor Web App middleware pipeline ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// --- WriteAngle PDF export endpoint ---
app.MapGet("/export/{id:int}", async (int id, IReportRepository repo, IPdfExportService exporter) =>
{
    var report = await repo.GetByIdAsync(id);
    if (report == null) return Results.NotFound();
    var pdfBytes = exporter.Export(report);
    return Results.File(pdfBytes, "application/pdf", $"report-{id}.pdf");
});

app.Run();