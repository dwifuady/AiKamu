using Serilog;
using AiKamu.Bot;
using AiKamu.Commands.OpenAi;
using AiKamu.Commands.AddCommand;
using AiKamu.Commands.SiCepat;
using AiKamu.Bot.Replier;

var builder = WebApplication.CreateBuilder(args);

#region Configuration
// Configuration
var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

if (environment?.Equals("Development", StringComparison.InvariantCultureIgnoreCase) == true)
{
    configurationBuilder.AddUserSecrets<DiscordBotConfig>(optional: true, reloadOnChange: true);
    configurationBuilder.AddUserSecrets<OpenAiConfig>(optional: true, reloadOnChange: true);
    configurationBuilder.AddUserSecrets<SiCepatConfig>(optional: true, reloadOnChange: true);
}

var configuration = configurationBuilder.Build();
builder.Services.Configure<DiscordBotConfig>(configuration.GetSection("DiscordBotConfig"));
builder.Services.Configure<OpenAiConfig>(configuration.GetSection("OpenAiConfig"));
builder.Services.Configure<SiCepatConfig>(configuration.GetSection("SiCepatConfig"));

#endregion

// Add services to the container.
builder.Services.AddSingleton<BotService>();
builder.Services.AddHostedService<BotService>();
builder.Services.AddSingleton<ISlashCommandReplier, SlashCommandReplier>();
builder.Services.AddSingleton<IMessageReplier, MessageReplier>();
builder.Services.AddHttpClient();

#region Commands
builder.Services.AddOpenAi();
builder.Services.AddCommandManagement();
builder.Services.AddSiCepat(configuration.GetSection("SiCepatConfig").Get<SiCepatConfig>());
#endregion

#region Serilog
// Serilog
builder.Host.UseSerilog();
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();
#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.MapGet("/", () => "Hello");

app.Run();