using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpenseTrackerLibrary.Domain.Entities
{
    public class LimitCheckResult
    {
        public bool IsLimitExceeded { get; set; }
        public bool IsWarningNeeded { get; set; }
        public decimal? CurrentLimit { get; set; }
        public decimal CurrentSpent { get; set; }
    }
}
