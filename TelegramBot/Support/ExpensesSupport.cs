using ExpenseTrackerLibrary.Application.Services;
using ExpenseTrackerLibrary.Domain.Entities;
using System.Net.Http;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Config;
using TelegramBot.Interfaces;
using TelegramBot.Services;
using static System.Net.Mime.MediaTypeNames;

namespace TelegramBot.Support
{
    public class ExpensesSupport : IExpensesSupport
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IAdviceService _tipservice;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<long, ExpenseCreationState> _userStates;
        private readonly Dictionary<long, MyExpensesCheck> _myExp;
        private readonly Dictionary<long, LimitSet> _limit;
        private readonly TelegramBotConfig _config;
        private readonly Lazy<ICategorySupport> _service;
        
        public ExpensesSupport
            (TelegramBotConfig config,
            Lazy<ICategorySupport> service,
            IAdviceService tipservice)
        {
            _config = config;
            _botClient = new TelegramBotClient(_config.Token);
            _httpClient = new HttpClient { BaseAddress = new Uri(_config.ApiBaseUrl) };
            _userStates = new Dictionary<long, ExpenseCreationState>();
            _limit = new Dictionary<long, LimitSet>();
            _myExp = new Dictionary<long, MyExpensesCheck>();
            _service = service;
            _tipservice = tipservice;
        }

        public async Task HandleStartCommand(long chatId, CancellationToken ct)
        {
            var chat = await _botClient.GetChat(chatId, ct);

            if (_userStates.TryGetValue(chatId, out var state) || await _service.Value.isActive(chatId, ct))
            {
                await ClearAllStates(chatId, ct);
            }
            await _botClient.SendMessage(
                chatId: chatId,
                text: $"Привет, {chat.FirstName ?? "друг"}! Это бот для подсчета твоих расходов.\n" +
                      "Для получения всех доступных комманд пропиши /commands",
                cancellationToken: ct);
        }

        public async Task HandleCommsCommand(long chatId, CancellationToken ct)
        {
            if (_userStates.TryGetValue(chatId, out var state) || await _service.Value.isActive(chatId, ct))
            {
                await ClearAllStates(chatId, ct);
            }
            await _botClient.SendMessage(
                chatId: chatId,
                text: "Доступные команды:\n" +
                      "/create - добавить расход\n" +
                      "/statistic - получение статистики\n" +
                      "/weekly - расходы за неделю\n" +
                      "/monthly - расходы за месяц\n" +
                      "/newcat - создание новой категории расходов\n" +
                      "/mycat - получение своих категорий\n" +
                      "/weeklyc - получение расходов за неделю по определенной категории\n" +
                      "/monthlyc - получение расходов за месяц по определенной категории\n" +
                      "/days - получение расходов за любое кол-во дней\n" +
                      "/myexp - получение последних расходов\n" +
                      "/setlimit - назначение собственного лимита на месячные расходы\n" +
                      "/clear - удаление лимита\n" +
                      "/tips - получение совета по экономии денег",
                cancellationToken: ct);
        }


        //---------------------------------CREATE EXPENSE-----------------------------------



