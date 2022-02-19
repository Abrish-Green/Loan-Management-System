using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LoanManagementSystem.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [DisplayName("Payment Date")]
      
        public DateTime PayedDate { get; set; }
        [Required]
        [DisplayName("Payment Month")]
        public DateTime PayedMonth { get; set; }
        [Required]
        [DisplayName("Payment Amount")]
        public double PayedAmount { get; set; }
        [DisplayName("Remaining Loan Amount")]
        public double RemainingLoanAmount { get; set; }
        [DisplayName("Remaining Month Payment")]
        public double RemainingMonthPayment { get; set; }
        [DisplayName("Penalty Payment Amount")]
        public double PenaltyPaymentAmount { get; set; }
        [DisplayName("Next Payment Date")]
        public DateTime NextPaymentDate { get; set; }
        [DisplayName("Loan Status")]
        public string LoanStatus { get; set; }
        [DisplayName("Loan Covered")]
        public bool LoanCovered { get; set; }
        [DisplayName("User Id")]
        public int UserId { get; set; }

    }
}
