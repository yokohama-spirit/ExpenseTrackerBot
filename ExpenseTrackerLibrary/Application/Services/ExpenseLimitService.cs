using ExpenseTrackerLibrary.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpenseTrackerLibrary.Application.Services
{
    public class ExpenseLimitService : IExpenseLimitService
    {
        private readonly IMonthlyLimitRepository _limitRepo;
        private readonly IExpenseRepository _expenseRepo;

        public ExpenseLimitService(IMonthlyLimitRepository limitRepo, IExpenseRepository expenseRepo)
        {
            _limitRepo = limitRepo;
            _expenseRepo = expenseRepo;
        }

        public async Task<LimitCheckResult> CheckLimitAfterExpense(long chatId, decimal expenseAmount)
        {
            var result = new LimitCheckResult();

            if (!await _limitRepo.HasLimitAsync(chatId))
                return result;

            var limit = await _limitRepo.GetCurrentLimitAsync(chatId);
            var monthlyTotal = await _expenseRepo.GetCurrentMonthTotalAsync(chatId) + expenseAmount;

            result.IsLimitExceeded = monthlyTotal >= limit;
            result.IsWarningNeeded = monthlyTotal >= limit * 0.75m;
            result.CurrentLimit = limit;
            result.CurrentSpent = monthlyTotal;

            return result;
        }
    }

    public class LimitCheckResult
    {
        public bool IsLimitExceeded { get; set; }
        public bool IsWarningNeeded { get; set; }
        public decimal? CurrentLimit { get; set; }
        public decimal CurrentSpent { get; set; }
    }
}
