using Microsoft.Extensions.DependencyInjection;
using TelegramBot.Config;
using TelegramBot.Interfaces;
using TelegramBot.Services;
using TelegramBot.Support;

var builder = WebApplication.CreateBuilder(args);

// Bot config
var botConfig = new TelegramBotConfig
{
    Token = builder.Configuration["TelegramBotConfig:Token"],
    ApiBaseUrl = builder.Configuration["TelegramBotConfig:ApiBaseUrl"] 
};

// DI
builder.Services.AddSingleton(botConfig);
builder.Services.AddSingleton<ITelegramBotService, TelegramBotService>();
builder.Services.AddSingleton<ICategorySupport, CategorySupport>();
builder.Services.AddSingleton<IAdviceService, AdviceService>();
builder.Services.AddSingleton<IExpensesSupport>(provider =>
    new ExpensesSupport(
        botConfig,
        new Lazy<ICategorySupport>(provider.GetRequiredService<ICategorySupport>),
        provider.GetRequiredService<IAdviceService>()
    ));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Bot start
var botService = app.Services.GetRequiredService<ITelegramBotService>();
var cts = new CancellationTokenSource();
await botService.StartAsync(cts.Token);

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();