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


        public async Task<string> GenerateMonthlyStats(long chatId)
        {
            var cacheKey = $"stats:{chatId}";
            var cachedData = await _redisCache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<string>(cachedData);
            }

            var currentMonthTotal = await GetCurrentMonthTotalAsync(chatId);
            var previousMonthTotal = await GetPreviousMonthTotalAsync(chatId);

            var currentMonthCategories = await GetCurrentMonthCategoriesAsync(chatId);
            var previousMonthCategories = await GetPreviousMonthCategoriesAsync(chatId);

            var sb = new StringBuilder();


            if (previousMonthTotal == 0)
            {
                sb.AppendLine($"📊 В этом месяце вы уже потратили: {currentMonthTotal}₽");
            }
            else
            {
                var difference = currentMonthTotal - previousMonthTotal;
                var differencePercent = previousMonthTotal != 0
                    ? Math.Round(difference / previousMonthTotal * 100, 2)
                    : 0;

                var trend = difference >= 0 ? "больше" : "меньше";
                sb.AppendLine($"📊 В этом месяце вы потратили на {Math.Abs(differencePercent)}% {trend}, чем в прошлом!");
            }

            sb.AppendLine();


            var allCategories = currentMonthCategories.Keys.Union(previousMonthCategories.Keys).Distinct();

            foreach (var category in allCategories)
            {
                currentMonthCategories.TryGetValue(category, out var currentSpent);
                previousMonthCategories.TryGetValue(category, out var prevMonthSpent);

                if (prevMonthSpent == 0)
                {
                    if (currentSpent > 0)
                    {
                        sb.AppendLine($"💰 На категорию {category} в этом месяце потрачено: {currentSpent}₽");
                    }
                    continue;
                }

                if (currentSpent == 0)
                {
                    sb.AppendLine($"💤 В этом месяце по категории {category} трат не было");
                    continue;
                }

                var difference = currentSpent - prevMonthSpent;
                var differencePercent = prevMonthSpent != 0
                    ? Math.Round(difference / prevMonthSpent * 100, 2)
                    : 0;

                var trend = difference >= 0 ? "потратили больше 😢" : "сэкономили 😎";
                sb.AppendLine($"📌 На категорию {category} в этом месяце {trend} на {Math.Abs(differencePercent)}%");
            }

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };

            await _redisCache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(sb.ToString()),
                cacheOptions);

            return sb.ToString();
        }

        private async Task<decimal> GetCurrentMonthTotalAsync(long chatId)
        {
            var now = DateTime.UtcNow;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddTicks(-1);

            return await _conn.Expenses
                .Where(e => e.ChatId == chatId && e.CreatedAt >= firstDayOfMonth && e.CreatedAt <= lastDayOfMonth)
                .SumAsync(e => e.Amount);
        }

        private async Task<decimal> GetPreviousMonthTotalAsync(long chatId)
        {
            var now = DateTime.UtcNow.AddMonths(-1);
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddTicks(-1);

            return await _conn.Expenses
                .Where(e => e.ChatId == chatId && e.CreatedAt >= firstDayOfMonth && e.CreatedAt <= lastDayOfMonth)
                .SumAsync(e => e.Amount);
        }

        private async Task<Dictionary<string, decimal>> GetCurrentMonthCategoriesAsync(long chatId)
        {
            var now = DateTime.UtcNow;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddTicks(-1);

            return await _conn.Expenses
                .Where(e => e.ChatId == chatId && e.CreatedAt >= firstDayOfMonth && e.CreatedAt <= lastDayOfMonth)
                .Include(e => e.Categories)
                .GroupBy(e => e.Categories.FirstOrDefault().Name ?? "Без категории")
                .Select(g => new { Category = g.Key, Total = g.Sum(e => e.Amount) })
                .ToDictionaryAsync(x => x.Category, x => x.Total);
        }

        private async Task<Dictionary<string, decimal>> GetPreviousMonthCategoriesAsync(long chatId)
        {
            var now = DateTime.UtcNow.AddMonths(-1);
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddTicks(-1);

            return await _conn.Expenses
                .Where(e => e.ChatId == chatId && e.CreatedAt >= firstDayOfMonth && e.CreatedAt <= lastDayOfMonth)
                .Include(e => e.Categories)
                .GroupBy(e => e.Categories.FirstOrDefault().Name ?? "Без категории")
                .Select(g => new { Category = g.Key, Total = g.Sum(e => e.Amount) })
                .ToDictionaryAsync(x => x.Category, x => x.Total);
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

                sb.AppendLine($"Добавлено: {expense.CreatedAt.AddHours(3).ToString("HH:mm, dd MMMM yyyy", culture)}");
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
