using Telegram.Bot;
using TelegramBot.Config;

namespace TelegramBot.Interfaces
{
    public interface IExpensesSupport
    {
        Task HandleStartCommand(long chatId, CancellationToken ct);
        Task HandleCreateCommand(long chatId, CancellationToken ct);
        Task HandleCheckWeeklyCommand(long chatId, CancellationToken ct);
        Task HandleCheckMonthlyCommand(long chatId, CancellationToken ct);
        Task HandleUserInput(long chatId, string text, CancellationToken ct);
        Task HandleCommsCommand(long chatId, CancellationToken ct);
        Task<bool> isActive(long chatId, CancellationToken ct);
        Task HandleMyExpensesCommand(long chatId, CancellationToken ct);
        Task HandleMyExpensesInputCommand(long chatId, string text, CancellationToken ct);
        Task<bool> isActiveExpCheck(long chatId);
        Task HandleStatisticCommand(long chatId, CancellationToken ct);
        Task ClearThisStates(long chatId, CancellationToken ct);
        Task HandleSetLimitCommand(long chatId, CancellationToken ct);
        Task HandleSetLimitInputCommand(long chatId, string amountText, CancellationToken ct);
        Task<bool> isActiveLimit(long chatId);
        Task HandleGetTipsCommand(long chatId, CancellationToken ct);
        Task HandleClearLimitCommand(long chatId, CancellationToken ct);
        Task HandleWeeklyExpensesPlot(long chatId, CancellationToken ct);
    }
}
