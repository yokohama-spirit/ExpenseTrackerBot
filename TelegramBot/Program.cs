using TelegramBot.Config;
using TelegramBot.Services;

var builder = WebApplication.CreateBuilder(args);

// Конфигурация
var botConfig = new TelegramBotConfig
{
    Token = "8072496681:AAGjLFDlInucYN0wHrWyzfMgzLO1g1wROZ4",
    ApiBaseUrl = "https://localhost:3101"
};

// Регистрация сервисов
builder.Services.AddSingleton(botConfig);
builder.Services.AddSingleton<ITelegramBotService, TelegramBotService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Запуск бота
var botService = app.Services.GetRequiredService<ITelegramBotService>();
var cts = new CancellationTokenSource();
await botService.StartAsync(cts.Token);

// Конфигурация HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();