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
    public class Loan
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [DisplayName("First Name")]
        public string FirstName { get; set; }
        [Required]
        [DisplayName("Last Name")]
        public string LastName { get; set; }
        [Required]
        [DisplayName("Middle Name")]
        public string MiddleName { get; set; }


        [Required]
        [DisplayName("Sex")]
        public string Sex { get; set; }
        
        [DisplayName("Email")]
        [Required(ErrorMessage = "The Email Address is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }

        [Required]
        [DisplayName("Phone Number")]
        public string Phone { get; set; }
        [Required]
        [DisplayName("Loan Plan")]
        public int loanPlanId { get; set; }
        [Required]
        [DisplayName("Salary")]
        public decimal Salary { get; set; }
        [Required]
        [DisplayName("Loan Type")]
        public int loanTypeId { get; set; }
        [Required]
        [DisplayName("Loan Purpose")]
        public string loanPurpose { get; set; }
        [Required]
        [DisplayName("Loan Amount")]
        public decimal loanAmount { get; set; }

        [DefaultValue("PENDING")]
        [DisplayName("Loan Grant")]
        public string LoanGrant { get; set; }

        [DisplayName("Loan Date")]
        public DateTime loanDate { get; set; }
        [DefaultValue(0)]
        [DisplayName("Total Payable Amount")]
        public decimal TotalPayableAmount { get; set; }
        [DefaultValue(0)]
        [DisplayName("Monthly Payable Amount")]
        public decimal MonthlyPayableAmount { get; set; }
        [DefaultValue(0)]
        [DisplayName("Monthly Penalty")]
        public decimal MonthlyPenalty { get; set; }
        [DisplayName("Rejection Reason")]
        [DefaultValue("None")]
        public string RejectionReason { get; set; }
        [DisplayName("User Id")]
        public int UserId { get; set; }

    }
}