        public async Task HandleCreateCommand(long chatId, CancellationToken ct)
        {
            if (_userStates.TryGetValue(chatId, out var state) || await _service.Value.isActive(chatId, ct))
            {
                await ClearAllStates(chatId, ct);
            }

            _userStates[chatId] = new ExpenseCreationState { Step = 1 };


            await _botClient.SendMessage(
                chatId: chatId,
                text: "Введите сумму расхода:",
                cancellationToken: ct);
        }
        public async Task HandleUserInput(long chatId, string text, CancellationToken ct)
        {
            await StateRemover(text, chatId, ct);

            if (!_userStates.TryGetValue(chatId, out var state))
                return;

            switch (state.Step)
            {
                case 1 when decimal.TryParse(text, out var amount):
                    state.Amount = amount;
                    state.Step = 2;
                    await AskForDescription(chatId, ct);
                    break;

                case 2:

                    await StateRemover(text, chatId, ct);

                    var removeKeyboard = new ReplyKeyboardRemove();
                    state.Description = text.Equals("Пропустить", StringComparison.OrdinalIgnoreCase)
                        ? "Не указано"
                        : text;

                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: text.Equals("Пропустить", StringComparison.OrdinalIgnoreCase)
                            ? "Описание пропущено"
                            : "Описание сохранено",
                        replyMarkup: removeKeyboard,
                        cancellationToken: ct);

                    state.Step = 3;
                    await AskForCategory(chatId, ct);
                    break;

                case 3:

                    var removeCatKeyboard = new ReplyKeyboardRemove();
                    string category = text.Equals("Пропустить", StringComparison.OrdinalIgnoreCase)
                        ? "Не указано"
                        : text;

                    var getResponse = await _httpClient.GetFromJsonAsync<bool>(
                        $"/api/category/ix/{category}/{chatId}");


                    if (text.Equals("Пропустить", StringComparison.OrdinalIgnoreCase))
                    {
                        await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Категория пропущена",
                        replyMarkup: removeCatKeyboard,
                        cancellationToken: ct);

                        await ProcessExpenseCreation(chatId, state.Description, category, state, ct);
                    }
                    else if (!getResponse)
                    {
                        await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Категория указана некорректно, в вашем списке категорий её не имеется." +
                        "\nДля создания категории воспользуйтесь коммандой /newcat.",
                        cancellationToken: ct);
                    }
                    else
                    {
                        await _botClient.SendMessage(
                        chatId: chatId,
                        text: "Категория сохранена",
                        replyMarkup: removeCatKeyboard,
                        cancellationToken: ct);

                        await ProcessExpenseCreation(chatId, state.Description, category, state, ct);
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

        public async Task ProcessExpenseCreation(long chatId, string description, string categoryName,
            ExpenseCreationState state, CancellationToken ct)
        {
            var expense = new Expense
            {
                Amount = state.Amount,
                Content = description,
                ChatId = chatId
            };

            if (!categoryName.Equals("Не указано"))
            {
                expense.Categories.Add(new Category
                {
                    ChatId = chatId,
                    Name = categoryName
                });
            }

            var response = await _httpClient.PostAsJsonAsync("/api/expense", expense, ct);

            if (response.IsSuccessStatusCode)
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "✅ Расход добавлен!",
                    cancellationToken: ct);


                var limitCheck = await _httpClient.GetFromJsonAsync<LimitCheckResult>(
                    $"/api/limits/check?chatId={chatId}&amount={state.Amount}");

                if (limitCheck.IsLimitExceeded)
                {
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: "⚠ Лимит превзойден :(",
                        cancellationToken: ct);
                }
                else if (limitCheck.IsWarningNeeded)
                {
                    var messages = new[]
                    {
                $"До лимита осталось совсем чуть-чуть! Ваш лимит - {limitCheck.CurrentLimit}₽, а за текущий месяц уже потрачено {limitCheck.CurrentSpent}₽!😨",
                $"Осторожно! Вы превзошли 75% лимита ({limitCheck.CurrentSpent}₽ из {limitCheck.CurrentLimit}₽)🙀",
                $"Лимит близок! Осталось всего {limitCheck.CurrentLimit - limitCheck.CurrentSpent}₽ до предела😱",
                $"⚡До лимита рукой подать! Стремление - наше все, но в этом случае стоило бы притормозить...🙅"
                    };

                    var random = new Random();
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: messages[random.Next(messages.Length)],
                        cancellationToken: ct);
                }
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




        //---------------------------------CHECK WEEKLY-----------------------------------


        public async Task HandleCheckWeeklyCommand(long chatId, CancellationToken ct)
        {
            if (await TryCancelState("/weekly", chatId, ct))
                return;


            var weekly = await _httpClient.GetFromJsonAsync<decimal>($"/api/expense/checkw/{chatId}", ct);

            var removeKeyboard = new ReplyKeyboardRemove();
            await _botClient.SendMessage(
                chatId: chatId,
                text: $"Расходы за последнюю неделю: {weekly} ₽",
                replyMarkup: removeKeyboard,
                cancellationToken: ct);
        }


        //---------------------------------CHECK MONTHLY-----------------------------------

        public async Task HandleCheckMonthlyCommand(long chatId, CancellationToken ct)
        {
            if (await TryCancelState("/monthly", chatId, ct))
                return;


            var monthly = await _httpClient.GetFromJsonAsync<decimal>($"/api/expense/checkm/{chatId}", ct);

            var removeKeyboard = new ReplyKeyboardRemove();
            await _botClient.SendMessage(
                chatId: chatId,
                text: $"Расходы за последний месяц: {monthly} ₽",
                replyMarkup: removeKeyboard,
                cancellationToken: ct);
        }


        //---------------------------------CHECK STATISTIC-----------------------------------

        public async Task HandleStatisticCommand(long chatId, CancellationToken ct)
        {
            if (await TryCancelState("/statistic", chatId, ct))
                return;


            var stat = await _httpClient.GetStringAsync($"/api/expense/statistic/{chatId}", ct);

            var removeKeyboard = new ReplyKeyboardRemove();
            await _botClient.SendMessage(
                chatId: chatId,
                text: stat,
                replyMarkup: removeKeyboard,
                cancellationToken: ct);
        }



        //---------------------------------GET TIPS-----------------------------------

        public async Task HandleGetTipsCommand(long chatId, CancellationToken ct)
        {
            if (await TryCancelState("/tips", chatId, ct))
                return;


            var tips = await _tipservice.GetFullTips();

            var removeKeyboard = new ReplyKeyboardRemove();

            var random = new Random();
            await _botClient.SendMessage(
                chatId: chatId,
                text: tips[random.Next(tips.Length)],
                replyMarkup: removeKeyboard,
                cancellationToken: ct);
        }


        //---------------------------------LIMITS (SET, CLEAR)-----------------------------------

        public async Task HandleSetLimitCommand(long chatId, CancellationToken ct)
        {

            _limit[chatId] = new LimitSet
            {
                Step = 1
            };

            Console.WriteLine($"Состояние для {chatId} установлено: {nameof(MyExpensesCheck)}");

            var removeKeyboard = new ReplyKeyboardRemove();
            await _botClient.SendMessage(
                chatId: chatId,
                text: "Введите лимит для трат, который хотите установить:",
                replyMarkup: removeKeyboard,
                cancellationToken: ct);
        }

        public async Task HandleSetLimitInputCommand(long chatId, string text, CancellationToken ct)
        {
            await StateRemover(text, chatId, ct);

            if (!_limit.TryGetValue(chatId, out var state))
            {
                Console.WriteLine($"Не найдено состояние для chatId {chatId}");
                return;
            }

            switch (state.Step)
            {
                case 1 when decimal.TryParse(text, out var amount):

                    var response = await _httpClient.PostAsync(
                        $"/api/limits/set?chatId={chatId}&amount={amount}", null, ct);

                    if (response.IsSuccessStatusCode)
                    {
                        await _botClient.SendMessage(
                            chatId: chatId,
                            text: $"✅ Лимит установлен: {amount}₽",
                            cancellationToken: ct);
                        _limit.Remove(chatId);
                    }
                    else
                    {
                        await _botClient.SendMessage(
                            chatId: chatId,
                            text: "Некорректная сумма лимита",
                            cancellationToken: ct);
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

        public async Task HandleClearLimitCommand(long chatId, CancellationToken ct)
        {
            if (await TryCancelState("/clear", chatId, ct))
                return;


            var response = await _httpClient.GetStringAsync(
                $"/api/limits/clear?chatId={chatId}", ct);

            if (response == "Y")
            {
                var removeKeyboard = new ReplyKeyboardRemove();
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "Лимит успешно сброшен!",
                    replyMarkup: removeKeyboard,
                    cancellationToken: ct);
            }
            else
            {
                var removeKeyboard = new ReplyKeyboardRemove();
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "У вас не установлен лимит.",
                    replyMarkup: removeKeyboard,
                    cancellationToken: ct);
            }
        }


        //---------------------------------GET MY EXPENSES-----------------------------------

        public async Task HandleMyExpensesCommand(long chatId, CancellationToken ct)
        {

            _myExp[chatId] = new MyExpensesCheck
            {
                Step = 1
            };

            Console.WriteLine($"Состояние для {chatId} установлено: {nameof(MyExpensesCheck)}");

            var removeKeyboard = new ReplyKeyboardRemove();
            await _botClient.SendMessage(
                chatId: chatId,
                text: "Введите кол-во последних расходов, которое хотите получить (не более 100):",
                replyMarkup: removeKeyboard,
                cancellationToken: ct);
        }

        public async Task HandleMyExpensesInputCommand(long chatId, string text, CancellationToken ct)
        {
            await StateRemover(text, chatId, ct);

            if (!_myExp.TryGetValue(chatId, out var state))
            {
                Console.WriteLine($"Не найдено состояние для chatId {chatId}");
                return;
            }

            switch (state.Step)
            {
                case 1 when decimal.TryParse(text, out var amount):
                    if (amount > 100)
                    {
                        await _botClient.SendMessage(
                            chatId: chatId,
                            text: "Я же сказал — не более ста😆",
                            cancellationToken: ct);
                    }
                    else if (amount <= 0)
                    {
                        await _botClient.SendMessage(
                            chatId: chatId,
                            text: "Маловато ты просишь😳",
                            cancellationToken: ct);
                    }
                    else
                    {
                        var getResponse = await _httpClient.GetStringAsync(
                            $"/api/expense/format/{chatId}/{amount}");


                        var messageParts = SplitMessage(getResponse, 4000);

                        foreach (var part in messageParts)
                        {
                            await _botClient.SendMessage(
                                chatId: chatId,
                                text: part,
                                cancellationToken: ct);


                            await Task.Delay(300, ct);
                        }

                        _myExp.Remove(chatId);
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

        private List<string> SplitMessage(string message, int maxLength)
        {
            var parts = new List<string>();

            for (int i = 0; i < message.Length; i += maxLength)
            {
                int length = Math.Min(maxLength, message.Length - i);
                parts.Add(message.Substring(i, length));
            }

            return parts;
        }


        //---------------------------------ASKS-----------------------------------



        private async Task AskForDescription(long chatId, CancellationToken ct)
        {
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
                text: "Введите описание расхода (зачем, когда, на кого и т.д.):",
                replyMarkup: replyKeyboard,
                cancellationToken: ct);
        }

        private async Task AskForCategory(long chatId, CancellationToken ct)
        {
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
                text: "Введите категорию расхода:",
                replyMarkup: replyKeyboard,
                cancellationToken: ct);
        }


        //---------------------------------METHODS FOR STATES-----------------------------------



        private async Task ClearAllStates(long chatId, CancellationToken ct)
        {
            _userStates.Remove(chatId);
            _myExp.Remove(chatId);
            _limit.Remove(chatId);
            await _service.Value.ClearAllStatesNoUser(chatId, ct);

            await _botClient.SendMessage(
                chatId: chatId,
                text: "❌ Прошлая команда отменена.\n" +
                "Для использования новой команды пропишите ее еще раз.",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: ct);
        }

        public async Task ClearThisStates(long chatId, CancellationToken ct)
        {
            _userStates.Remove(chatId);
            _myExp.Remove(chatId);
            _limit.Remove(chatId);
        }

        private async Task StateRemover(string text, long chatId, CancellationToken ct)
        {
            bool textIs = text == "/days" || text == "/create" || text == "/weekly"
            || text == "/monthly" || text == "/newcat" || text == "/mycat"
            || text == "/weeklyc" || text == "/monthlyc" || text == "/myexp"
            || text == "/start" || text == "/commands" | text == "/setlimit"
            || text == "/statistic" || text == "/tips" || text == "/clear";
            if (textIs)
            {
                await ClearAllStates(chatId, ct);
            }
        }

        private async Task<bool> TryCancelState(string text, long chatId, CancellationToken ct)
        {
            bool isCommand = text == "/days" || text == "/create" || text == "/weekly"
            || text == "/monthly" || text == "/newcat" || text == "/mycat"
            || text == "/weeklyc" || text == "/monthlyc" || text == "/myexp"
            || text == "/start" || text == "/commands" | text == "/setlimit"
            || text == "/statistic" || text == "/tips" || text == "/clear";

            if (!isCommand)
                return false;

            if (_userStates.ContainsKey(chatId) || _myExp.ContainsKey(chatId))
            {
                await ClearAllStates(chatId, ct);
                return true; 
            }

            return false;
        }


        //---------------------------------CHECK STATES-----------------------------------

        public async Task<bool> isActive(long chatId, CancellationToken ct)
        {
            if (_userStates.TryGetValue(chatId, out var state))
                return true;
            return false;
        }

        public async Task<bool> isActiveExpCheck(long chatId)
        {
            if (_myExp.TryGetValue(chatId, out var state))
                return true;
            return false;
        }

        public async Task<bool> isActiveLimit(long chatId)
        {
            if (_limit.TryGetValue(chatId, out var state))
                return true;
            return false;
        }
    }
}
