using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpenseTrackerLibrary.Domain.Interfaces
{
    public interface IMonthlyLimitRepository
    {
        Task SetLimit(long chatId, decimal amount);
        Task<string> ClearLimit(long chatId);
        Task<decimal?> GetCurrentLimit(long chatId);
        Task<bool> HasLimit(long chatId);
    }
}
