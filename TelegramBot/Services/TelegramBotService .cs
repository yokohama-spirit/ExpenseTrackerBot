using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Config;

namespace TelegramBot.Services
{
    public class TelegramBotService : ITelegramBotService
    {
        private readonly TelegramBotClient _botClient;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<long, ExpenseCreationState> _userStates;
        private readonly TelegramBotConfig _config;

        public TelegramBotService(TelegramBotConfig config)
        {
            _config = config;
            _botClient = new TelegramBotClient(_config.Token);
            _httpClient = new HttpClient { BaseAddress = new Uri(_config.ApiBaseUrl) };
            _userStates = new Dictionary<long, ExpenseCreationState>();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                cancellationToken: cancellationToken);

            Console.WriteLine("Бот запущен. Нажмите Ctrl+C для остановки...");
        }

        private async Task HandleUpdateAsync(ITelegramBotClient bot, Telegram.Bot.Types.Update update, CancellationToken ct)
        {
            if (update.Message is not { } message || message.Text is not { } text)
                return;

            long chatId = message.Chat.Id;

            switch (text)
            {
                case "/start":
                    await HandleStartCommand(chatId, ct);
                    break;

                case "/create":
                    await HandleCreateCommand(chatId, ct);
                    break;

                case "/checkw":
                    await HandleCheckWeeklyCommand(chatId, ct);
                    break;

                case "/checkm":
                    await HandleCheckMonthlyCommand(chatId, ct);
                    break;

                default:
                    await HandleUserInput(chatId, text, ct);
                    break;
            }
        }
        private async Task HandleStartCommand(long chatId, CancellationToken ct)
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                "Доступные команды:\n" +
                "/create - добавить расход\n" +
                "/checkw - расходы за неделю\n" +
                "/checkm - расходы за месяц",
                cancellationToken: ct);
        }

        private async Task HandleCreateCommand(long chatId, CancellationToken ct)
        {
            _userStates[chatId] = new ExpenseCreationState { Step = 1 };


            var replyKeyboard = new ReplyKeyboardMarkup(new[]
            {
             new KeyboardButton[] { "Пропустить" }
            })
            {
                ResizeKeyboard = true 
            };


            await _botClient.SendTextMessageAsync(chatId, "Введите сумму расхода:", cancellationToken: ct);
        }

        private async Task HandleCheckWeeklyCommand(long chatId, CancellationToken ct)
        {
            var weekly = await _httpClient.GetFromJsonAsync<decimal>($"/api/expense/checkw/{chatId}", ct);
            await _botClient.SendTextMessageAsync(chatId, $"Расходы за неделю: {weekly} ₽", cancellationToken: ct);
        }

        private async Task HandleCheckMonthlyCommand(long chatId, CancellationToken ct)
        {
            var monthly = await _httpClient.GetFromJsonAsync<decimal>($"/api/expense/checkm/{chatId}", ct);
            await _botClient.SendTextMessageAsync(chatId, $"Расходы за месяц: {monthly} ₽", cancellationToken: ct);
        }

        private async Task HandleUserInput(long chatId, string text, CancellationToken ct)
        {
            if (!_userStates.TryGetValue(chatId, out var state)) return;

            switch (state.Step)
            {
                case 1 when decimal.TryParse(text, out var amount):
                    state.Amount = amount;
                    state.Step = 2;


                    var replyKeyboard = new ReplyKeyboardMarkup(new[]
                    {
                    new KeyboardButton[] { "Пропустить" }
                    })
                    {
                        ResizeKeyboard = true
                    };

                    await _botClient.SendTextMessageAsync(
                        chatId,
                        "Введите описание расхода:",
                        replyMarkup: replyKeyboard,
                        cancellationToken: ct);
                    break;

                case 2:

                    var removeKeyboard = new ReplyKeyboardRemove();

                    if (text == "Пропустить")
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId,
                            "Описание пропущено",
                            replyMarkup: removeKeyboard,
                            cancellationToken: ct);

                        await ProcessExpenseCreation(chatId, string.Empty, state, ct);
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId,
                            "Описание сохранено",
                            replyMarkup: removeKeyboard, 
                            cancellationToken: ct);

                        await ProcessExpenseCreation(chatId, text, state, ct);
                    }
                    break;

                default:
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        "Некорректный ввод, попробуйте снова",
                        cancellationToken: ct);
                    break;
            }
        }

        private async Task ProcessExpenseCreation(long chatId, string text, ExpenseCreationState state, CancellationToken ct)
        {
            var expense = new ExpenseTrackerLibrary.Domain.Entities.Expense
            {
                Amount = state.Amount,
                Content = text,
                ChatId = chatId
            };

            var response = await _httpClient.PostAsJsonAsync("/api/expense", expense, ct);
            if (response.IsSuccessStatusCode)
                await _botClient.SendTextMessageAsync(chatId, "✅ Расход добавлен!", cancellationToken: ct);
            else
                await _botClient.SendTextMessageAsync(chatId, "❌ Ошибка при добавлении", cancellationToken: ct);

            _userStates.Remove(chatId);
        }

        private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            return Task.CompletedTask;
        }
    }
}
