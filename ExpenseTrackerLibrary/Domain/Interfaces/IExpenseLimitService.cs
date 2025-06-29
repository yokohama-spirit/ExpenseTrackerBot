using ExpenseTrackerLibrary.Application.Services;
using ExpenseTrackerLibrary.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpenseTrackerLibrary.Domain.Interfaces
{
    public interface IExpenseLimitService
    {
        Task<LimitCheckResult> CheckLimitAfterExpense(long chatId, decimal expenseAmount);
    }
}
