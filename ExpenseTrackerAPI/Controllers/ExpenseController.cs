using ExpenseTrackerLibrary.Domain.Entities;
using ExpenseTrackerLibrary.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTrackerAPI.Controllers
{
    [Route("api/expense")]
    [ApiController]
    public class ExpenseController : ControllerBase
    {
        private readonly IExpenseRepository _rep;

        public ExpenseController(IExpenseRepository rep)
        {
            _rep = rep;
        }

        [HttpPost]
        public async Task<IActionResult> CreateExpense
            ([FromBody] Expense command)
        {
            if (!ModelState.IsValid)
            {
                var error = ModelState.Values.SelectMany(e => e.Errors.Select(er => er.ErrorMessage));
                return BadRequest($"Некорректно указаны данные! Ошибка: {error}");
            }
            try
            {
                await _rep.CreateExpense(command);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка: {ex}");
            }
        }


        [HttpGet("format/{chatId}/{count}")]
        public async Task<string> CheckWeeklyExpenses(long chatId, int count)
        {
            var result = await _rep.FormatExpenses(chatId, count);
            return result;
        }


        [HttpGet("checkw/{chatId}")]
        public async Task<decimal> CheckWeeklyExpenses(long chatId)
        {
            var result = await _rep.CheckWeeklyExpenses(chatId);
            return result;
        }

        [HttpGet("checkm/{chatId}")]
        public async Task<decimal> CheckMonthlyExpenses(long chatId)
        {
            var result = await _rep.CheckMonthlyExpenses(chatId);
            return result;
        }

        [HttpGet("custom/{days}/{chatId}")]
        public async Task<decimal> CheckCustomTimeDays(int days, long chatId)
        {
            var result = await _rep.CheckCustomTimeDays(days, chatId);
            return result;
        }
    }
}
