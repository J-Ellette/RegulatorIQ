using Microsoft.EntityFrameworkCore;
using RegulatorIQ.Data;
using RegulatorIQ.Services;
using RegulatorIQ.Services.Analysis;
using RegulatorIQ.Services.BackgroundServices;
using Hangfire;
using Hangfire.PostgreSql;

var builder = Host.CreateApplicationBuilder(args);

// Database
builder.Services.AddDbContext<RegulatorIQContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Application Services
builder.Services.AddScoped<IRegulatoryDocumentService, RegulatoryDocumentService>();
builder.Services.AddScoped<IComplianceFrameworkService, ComplianceFrameworkService>();
builder.Services.AddScoped<IDocumentAnalysisService, DocumentAnalysisService>();
builder.Services.AddScoped<IChangeImpactService, ChangeImpactService>();
builder.Services.AddScoped<IRegulatoryMonitoringService, RegulatoryMonitoringService>();
builder.Services.AddScoped<IAIAnalysisProvider, AIAnalysisProvider>();
builder.Services.AddScoped<IRulesAnalysisProvider, RulesAnalysisProvider>();

// HTTP client for ML services
builder.Services.AddHttpClient("MLServices");

// AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Hangfire
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(o => o.UseNpgsqlConnection(connectionString)));
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = builder.Configuration.GetValue<int>("Hangfire:WorkerCount", 5);
});

var host = builder.Build();

// Configure recurring jobs
using (var scope = host.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobManager.ConfigureRegulatoryMonitoring();
}

await host.RunAsync();
