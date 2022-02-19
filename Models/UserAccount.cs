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
    public class UserAccount
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [DisplayName("User Name")]
        public string User_Name { get; set; }
        [Required]
        [DisplayName("Password")]
        public string User_Password { get; set; }
        
        [DefaultValue(false)]
        public bool IsAdmin { get; set; }

    }
}
