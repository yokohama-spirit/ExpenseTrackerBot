using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpenseTrackerLibrary.Domain.Interfaces
{
    public interface IPlotService
    {
        byte[] GenerateWeeklyExpensesPlot(Dictionary<DateTime, decimal> dailyExpenses);
    }
}
