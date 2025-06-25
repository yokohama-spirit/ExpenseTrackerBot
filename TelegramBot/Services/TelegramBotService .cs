using ExpenseTrackerLibrary.Application.Dto;
using ExpenseTrackerLibrary.Domain.Entities;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Config;
using TelegramBot.Support;
using static System.Net.Mime.MediaTypeNames;

namespace TelegramBot.Services
{
    public class TelegramBotService : ITelegramBotService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<long, ExpenseCreationState> _userStates;
        private readonly Dictionary<long, CategoryCreationState> _userCatStates;
        private readonly Dictionary<long, CategoryCheckStateW> _checkByCatStates;
        private readonly Dictionary<long, CategoryCheckStateM> _checkByCatStatesM;
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
            _checkByCatStates = new Dictionary<long, CategoryCheckStateW>();
            _checkByCatStatesM = new Dictionary<long, CategoryCheckStateM>();
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

            if (_checkByCatStates.ContainsKey(chatId))
            {
                await HandleWeeklyInputCommand(chatId, text, ct);
                return;
            }

            if (_checkByCatStatesM.ContainsKey(chatId))
            {
                await HandleMonthlyInputCommand(chatId, text, ct);
                return;
            }

            switch (text)
            {
                case "/start":
                    await _ex.HandleStartCommand(chatId, ct);
                    break;

                case "/commands":
                    await _ex.HandleCommsCommand(chatId, ct);
                    break;

                case "/create":
                    await _ex.HandleCreateCommand(chatId, ct);
                    break;

                case "/weekly":
                    await _ex.HandleCheckWeeklyCommand(chatId, ct);
                    break;

                case "/monthly":
                    await _ex.HandleCheckMonthlyCommand(chatId, ct);
                    break;

                case "/newcat":
                    await HandleCreateCommand(chatId, ct);
                    break;

                case "/mycat":
                    await HandleMyCategoriesCommand(chatId, ct);
                    break;

                case "/weeklyc":
                    await HandleWeeklyCommand(chatId, ct);
                    break;

                case "/monthlyc":
                    await HandleMonthlyCommand(chatId, ct);
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
            var getResponse = await _httpClient.GetFromJsonAsync<bool>(
                    $"/api/category/ix/{text}/{chatId}");


            if (getResponse)
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "Категория с таким названием уже существует.",
                    cancellationToken: ct);
            }
            else
            {
                var category = new CreateCategoryDTO
                {
                    ChatId = chatId,
                    Name = text
                };

                var response = await _httpClient.PostAsJsonAsync("/api/category", category, ct);

                Console.WriteLine($"Ответ от API: {response.StatusCode}");
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
                    Console.WriteLine($"Ошибка при добавлении категории: {errorContent}");

                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "❌ Ошибка при добавлении категории",
                        cancellationToken: ct);
                }

                _userCatStates.Remove(chatId);
            }
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









        public async Task HandleWeeklyCommand(long chatId, CancellationToken ct)
        {
            Console.WriteLine($"Получена команда /newcat от chatId: {chatId}");

            _checkByCatStates[chatId] = new CategoryCheckStateW
            {
                Step = 1
            };

            Console.WriteLine($"Состояние для {chatId} установлено: {nameof(CategoryCreationState)}");

            await _botClient.SendMessage(
                chatId: chatId,
                text: "Введите название категории:",
                cancellationToken: ct);
        }

        public async Task HandleWeeklyInputCommand(long chatId, string text, CancellationToken ct)
        {
            if (!_checkByCatStates.TryGetValue(chatId, out var state))
            {
                Console.WriteLine($"Не найдено состояние для chatId {chatId}");
                return;
            }


            switch (state.Step)
            {
                case 1:
                    
                    var getResponse = await _httpClient.GetFromJsonAsync<bool>(
                    $"/api/category/ix/{text}/{chatId}");

                    if (!getResponse)
                    {
                        await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Категория указана некорректно, в вашем списке категорий её не имеется." +
                        "\nДля создания категории воспользуйтесь коммандой /newcat.",
                        cancellationToken: ct);
                        _checkByCatStates.Remove(chatId);
                    }
                    else
                    {
                        var weekly = await _httpClient.GetFromJsonAsync<decimal>($"/api/category/checkw/{text}/{chatId}", ct);
                        await _botClient.SendMessage(
                            chatId: chatId,
                            text: $"Расходы за неделю: {weekly} ₽",
                            cancellationToken: ct);
                        _checkByCatStates.Remove(chatId);
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


        public async Task HandleMonthlyCommand(long chatId, CancellationToken ct)
        {
            Console.WriteLine($"Получена команда /newcat от chatId: {chatId}");

            _checkByCatStatesM[chatId] = new CategoryCheckStateM
            {
                Step = 1
            };

            Console.WriteLine($"Состояние для {chatId} установлено: {nameof(CategoryCreationState)}");

            await _botClient.SendMessage(
                chatId: chatId,
                text: "Введите название категории:",
                cancellationToken: ct);
        }

        public async Task HandleMonthlyInputCommand(long chatId, string text, CancellationToken ct)
        {
            if (!_checkByCatStatesM.TryGetValue(chatId, out var state))
            {
                Console.WriteLine($"Не найдено состояние для chatId {chatId}");
                return;
            }


            switch (state.Step)
            {
                case 1:

                    var getResponse = await _httpClient.GetFromJsonAsync<bool>(
                    $"/api/category/ix/{text}/{chatId}");

                    if (!getResponse)
                    {
                        await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Категория указана некорректно, в вашем списке категорий её не имеется." +
                        "\nДля создания категории воспользуйтесь коммандой /newcat.",
                        cancellationToken: ct);
                        _checkByCatStatesM.Remove(chatId);
                    }
                    else
                    {
                        var monthly = await _httpClient.GetFromJsonAsync<decimal>($"/api/category/checkm/{text}/{chatId}", ct);
                        await _botClient.SendMessage(
                            chatId: chatId,
                            text: $"Расходы за месяц: {monthly} ₽",
                            cancellationToken: ct);
                        _checkByCatStatesM.Remove(chatId);
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
    }
}
