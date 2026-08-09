using JobTracker.Components;
using JobTracker.Data;
using JobTracker.Services;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
builder.Services.AddSingleton<GmailReaderService>();
builder.Services.AddHttpClient<GeminiService>();
builder.Services.AddHostedService<GmailPollingService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/export/excel", async (AppDbContext db) =>
{
    var apps = await db.Applications
        .Where(a => a.Status == "offer" || a.Status == "rejected")
        .OrderByDescending(a => a.LastUpdated)
        .ToListAsync();

    using var workbook = new XLWorkbook();
    var sheet = workbook.Worksheets.Add("Outcomes");

    sheet.Cell(1, 1).Value = "Company";
    sheet.Cell(1, 2).Value = "Role";
    sheet.Cell(1, 3).Value = "Date Applied";
    sheet.Cell(1, 4).Value = "Outcome";
    sheet.Cell(1, 5).Value = "Date Decided";
    sheet.Cell(1, 6).Value = "AI Summary";
    sheet.Row(1).Style.Font.Bold = true;

    int row = 2;
    foreach (var a in apps)
    {
        sheet.Cell(row, 1).Value = a.Company;
        sheet.Cell(row, 2).Value = a.Role;
        sheet.Cell(row, 3).Value = a.DateApplied.ToString();
        sheet.Cell(row, 4).Value = a.Status;
        sheet.Cell(row, 5).Value = a.LastUpdated.ToShortDateString();
        sheet.Cell(row, 6).Value = a.Summary ?? "";
        row++;
    }

    sheet.Columns().AdjustToContents();

    using var stream = new MemoryStream();
    workbook.SaveAs(stream);

    return Results.File(stream.ToArray(),
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        $"job-outcomes-{DateTime.Today:yyyy-MM-dd}.xlsx");
});

app.Run();