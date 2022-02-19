using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LoanManagementSystem.Models;
using Scrypt;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using LoanManagementSystem.Helpers;

namespace LoanManagementSystem.Controllers.customer
{
    public class CustomerController : Controller
    {
        private readonly LoanManagementContext _context;

        public CustomerController(LoanManagementContext context)
        {
            _context = context;
        }
       
        public IActionResult Index()
        {
            ViewBag.HomeActive = "active";
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            int? id = HttpContext.Session.GetInt32("userId");


            //if there is a loan
            if (_context.Loan.Any(e => e.UserId == id))
            {
                
                if (_context.Loan.OrderBy(e => e.Id).LastOrDefault(e => e.UserId == id).LoanGrant == "ACCEPTED")
                {
                    //Monthly Payment
                    TempData["monthlyPayment"] = Math.Round(Convert.ToDouble(_context.Loan.OrderBy(e => e.Id).LastOrDefault(e => e.UserId == id).MonthlyPayableAmount),2);
                    
                    //Loan Status

                    TempData["loanStatus"] = _context.Loan.OrderBy(e => e.Id).LastOrDefault(e => e.UserId == id).LoanGrant;

                }
                else
                {
                    TempData["monthlyPayment"] = 0;
                    TempData["loanStatus"] = _context.Loan.OrderBy(e => e.Id).LastOrDefault(e => e.UserId == id).LoanGrant;


                }
                //Next Payment
                TempData["nextPayment"] = DateTime.Now.AddMonths(1).ToString("MMMM dddd");

            }
            else {
                TempData["monthlyPayment"] = 0;
                TempData["loanStatus"] = null;

            }


            //if there is a payment
            if (_context.Payments.Any(e => e.UserId == id))
            {
                //Next Payment Date
                TempData["nextPayment"] = _context.Payments.OrderBy(e => e.Id).LastOrDefault(e => e.UserId == id).NextPaymentDate.ToString("MMMM dd dddd");
                //Total remaining amount of money
                TempData["totalRemainingLoan"] = _context.Payments.OrderBy(e => e.Id).LastOrDefault(e => e.UserId == id).RemainingLoanAmount.ToString();

            }
            else
            {
                TempData["nextPayment"] = "No Active Loan";
                TempData["totalRemainingLoan"] = null;
            }

            return View();
        }
       
