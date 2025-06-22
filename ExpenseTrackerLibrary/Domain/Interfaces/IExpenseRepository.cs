using ExpenseTrackerLibrary.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpenseTrackerLibrary.Domain.Interfaces
{
    public interface IExpenseRepository
    {
        Task CreateExpense(Expense ex);
        Task<decimal> CheckWeeklyExpenses(long chatId);
        Task<decimal> CheckMonthlyExpenses(long chatId);
    }
}
