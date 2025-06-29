using ExpenseTrackerLibrary.Domain.Interfaces;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpenseTrackerLibrary.Application.Services
{
    public class PlotService : IPlotService
    {
        public byte[] GenerateWeeklyExpensesPlot(Dictionary<DateTime, decimal> dailyExpenses)
        {
            // Создаем график
            var plt = new Plot();

            // Подготавливаем данные
            var dates = dailyExpenses.Keys.OrderBy(d => d).ToArray();
            var dateLabels = dates.Select(d => d.ToString("dd.MM")).ToArray();
            var amounts = dailyExpenses.Values.Select(a => (double)a).ToArray();
            var positions = Enumerable.Range(0, amounts.Length).Select(i => (double)i).ToArray();

            // Добавляем столбцы
            var bars = plt.Add.Bars(positions, amounts);
            bars.Color = Colors.Blue;

            // Настраиваем заголовок
            plt.Title("💸 Недельные расходы", size: 21);

            // Настраиваем подписи осей
            plt.YLabel("Сумма (RUB)");
            plt.XLabel("День");

            // Настраиваем подписи на оси X
            plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(positions, dateLabels);

            // Добавляем подписи значений над столбцами
            for (int i = 0; i < amounts.Length; i++)
            {
                var txt = plt.Add.Text(
                    text: amounts[i].ToString("N0"),
                    x: positions[i],
                    y: amounts[i] * 1.05
                );
                txt.LabelFontColor = Colors.Black;
                txt.LabelFontSize = 12;
                txt.LabelBold = true;
            }

            // Автомасштабирование
            plt.Axes.AutoScale();

            // Сохраняем в виде изображения
            return plt.GetImageBytes(800, 400);
        }
    }
}
