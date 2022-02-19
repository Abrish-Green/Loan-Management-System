using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LoanManagementSystem.Models;

namespace LoanManagementSystem.Models
{
    public class LoanManagementContext:DbContext
    {
        public LoanManagementContext(DbContextOptions<LoanManagementContext> options):base(options)
        {
        }
        public DbSet<UserAccount> Accounts { get; set; }
        public DbSet<LoanPlan> LoanPlans { get; set; }
        public DbSet<LoanType> LoanTypes { get; set; }
        public DbSet<LoanManagementSystem.Models.Loan> Loan { get; set; }
        public DbSet<LoanManagementSystem.Models.Payment> Payments { get; set; }


    }
}
