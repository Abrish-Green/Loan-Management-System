using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LoanManagementSystem.Models
{
    [Keyless]
    public class AllModel
    {
        
        public static List<Loan> Loans { get; set; }
        public static List<LoanPlan> LoanPlans { get; set; }

        public static List<LoanType> LoanType { get; set; }

        public static List<Payment> Payment { get; set; }

        public static List<UserAccount> Account { get; set; }
    }
}
