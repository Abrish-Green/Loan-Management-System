using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LoanManagementSystem.Models
{
    public class LoanPlan
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [DisplayName("Loan Plan in Month")]
        public int Month { get; set; }
        [Required]
        [DisplayName("Loan Interest")]
        public decimal Interest { get; set; }
        [Required]
        [DisplayName("Monthly Over Due Penalty")]
        public decimal MonthlyOverDuePenalty { get; set; }
    }
}
