using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LoanManagementSystem.Models
{
    public class LoanType
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [DisplayName("Loan Type")]
        public string LoanTypeName { get; set; }
        [Required]
        [DisplayName("Loan Descritption")]
        public string LoanDescription { get; set; }
       
    }
}
