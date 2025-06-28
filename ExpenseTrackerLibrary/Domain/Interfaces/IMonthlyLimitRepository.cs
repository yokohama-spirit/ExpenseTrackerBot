using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpenseTrackerLibrary.Domain.Interfaces
{
    public interface IMonthlyLimitRepository
    {
        Task SetLimitAsync(long chatId, decimal amount);
        Task<decimal?> GetCurrentLimitAsync(long chatId);
        Task<bool> HasLimitAsync(long chatId);
    }
}