        public IActionResult Account()
        {
            ViewBag.AccountActive = "active";
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> UpdateAccount(UserAccount userAccount)
        {
               ViewBag.AccountActive = "active";
               ScryptEncoder encoder = new ScryptEncoder();
               //userAccount = _context.Accounts.Single(e => e.Id == HttpContext.Session.GetInt32("userId"));
               userAccount.User_Password = encoder.Encode(userAccount.User_Password);
               _context.Update(userAccount);
               await _context.SaveChangesAsync();
            TempData["AlertMessage"] = "Account Updated Successfully.";
            TempData["AlertType"] = "success";
            return RedirectToAction(actionName:"Account",controllerName:"customer");
    
        }

        public IActionResult Loan()
        {
            ViewBag.LoanActive = "active";
            var LoanPlan = _context.LoanPlans.ToList();
            var LoanType = _context.LoanTypes.ToList();

            ViewData["plan"] = LoanPlan;
            ViewData["type"] = LoanType;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Loan(Loan loan)
        {
            ViewBag.LoanActive = "active";

            if (ModelState.IsValid)
            {


                //calculate loan
                double interestRate = 0;
                int totalMonth = LoanPlanExists(loan.loanPlanId).Month;
                interestRate = Convert.ToDouble(LoanPlanExists(loan.loanPlanId).Interest);
                double OverDuesPenaltyRate = Convert.ToDouble(LoanPlanExists(loan.loanPlanId).MonthlyOverDuePenalty);
                double p = Convert.ToDouble(loan.loanAmount);
                double r = (interestRate / 100) / 12;
                int n = totalMonth;
                double monthlyUpper = p * (r) * Math.Pow(1 + (r), n);
                double monthlyLower = Math.Pow(1 + r, n) - 1;
                

                double monthlyRate = (interestRate / 12) / 100;
                double MonthlyPayableAmount = monthlyUpper / monthlyLower;
                OverDuesPenaltyRate = OverDuesPenaltyRate / 12;
                OverDuesPenaltyRate = OverDuesPenaltyRate / 100;
                double TotalPayableAmount = MonthlyPayableAmount * totalMonth;
                double MonthlyPenalty = p * OverDuesPenaltyRate;


                loan.UserId = (int)HttpContext.Session.GetInt32("userId");
                loan.loanDate = DateTime.Now;
                loan.TotalPayableAmount = Convert.ToDecimal(TotalPayableAmount);
                loan.MonthlyPayableAmount = Convert.ToDecimal(MonthlyPayableAmount);
                loan.MonthlyPenalty = Convert.ToDecimal(MonthlyPenalty);
                loan.LoanGrant = "PENDING";
                loan.RejectionReason = "NONE";
                _context.Add(loan);
                await _context.SaveChangesAsync();
                return RedirectToAction(actionName:"ViewLoan",controllerName:"customer");
            }
            return View();// RedirectToAction(actionName: "Loan", controllerName: "customer");
        }

        public async Task<IActionResult> ViewLoan()
        {
            ViewBag.LoanActive = "active";
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            return View(await _context.Loan.ToListAsync());
            
        }


        // GET: Users/Edit/5
        public async Task<IActionResult> EditLoan(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loan = await _context.Loan.FindAsync(id);
            if (loan == null)
            {
                return NotFound();
            }
            var LoanPlan = _context.LoanPlans.ToList();
            var LoanType = _context.LoanTypes.ToList();

            ViewData["plan"] = LoanPlan;
            ViewData["type"] = LoanType;

            return View(loan);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLoan(int id, Loan loan)
        {
            if (id != loan.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                { double interestRate = 0;
                int totalMonth = LoanPlanExists(loan.loanPlanId).Month;
                interestRate = Convert.ToDouble(LoanPlanExists(loan.loanPlanId).Interest);
                double OverDuesPenaltyRate = Convert.ToDouble(LoanPlanExists(loan.loanPlanId).MonthlyOverDuePenalty);
                double p = Convert.ToDouble(loan.loanAmount);
                double r = (interestRate / 100) / 12;
                int n = totalMonth;
                double monthlyUpper = p * (r) * Math.Pow(1 + (r), n);
                double monthlyLower = Math.Pow(1 + r, n) - 1;
                

                double monthlyRate = (interestRate / 12) / 100;
                double MonthlyPayableAmount = monthlyUpper / monthlyLower;
                OverDuesPenaltyRate = OverDuesPenaltyRate / 12;
                OverDuesPenaltyRate = OverDuesPenaltyRate / 100;
                double TotalPayableAmount = MonthlyPayableAmount * totalMonth;
                double MonthlyPenalty = p * OverDuesPenaltyRate;


                loan.UserId = (int)HttpContext.Session.GetInt32("userId");
                loan.loanDate = DateTime.Now;
                loan.TotalPayableAmount = Convert.ToDecimal(TotalPayableAmount);
                loan.MonthlyPayableAmount = Convert.ToDecimal(MonthlyPayableAmount);
                loan.MonthlyPenalty = Convert.ToDecimal(MonthlyPenalty);
                loan.LoanGrant = "PENDING";
                loan.RejectionReason = "NONE";
                    _context.Update(loan);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    
                }
                return RedirectToAction(actionName:"ViewLoan", controllerName:"Customer");
            }
            return View(loan);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> DetailLoan(int? id)
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            if (id == null)
            {
                return NotFound();
            }

            var loan = await _context.Loan
                .FirstOrDefaultAsync(m => m.Id == id);
            if (loan == null)
            {
                return NotFound();
            }

            return View(loan);
        }


        // GET: Users/Delete/5
        public async Task<IActionResult> DeleteLoan(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loan = await _context.Loan
                .FirstOrDefaultAsync(m => m.Id == id);
            if (loan == null)
            {
                return NotFound();
            }

            return View(loan);
        }
        // POST: Users/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLoan(int id)
        {
            TempData["AlertType"] = "success";
            TempData["AlertMessage"] = "Loan Request Has Been Deleted Successfully";
            var loan = await _context.Loan.FindAsync(id);
            _context.Loan.Remove(loan);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Payment()
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            return View(await _context.Payments.ToListAsync());
        }
        public IActionResult Report()
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            //Monthly Loans
            int? id = HttpContext.Session.GetInt32("userId");
            //Monthly active Loans
            TempData["AcceptedLoan"] = _context.Loan.Where(e => e.loanDate.Year == DateTime.Now.Year).Where(e => e.LoanGrant == "ACCEPTED" && e.UserId == id).Count();
            //Monthly Closed Loans
            TempData["RejectedLoan"] = _context.Loan.Where(e => e.loanDate.Year == DateTime.Now.Year).Where(e => e.LoanGrant == "REJECTED" && e.UserId == id).Count();
            //Monthly profit
            TempData["CoveredLoan"] = _context.Payments.Where(e => e.LoanCovered == true && e.UserId == id).Count() ;
            //Monthly Range
            //Monthly Active Users
            TempData["TotalMoneyPaid"] = _context.Payments.Where(e=> e.UserId == id).Select(e => e.PayedAmount).Sum();
            //Monthly Expected Payments
            TempData["TotalPenaltyMoney"] = _context.Payments.Where(e => e.UserId == id).Select(e => e.PenaltyPaymentAmount).Sum();
            //Monthly Payed Loans
           

            return View();
        }

        public IActionResult Logout()
        {
            return RedirectToAction(actionName:"Logout", controllerName:"Login");
        }
        public LoanPlan LoanPlanExists(int id)
        {
            return _context.LoanPlans.FirstOrDefault(e => e.Id == id);
        }

    }
}
