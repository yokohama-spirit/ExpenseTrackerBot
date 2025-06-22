using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpenseTrackerLibrary.Domain.Entities
{
    public class Expense
    {
        public string Id {  get; set; } = Guid.NewGuid().ToString();
        public decimal Amount { get; set; }
        public string? Content { get; set; }
        public long? ChatId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
