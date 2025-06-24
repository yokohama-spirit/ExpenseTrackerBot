using ExpenseTrackerLibrary.Domain.Entities;
using ExpenseTrackerLibrary.Domain.Interfaces;
using ExpenseTrackerLibrary.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ExpenseTrackerLibrary.Infrastructure.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly DatabaseConnect _conn;
        private readonly IDistributedCache _redisCache;

        public CategoryRepository
            (DatabaseConnect conn,
            IDistributedCache redisCache)
        {
            _conn = conn;
            _redisCache = redisCache;
        }

        public async Task CreateCategory(string cat, long chatId)
        {
            var category = new Category
            {
                Name = cat,
                ChatId = chatId,
            };

            await _conn.Categories.AddAsync(category);
            await _conn.SaveChangesAsync();
        }

        public async Task<string> GetUserCategoriesString(long chatId)
        {
            var cacheKey = $"my_categories:{chatId}";
            var cachedData = await _redisCache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<string>(cachedData);
            }

            var categories = await _conn.Categories
                .Where(c => c.ChatId == chatId)
                .ToListAsync();

            var categoriesString = string.Join(", ", categories.Select(c => c.Name));

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };

            await _redisCache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(categoriesString),
                cacheOptions);

            return categoriesString;

        }

        public async Task<bool> isExistsCategory(string categoryName, long chatId)
        {
            var trimmedName = categoryName.Trim(',').ToLowerInvariant();

            var category = await _conn.Categories
                .Where(c => c.Name.Trim(',').ToLower() == trimmedName && c.ChatId == chatId)
                .FirstOrDefaultAsync();

            return category != null;
        }
    }
}
