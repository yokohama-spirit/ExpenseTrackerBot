namespace TelegramBot.Support
{
    public interface ICategorySupport
    {
        Task HandleCreateCommand(long chatId, CancellationToken ct);
        Task HandleCategoryUserInput(long chatId, string text, CancellationToken ct);
        Task HandleMyCategoriesCommand(long chatId, CancellationToken ct);
        Task HandleWeeklyCommand(long chatId, CancellationToken ct);
        Task HandleWeeklyInputCommand(long chatId, string text, CancellationToken ct);
        Task HandleMonthlyCommand(long chatId, CancellationToken ct);
        Task HandleMonthlyInputCommand(long chatId, string text, CancellationToken ct);
        Task HandleDaysCommand(long chatId, CancellationToken ct);
        Task HandleDaysInputCommand(long chatId, string text, CancellationToken ct);
        Task ClearAllStatesNoUser(long chatId, CancellationToken ct);
        Task<bool> isActive(long chatId, CancellationToken ct);

        Task<bool> isActiveUserCatStates(long chatId);
        Task<bool> isActiveCheckByCatStates(long chatId);
        Task<bool> isActiveCheckByCatStatesM(long chatId);
        Task<bool> isActiveDaysStates(long chatId);
        Task ClearAllStates(long chatId, CancellationToken ct);
    }
}
