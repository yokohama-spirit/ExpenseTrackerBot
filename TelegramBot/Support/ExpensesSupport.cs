using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Config;

namespace TelegramBot.Support
{
    public class ExpensesSupport : IExpensesSupport
    {
        private readonly ITelegramBotClient _botClient;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<long, ExpenseCreationState> _userStates;
        private readonly TelegramBotConfig _config;

        public ExpensesSupport(TelegramBotConfig config)
        {
            _config = config;
            _botClient = new TelegramBotClient(_config.Token);
            _httpClient = new HttpClient { BaseAddress = new Uri(_config.ApiBaseUrl) };
            _userStates = new Dictionary<long, ExpenseCreationState>();
        }

        public async Task HandleStartCommand(long chatId, CancellationToken ct)
        {
            await _botClient.SendMessage(
                chatId: chatId,
                text: "Привет! Это бот для подсчета твоих трат.\n" +
                      "Доступные команды:\n" +
                      "/create - добавить расход\n" +
                      "/checkw - расходы за неделю\n" +
                      "/checkm - расходы за месяц\n" +
                      "/newcat - создание новой категории расходов\n" +
                      "/mycat - получение своих категорий",
                cancellationToken: ct);
        }

        public async Task HandleCreateCommand(long chatId, CancellationToken ct)
        {
            _userStates[chatId] = new ExpenseCreationState { Step = 1 };


            await _botClient.SendMessage(
                chatId: chatId,
                text: "Введите сумму расхода:",
                cancellationToken: ct);
        }

        public async Task HandleCheckWeeklyCommand(long chatId, CancellationToken ct)
        {
            var weekly = await _httpClient.GetFromJsonAsync<decimal>($"/api/expense/checkw/{chatId}", ct);
            await _botClient.SendMessage(
                chatId: chatId,
                text: $"Расходы за неделю: {weekly} ₽",
                cancellationToken: ct);
        }

        public async Task HandleCheckMonthlyCommand(long chatId, CancellationToken ct)
        {
            var monthly = await _httpClient.GetFromJsonAsync<decimal>($"/api/expense/checkm/{chatId}", ct);
            await _botClient.SendMessage(
                chatId: chatId,
                text: $"Расходы за месяц: {monthly} ₽",
                cancellationToken: ct);
        }

        public async Task HandleUserInput(long chatId, string text, CancellationToken ct)
        {
            if (!_userStates.TryGetValue(chatId, out var state))
                return;

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
                        ResizeKeyboard = true,
                        OneTimeKeyboard = true
                    };

                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Введите описание расхода:",
                        replyMarkup: replyKeyboard,
                        cancellationToken: ct);
                    break;

                case 2:
                    var removeKeyboard = new ReplyKeyboardRemove();

                    if (text.Equals("Пропустить", StringComparison.OrdinalIgnoreCase))
                    {
                        await _botClient.SendMessage(
                            chatId: chatId,
                            text: "Описание пропущено",
                            replyMarkup: removeKeyboard,
                            cancellationToken: ct);

                        string content = "Не указано";

                        await ProcessExpenseCreation(chatId, content, state, ct);
                    }
                    else
                    {
                        await _botClient.SendMessage(
                            chatId: chatId,
                            text: "Описание сохранено",
                            replyMarkup: removeKeyboard,
                            cancellationToken: ct);

                        await ProcessExpenseCreation(chatId, text, state, ct);
                    }
                    break;

                default:
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Некорректный ввод, попробуйте снова",
                        cancellationToken: ct);
                    break;
            }
        }

        public async Task ProcessExpenseCreation(long chatId, string text, ExpenseCreationState state, CancellationToken ct)
        {
            var expense = new ExpenseTrackerLibrary.Domain.Entities.Expense
            {
                Amount = state.Amount,
                Content = text,
                ChatId = chatId
            };

            var response = await _httpClient.PostAsJsonAsync("/api/expense", expense, ct);

            if (response.IsSuccessStatusCode)
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "✅ Расход добавлен!",
                    cancellationToken: ct);
            }
            else
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "❌ Ошибка при добавлении",
                    cancellationToken: ct);
            }

            _userStates.Remove(chatId);
        }

        public Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            return Task.CompletedTask;
        }
    }
}
