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
    public class CategoryRepository : ICategoryRepository
    {
        private readonly DatabaseConnect _conn;

        public CategoryRepository(DatabaseConnect conn)
        {
            _conn = conn;
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
            var categories = await _conn.Categories
                .Where(c => c.ChatId == chatId)
                .ToListAsync();
            if(categories == null)
            {
                return "Вы пока не добавляли категории." +
                    "\nДля создания категории воспользуйтесь /newcat.";
            }
            else
            {
                var categoriesString = string.Join(", ", categories.Select(c => c.Name));
                return categoriesString;
            }
        }
    }
}
