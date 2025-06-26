namespace TelegramBot.Services
{
    public interface ITelegramBotService
    {
        Task StartAsync(CancellationToken cancellationToken);
        Task<bool> isActive(long chatId, CancellationToken ct);
        Task ClearAllStatesNoUser(long chatId, CancellationToken ct);
    }
}
