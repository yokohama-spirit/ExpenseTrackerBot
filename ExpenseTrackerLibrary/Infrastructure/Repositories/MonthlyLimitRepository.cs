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

        public async Task SetLimit(long chatId, decimal amount)
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


        public async Task<string> ClearLimit(long chatId)
        {
            var existing = await _conn.Limits
                .FirstOrDefaultAsync(x => x.ChatId == chatId);

            if (existing != null)
            {
                _conn.Limits.Remove(existing);
                await _conn.SaveChangesAsync();
                return "Y";
            }
            else
            {
                return "N";
            }
        }

        public async Task<decimal?> GetCurrentLimit(long chatId)
        {
            return await _conn.Limits
                .Where(x => x.ChatId == chatId)
                .Select(x => x.Amount)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> HasLimit(long chatId)
        {
            return await _conn.Limits
                .AnyAsync(x => x.ChatId == chatId);
        }
    }
}
