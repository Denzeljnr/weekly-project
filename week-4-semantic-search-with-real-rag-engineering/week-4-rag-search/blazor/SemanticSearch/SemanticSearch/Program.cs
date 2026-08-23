using SemanticSearch.Components;
using SemanticSearch.Data;
using SemanticSearch.Interfaces;
using SemanticSearch.Services;
using SemanticSearch.Services.Chunking;
using SemanticSearch.Services.Gemini;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// --- Database, with pgvector type mapping ---
var connectionString = builder.Configuration.GetConnectionString("SemanticSearch");
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.UseVector();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource, o => o.UseVector()));

// --- Chunking strategies (Strategy pattern — IngestionService loops over all of these) ---
builder.Services.AddSingleton<IChunker, FixedSizeChunker>();
builder.Services.AddSingleton<IChunker, ParagraphAwareChunker>();

// --- Gemini clients (shared, reusable) ---
builder.Services.AddHttpClient<IGeminiTextClient, GeminiTextClient>();
builder.Services.AddHttpClient<IEmbeddingClient, GeminiEmbeddingClient>();

// --- Pipeline services ---
builder.Services.AddScoped<IIngestionService, IngestionService>();
builder.Services.AddSingleton<EmbeddingRateLimiter>();
builder.Services.AddSingleton<IPdfTextExtractor, PdfTextExtractor>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IRetrievalService, RetrievalService>();
builder.Services.AddScoped<IRerankingService, RerankingService>();
builder.Services.AddScoped<IAnswerService, AnswerService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();