using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpenseTrackerLibrary.Domain.Entities
{
    public class Category
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public long? ChatId { get; set; }
        public string Name { get; set; }
        public List<Expense>? Expenses { get; set; }
    }
}
