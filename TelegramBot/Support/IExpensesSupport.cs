using Telegram.Bot;
using TelegramBot.Config;

namespace TelegramBot.Support
{
    public interface IExpensesSupport
    {
        Task HandleStartCommand(long chatId, CancellationToken ct);
        Task HandleCreateCommand(long chatId, CancellationToken ct);
        Task HandleCheckWeeklyCommand(long chatId, CancellationToken ct);
        Task HandleCheckMonthlyCommand(long chatId, CancellationToken ct);
        Task HandleUserInput(long chatId, string text, CancellationToken ct);
        Task ProcessExpenseCreation(long chatId, string text, string cat, ExpenseCreationState state, CancellationToken ct);
        Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct);
    }
}
