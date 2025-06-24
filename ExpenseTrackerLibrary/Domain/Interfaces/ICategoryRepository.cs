using ExpenseTrackerLibrary.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpenseTrackerLibrary.Domain.Interfaces
{
    public interface ICategoryRepository
    {
        Task CreateCategory(string cat, long chatId);
        Task<string> GetUserCategoriesString(long chatId);
        Task<bool> isExistsCategory(string categoryName, long chatId);
    }
}
