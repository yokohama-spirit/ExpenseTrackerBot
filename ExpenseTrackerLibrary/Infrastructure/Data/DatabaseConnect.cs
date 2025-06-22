using ExpenseTrackerLibrary.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpenseTrackerLibrary.Infrastructure.Data
{
    public class DatabaseConnect : DbContext
    {
        public DatabaseConnect(DbContextOptions<DatabaseConnect> options) : base(options) { }

        public DbSet<Expense> Expenses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Expense>().HasKey(p => p.Id);
        }
    }
}
