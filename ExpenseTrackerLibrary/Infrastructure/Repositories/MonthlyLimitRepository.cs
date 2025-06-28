using ExpenseTrackerLibrary.Domain.Entities;
using ExpenseTrackerLibrary.Domain.Interfaces;
using ExpenseTrackerLibrary.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpenseTrackerLibrary.Infrastructure.Repositories
{
    public class MonthlyLimitRepository : IMonthlyLimitRepository
    {
        private readonly DatabaseConnect _conn;

        public MonthlyLimitRepository(DatabaseConnect conn)
        {
            _conn = conn;
        }

        public async Task SetLimitAsync(long chatId, decimal amount)
        {
            var existing = await _conn.Limits
                .FirstOrDefaultAsync(x => x.ChatId == chatId);

            if (existing != null)
            {
                existing.Amount = amount;
            }
            else
            {
                await _conn.Limits.AddAsync(new MonthlyLimit
                {
                    ChatId = chatId,
                    Amount = amount
                });
            }

            await _conn.SaveChangesAsync();
        }

        public async Task<decimal?> GetCurrentLimitAsync(long chatId)
        {
            return await _conn.Limits
                .Where(x => x.ChatId == chatId)
                .Select(x => x.Amount)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> HasLimitAsync(long chatId)
        {
            return await _conn.Limits
                .AnyAsync(x => x.ChatId == chatId);
        }
    }
}
