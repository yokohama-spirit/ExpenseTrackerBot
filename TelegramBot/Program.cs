using Telegram.Bot.Types;
using Telegram.Bot;
using TelegramBot;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var botClient = new TelegramBotClient("8072496681:AAGjLFDlInucYN0wHrWyzfMgzLO1g1wROZ4");
var httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:3101") };

// Состояния пользователей для создания расходов
var userStates = new Dictionary<long, ExpenseCreationState>();

botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync);
Console.WriteLine("Бот запущен. Нажмите Ctrl+C для остановки...");
Console.ReadLine();

async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
{
    if (update.Message is not { } message || message.Text is not { } text)
        return;

    long chatId = message.Chat.Id;

    // Обработка команд
    switch (text)
    {
        case "/start":
            await bot.SendTextMessageAsync(chatId,
                "Доступные команды:\n" +
                "/create - добавить расход\n" +
                "/checkw - расходы за неделю\n" +
                "/checkm - расходы за месяц");
            break;

        case "/create":
            userStates[chatId] = new ExpenseCreationState { Step = 1 };
            await bot.SendTextMessageAsync(chatId, "Введите сумму расхода:");
            break;

        case "/checkw":
            var weekly = await httpClient.GetFromJsonAsync<decimal>($"/api/expense/checkw/{chatId}");
            await bot.SendTextMessageAsync(chatId, $"Расходы за неделю: {weekly} ₽");
            break;

        case "/checkm":
            var monthly = await httpClient.GetFromJsonAsync<decimal>($"/api/expense/checkm/{chatId}");
            await bot.SendTextMessageAsync(chatId, $"Расходы за месяц: {monthly} ₽");
            break;

        default:
            if (userStates.TryGetValue(chatId, out var state))
            {
                switch (state.Step)
                {
                    case 1 when decimal.TryParse(text, out var amount):
                        state.Amount = amount;
                        state.Step = 2;
                        await bot.SendTextMessageAsync(chatId, "Введите описание расхода:");
                        break;

                    case 2:
                        var expense = new ExpenseTrackerLibrary.Domain.Entities.Expense
                        {
                            Amount = state.Amount,
                            Content = text,
                            ChatId = chatId
                        };

                        var response = await httpClient.PostAsJsonAsync("/api/expense", expense);
                        if (response.IsSuccessStatusCode)
                            await bot.SendTextMessageAsync(chatId, "✅ Расход добавлен!");
                        else
                            await bot.SendTextMessageAsync(chatId, "❌ Ошибка при добавлении");

                        userStates.Remove(chatId);
                        break;

                    default:
                        await bot.SendTextMessageAsync(chatId, "Некорректный ввод, попробуйте снова");
                        break;
                }
            }
            break;
    }
}

Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
{
    Console.WriteLine($"Ошибка: {ex.Message}");
    return Task.CompletedTask;
}


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
