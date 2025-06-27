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
        private readonly TelegramBotConfig _config;
        private readonly IExpensesSupport _ex;
        private readonly ICategorySupport _cat;

        public TelegramBotService
            (TelegramBotConfig config,
            IExpensesSupport ex,
            ICategorySupport cat)
        {
            _config = config;
            _botClient = new TelegramBotClient(_config.Token);
            _httpClient = new HttpClient { BaseAddress = new Uri(_config.ApiBaseUrl) };
            _ex = ex;
            _cat = cat;
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


            if (await _cat.isActiveUserCatStates(chatId))
            {
                await _cat.HandleCategoryUserInput(chatId, text, ct);
                return;
            }

            if (await _cat.isActiveCheckByCatStates(chatId))
            {
                await _cat.HandleWeeklyInputCommand(chatId, text, ct);
                return;
            }

            if (await _cat.isActiveCheckByCatStatesM(chatId))
            {
                await _cat.HandleMonthlyInputCommand(chatId, text, ct);
                return;
            }

            if (await _cat.isActiveDaysStates(chatId))
            {
                await _cat.HandleDaysInputCommand(chatId, text, ct);
                return;
            }

            if (await _ex.isActiveExpCheck(chatId))
            {
                await _ex.HandleMyExpensesInputCommand(chatId, text, ct);
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
                    await _cat.HandleCreateCommand(chatId, ct);
                    break;

                case "/mycat":
                    await _cat.HandleMyCategoriesCommand(chatId, ct);
                    break;

                case "/weeklyc":
                    await _cat.HandleWeeklyCommand(chatId, ct);
                    break;

                case "/monthlyc":
                    await _cat.HandleMonthlyCommand(chatId, ct);
                    break;

                case "/days":
                    await _cat.HandleDaysCommand(chatId, ct);
                    break;

                case "/myexp":
                    await _ex.HandleMyExpensesCommand(chatId, ct);
                    break;

                default:
                    await _ex.HandleUserInput(chatId, text, ct);
                    break;
            }

        }
        private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            return Task.CompletedTask;
        }

    }
}
