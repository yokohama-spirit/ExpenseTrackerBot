using ExpenseTrackerLibrary.Application.Dto;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Config;

namespace TelegramBot.Support
{
    public class CategorySupport : ICategorySupport
    {

        private readonly ITelegramBotClient _botClient;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<long, ExpenseCreationState> _userStates;
        private readonly Dictionary<long, CategoryCreationState> _userCatStates;
        private readonly Dictionary<long, CategoryCheckStateW> _checkByCatStates;
        private readonly Dictionary<long, CategoryCheckStateM> _checkByCatStatesM;
        private readonly Dictionary<long, CustomDaysCheck> _daysStates;
        private readonly TelegramBotConfig _config;
        private readonly IExpensesSupport _ex;

        public CategorySupport
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
            _daysStates = new Dictionary<long, CustomDaysCheck>();
            _ex = ex;
        }

        public async Task<bool> isActiveUserCatStates(long chatId)
        {
            if (_userCatStates.ContainsKey(chatId))
            {
                return true;
            }
            return false;
        }



        public async Task<bool> isActiveCheckByCatStates(long chatId)
        {
            if (_checkByCatStates.ContainsKey(chatId))
            {
                return true;
            }
            return false;
        }

        public async Task<bool> isActiveCheckByCatStatesM(long chatId)
        {
            if (_checkByCatStatesM.ContainsKey(chatId))
            {
                return true;
            }
            return false;
        }

        public async Task<bool> isActiveDaysStates(long chatId)
        {
            if (_daysStates.ContainsKey(chatId))
            {
                return true;
            }
            return false;
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
            await StateRemover(text, chatId, ct);

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
            bool isActive = _userStates.TryGetValue(chatId, out var state)
    || _userCatStates.TryGetValue(chatId, out var catState)
    || _checkByCatStates.TryGetValue(chatId, out var checkState)
    || _checkByCatStatesM.TryGetValue(chatId, out var checkStateM)
    || _daysStates.TryGetValue(chatId, out var daysStates)
    || await _ex.isActive(chatId, ct);

            if (isActive)
            {
                await ClearAllStates(chatId, ct);
            }
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



        public async Task HandleWeeklyCommand(long chatId, CancellationToken ct)
        {

            _checkByCatStates[chatId] = new CategoryCheckStateW
            {
                Step = 1
            };

            Console.WriteLine($"Состояние для {chatId} установлено: {nameof(CategoryCreationState)}");

            var removeKeyboard = new ReplyKeyboardRemove();
            await _botClient.SendMessage(
                chatId: chatId,
                text: "Введите название категории:",
                replyMarkup: removeKeyboard,
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

                    await StateRemover(text, chatId, ct);

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

            _checkByCatStatesM[chatId] = new CategoryCheckStateM
            {
                Step = 1
            };

            Console.WriteLine($"Состояние для {chatId} установлено: {nameof(CategoryCreationState)}");

            var removeKeyboard = new ReplyKeyboardRemove();
            await _botClient.SendMessage(
                chatId: chatId,
                text: "Введите название категории:",
                replyMarkup: removeKeyboard,
                cancellationToken: ct);
        }

        public async Task HandleMonthlyInputCommand(long chatId, string text, CancellationToken ct)
        {
            await StateRemover(text, chatId, ct);

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

        public async Task HandleDaysCommand(long chatId, CancellationToken ct)
        {

            _daysStates[chatId] = new CustomDaysCheck
            {
                Step = 1
            };

            Console.WriteLine($"Состояние для {chatId} установлено: {nameof(CategoryCreationState)}");

            var removeKeyboard = new ReplyKeyboardRemove();
            await _botClient.SendMessage(
                chatId: chatId,
                text: "Введите кол-во дней, за которое хотите получить отсчет:",
                replyMarkup: removeKeyboard,
                cancellationToken: ct);
        }

        public async Task HandleDaysInputCommand(long chatId, string text, CancellationToken ct)
        {
            await StateRemover(text, chatId, ct);

            if (!_daysStates.TryGetValue(chatId, out var state))
            {
                Console.WriteLine($"Не найдено состояние для chatId {chatId}");
                return;
            }


            switch (state.Step)
            {
                case 1 when decimal.TryParse(text, out var days):

                    var response = await _httpClient.GetFromJsonAsync<decimal>(
                    $"/api/expense/custom/{days}/{chatId}");

                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: $"За {days} дней вы потратили: {response}",
                        cancellationToken: ct);
                    _daysStates.Remove(chatId);
                    break;


                default:
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Некорректный ввод, попробуйте снова",
                        cancellationToken: ct);
                    break;
            }
        }


        public async Task ClearAllStatesNoUser(long chatId, CancellationToken ct)
        {
            _userStates.Remove(chatId);
            _userCatStates.Remove(chatId);
            _checkByCatStates.Remove(chatId);
            _checkByCatStatesM.Remove(chatId);
            _daysStates.Remove(chatId);
        }

        private async Task StateRemover(string text, long chatId, CancellationToken ct)
        {
            bool textIs = text == "/days" || text == "/create" || text == "/weekly"
            || text == "/monthly" || text == "/newcat" || text == "/mycat" 
            || text == "/weeklyc" || text == "/monthlyc" || text == "/myexp"
            || text == "/start" || text == "/commands" || text == "/statistic";
            if (textIs)
            {
                await ClearAllStates(chatId, ct);
            }
        }

        public async Task<bool> isActive(long chatId, CancellationToken ct)
        {
            return _userStates.TryGetValue(chatId, out var state)
            || _userCatStates.TryGetValue(chatId, out var catState)
            || _checkByCatStates.TryGetValue(chatId, out var checkState)
            || _checkByCatStatesM.TryGetValue(chatId, out var checkStateM)
            || _daysStates.TryGetValue(chatId, out var daysStates);
        }

        public async Task ClearAllStates(long chatId, CancellationToken ct)
        {
            await _ex.ClearUserState(chatId, ct);
            _userStates.Remove(chatId);
            _userCatStates.Remove(chatId);
            _checkByCatStates.Remove(chatId);
            _checkByCatStatesM.Remove(chatId);
            _daysStates.Remove(chatId);

            await _botClient.SendMessage(
                chatId: chatId,
                text: "❌ Команда отменена.\n" +
                "Для использования новой команды пропишите ее еще раз.",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: ct);
        }
    }
}
