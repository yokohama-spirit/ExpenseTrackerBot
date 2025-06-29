using ExpenseTrackerLibrary.Application.Services;
using ExpenseTrackerLibrary.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTrackerAPI.Controllers
{
    [Route("api/plot")]
    [ApiController]
    public class PlotController : ControllerBase
    {
        private readonly IExpServiceForPlot _expenseService;
        private readonly IPlotService _plotService;

        public PlotController(IExpServiceForPlot expenseService, IPlotService plotService)
        {
            _expenseService = expenseService;
            _plotService = plotService;
        }

        [HttpGet("weekly-plot")]
        public async Task<IActionResult> GetWeeklyExpensesPlot(long chatId)
        {
            var expenses = await _expenseService.GetWeeklyExpensesAsync(chatId);
            var plotBytes = _plotService.GenerateWeeklyExpensesPlot(expenses);

            return File(plotBytes, "image/png");
        }
    }
}
