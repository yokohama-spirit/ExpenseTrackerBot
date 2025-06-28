using ExpenseTrackerLibrary.Application.Services;
using ExpenseTrackerLibrary.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTrackerAPI.Controllers
{
    [Route("api/limits")]
    [ApiController]
    public class LimitsController : ControllerBase
    {
        private readonly IExpenseLimitService _expLimit;
        private readonly IMonthlyLimitRepository _limit;

        public LimitsController(
            IExpenseLimitService expLimit,
            IMonthlyLimitRepository limit)
        {
            _expLimit = expLimit;
            _limit = limit;
        }



        [HttpGet("check")]
        public async Task<ActionResult<LimitCheckResult>> CheckLimit(
[FromQuery] long chatId,
[FromQuery] decimal amount)
        {
            var result = await _expLimit.CheckLimitAfterExpense(chatId, amount);
            return Ok(result);
        }

        [HttpPost("set")]
        public async Task<IActionResult> SetLimit(
[FromQuery] long chatId,
[FromQuery] decimal amount)
        {
            try
            {
                await _limit.SetLimitAsync(chatId, amount);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка: {ex}");
            }
        }
    }
}
