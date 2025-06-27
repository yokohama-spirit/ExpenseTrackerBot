using ExpenseTrackerLibrary.Domain.Entities;
using ExpenseTrackerLibrary.Domain.Interfaces;
using ExpenseTrackerLibrary.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ExpenseTrackerLibrary.Infrastructure.Repositories
{
    public class ExpenseRepository : IExpenseRepository
    {
        private readonly DatabaseConnect _conn;
        private readonly IDistributedCache _redisCache;

        public ExpenseRepository
            (DatabaseConnect conn,
            IDistributedCache redisCache)
        {
            _conn = conn;
            _redisCache = redisCache;
        }

        public async Task<decimal> CheckCustomTimeDays(int days, long chatId)
        {
            var cacheKey = $"custom:{chatId}:{days}";
            var cachedData = await _redisCache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<decimal>(cachedData);
            }

            var expenses = _conn.Expenses;

            var now = DateTime.UtcNow;

            var startOfMonth = now.AddDays(-days);

            var totalAmount = expenses
                .Where(e => e.CreatedAt >= startOfMonth && e.CreatedAt <= now && e.ChatId == chatId)
                .Sum(e => e.Amount);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
            };

            await _redisCache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(totalAmount),
                cacheOptions);

            return totalAmount;
        }

        public async Task<decimal> CheckMonthlyExpenses(long chatId)
        {
            var cacheKey = $"monthly:{chatId}";
            var cachedData = await _redisCache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<decimal>(cachedData);
            }

            var expenses = _conn.Expenses;

            var now = DateTime.UtcNow;

            var startOfMonth = now.AddMonths(-1);

            var totalAmount = expenses
                .Where(e => e.CreatedAt >= startOfMonth && e.CreatedAt <= now && e.ChatId == chatId)
                .Sum(e => e.Amount);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };

            await _redisCache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(totalAmount),
                cacheOptions);

            return totalAmount;
        }

        public async Task<decimal> CheckWeeklyExpenses(long chatId)
        {
            var cacheKey = $"weekly:{chatId}";
            var cachedData = await _redisCache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<decimal>(cachedData);
            }

            var expenses = _conn.Expenses;

            var now = DateTime.UtcNow;

            var startOfWeek = now.AddDays(-7);

            var totalAmount = expenses
                .Where(e => e.CreatedAt >= startOfWeek && e.CreatedAt <= now && e.ChatId == chatId)
                .Sum(e => e.Amount);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };

            await _redisCache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(totalAmount),
                cacheOptions);

            return totalAmount;
        }

        public async Task<string> FormatExpenses(long chatId, int count)
        {
            var expenses = await _conn.Expenses
                .Where(e => e.ChatId == chatId)
                .OrderByDescending(e => e.CreatedAt)
                .Take(count)
                .Include(e => e.Categories) 
                .ToListAsync();

            if (expenses == null || !expenses.Any())
                return "Нет данных о расходах";

            var sb = new StringBuilder();
            var culture = new CultureInfo("ru-RU");

            foreach (var expense in expenses)
            {
                sb.AppendLine($"Кол-во: {expense.Amount}₽");
                sb.AppendLine($"Описание: {expense.Content ?? "Не указано"}");

                var category = expense.Categories?.FirstOrDefault()?.Name ?? "Не указана";
                sb.AppendLine($"Категория: {category}");

                sb.AppendLine($"Добавлено: {expense.CreatedAt.ToString("HH:mm, dd MMMM yyyy", culture)}");
                sb.AppendLine("--------------------------------------");
            }

            return sb.ToString();
        }

        public async Task CreateExpense(Expense ex)
        {
            await _conn.Expenses.AddAsync(ex);
            await _conn.SaveChangesAsync();
        }
    }
}
