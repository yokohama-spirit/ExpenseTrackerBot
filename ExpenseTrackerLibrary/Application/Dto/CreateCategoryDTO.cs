using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpenseTrackerLibrary.Application.Dto
{
    public class CreateCategoryDTO
    {
        public required long ChatId { get; set; }
        public required string Name { get; set; }
    }
}
