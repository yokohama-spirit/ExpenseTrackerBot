using ExpenseTrackerLibrary.Domain.Entities;
using ExpenseTrackerLibrary.Domain.Interfaces;
using ExpenseTrackerLibrary.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpenseTrackerLibrary.Infrastructure.Repositories
{
    public class ExpenseRepository : IExpenseRepository
    {
        private readonly DatabaseConnect _conn;

        public ExpenseRepository(DatabaseConnect conn)
        {
            _conn = conn;
        }
        public async Task<decimal> CheckMonthlyExpenses(long chatId)
        {
            var expenses = _conn.Expenses;

            var now = DateTime.UtcNow;

            var startOfWeek = now.AddMonths(-1);

            var totalAmount = expenses
                .Where(e => e.CreatedAt >= startOfWeek && e.CreatedAt <= now && e.ChatId == chatId)
                .Sum(e => e.Amount);

            return totalAmount;
        }

        public async Task<decimal> CheckWeeklyExpenses(long chatId)
        {
            var expenses = _conn.Expenses; 

            var now = DateTime.UtcNow;

            var startOfWeek = now.AddDays(-7);

            var totalAmount = expenses
                .Where(e => e.CreatedAt >= startOfWeek && e.CreatedAt <= now && e.ChatId == chatId)
                .Sum(e => e.Amount);

            return totalAmount;
        }

        public async Task CreateExpense(Expense ex)
        {
           await _conn.Expenses.AddAsync(ex);
           await _conn.SaveChangesAsync();
        }
    }
}
