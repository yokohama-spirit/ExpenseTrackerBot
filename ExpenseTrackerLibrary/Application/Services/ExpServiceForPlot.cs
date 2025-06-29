using ExpenseTrackerLibrary.Domain.Interfaces;
using ExpenseTrackerLibrary.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpenseTrackerLibrary.Application.Services
{
    public class ExpServiceForPlot : IExpServiceForPlot
    {
        private readonly DatabaseConnect _conn;

        public ExpServiceForPlot(DatabaseConnect conn)
        {
            _conn = conn;
        }

        public async Task<Dictionary<DateTime, decimal>> GetWeeklyExpensesAsync(long chatId)
        {
            var today = DateTime.UtcNow.Date;
            var weekAgo = today.AddDays(-7);

            var expenses = await _conn.Expenses
                .Where(e => e.ChatId == chatId && e.CreatedAt >= weekAgo)
                .ToListAsync();

            var dailyExpenses = expenses
                .GroupBy(e => e.CreatedAt.Date)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(e => e.Amount)
                );


            for (var date = weekAgo; date <= today; date = date.AddDays(1))
            {
                dailyExpenses.TryAdd(date, 0);
            }

            return dailyExpenses.OrderBy(e => e.Key).ToDictionary(e => e.Key, e => e.Value);
        }
    }
}
