using TelegramBot.Config;
using TelegramBot.Services;

var builder = WebApplication.CreateBuilder(args);

// Bot config
var botConfig = new TelegramBotConfig
{
    Token = "8072496681:AAGjLFDlInucYN0wHrWyzfMgzLO1g1wROZ4",
    ApiBaseUrl = "https://localhost:3101"
};

// DI
builder.Services.AddSingleton(botConfig);
builder.Services.AddSingleton<ITelegramBotService, TelegramBotService>();
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