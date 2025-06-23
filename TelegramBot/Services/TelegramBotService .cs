using ExpenseTrackerLibrary.Application.Dto;
using ExpenseTrackerLibrary.Domain.Entities;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Config;
using TelegramBot.Support;

namespace TelegramBot.Services
{
    public class TelegramBotService : ITelegramBotService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<long, ExpenseCreationState> _userStates;
        private readonly Dictionary<long, CategoryCreationState> _userCatStates;
        private readonly TelegramBotConfig _config;
        private readonly IExpensesSupport _ex;

        public TelegramBotService
            (TelegramBotConfig config,
            IExpensesSupport ex)
        {
            _config = config;
            _botClient = new TelegramBotClient(_config.Token);
            _httpClient = new HttpClient { BaseAddress = new Uri(_config.ApiBaseUrl) };
            _userStates = new Dictionary<long, ExpenseCreationState>();
            _userCatStates = new Dictionary<long, CategoryCreationState>();
            _ex = ex;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>() 
            };

            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandleErrorAsync, 
                receiverOptions: receiverOptions,
                cancellationToken: cancellationToken
            );

            Console.WriteLine("Бот запущен. Нажмите Ctrl+C для остановки...");
        }

        private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            if (update.Message is not { Text: { } text } message)
                return;

            long chatId = message.Chat.Id;


            if (_userCatStates.ContainsKey(chatId))
            {
                await HandleCategoryUserInput(chatId, text, ct);
                return;
            }


            switch (text)
            {
                case "/start":
                    await _ex.HandleStartCommand(chatId, ct);
                    break;

                case "/create":
                    await _ex.HandleCreateCommand(chatId, ct);
                    break;

                case "/checkw":
                    await _ex.HandleCheckWeeklyCommand(chatId, ct);
                    break;

                case "/checkm":
                    await _ex.HandleCheckMonthlyCommand(chatId, ct);
                    break;

                case "/newcat":
                    await HandleCreateCommand(chatId, ct);
                    break;

                case "/mycat":
                    await HandleMyCategoriesCommand(chatId, ct);
                    break;

                default:
                    await _ex.HandleUserInput(chatId, text, ct);
                    break;
            }
        }
        public async Task HandleCreateCommand(long chatId, CancellationToken ct)
        {
            Console.WriteLine($"Получена команда /newcat от chatId: {chatId}");

            _userCatStates[chatId] = new CategoryCreationState
            {
                Step = 1,
                OperationType = "category"
            };

            Console.WriteLine($"Состояние для {chatId} установлено: {nameof(CategoryCreationState)}");

            await _botClient.SendMessage(
                chatId: chatId,
                text: "Введите название категории:",
                cancellationToken: ct);
        }

        public async Task HandleCategoryUserInput(long chatId, string text, CancellationToken ct)
        {
            if (!_userCatStates.TryGetValue(chatId, out var state))
            {
                Console.WriteLine($"Не найдено состояние для chatId {chatId}");  
                return;
            }

            Console.WriteLine($"Обработка ввода для {chatId}: {text}, состояние: {state.OperationType}");

            switch (state.Step)
            {
                case 1:
                    await HandleCategoryInput(chatId, text, ct);
                    break;


                default:
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Некорректный ввод, попробуйте снова",
                        cancellationToken: ct);
                    break;
            }
        }


        private async Task HandleCategoryInput(long chatId, string text, CancellationToken ct)
        {
            var category = new CreateCategoryDTO
            {
                ChatId = chatId,
                Name = text
            };

            var response = await _httpClient.PostAsJsonAsync("/api/category", category, ct);

            Console.WriteLine($"Ответ от API: {response.StatusCode}");  // Логирование статуса ответа

            if (response.IsSuccessStatusCode)
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "✅ Категория успешно добавлена!",
                    cancellationToken: ct);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Ошибка при добавлении категории: {errorContent}");  // Лог ошибки

                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "❌ Ошибка при добавлении категории",
                    cancellationToken: ct);
            }

            _userCatStates.Remove(chatId);
        }


        public async Task HandleMyCategoriesCommand(long chatId, CancellationToken ct)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/category/mycat/{chatId}", ct);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Получен ответ: {content}");


                    var messageText = string.IsNullOrEmpty(content)
                        ? "У вас пока нет категорий"
                        : $"Ваши категории: {content}";

                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: messageText,
                        cancellationToken: ct);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Ошибка API: {errorContent}");

                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "❌ Ошибка при получении категорий",
                        cancellationToken: ct);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Исключение: {ex}");
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "⚠ Ошибка соединения с сервером",
                    cancellationToken: ct);
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            return Task.CompletedTask;
        }
    }
}
