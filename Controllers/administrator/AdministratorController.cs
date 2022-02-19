using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using LoanManagementSystem.Helpers;
using LoanManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Scrypt;

namespace LoanManagementSystem.Controllers.administrator
{
    public class AdministratorController : Controller
    {
        private readonly LoanManagementContext _context;

        public AdministratorController(LoanManagementContext context)
        {
            _context = context;
        }
       


        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }

            //today loan requests
            TempData["todaysLoanRequest"] = _context.Loan.Where(e => e.loanDate.Day == DateTime.Now.Day && e.loanDate.Year == DateTime.Now.Year).Where(e => e.LoanGrant == "PENDING").Count();

            //Monthly active Loans
            TempData["CoveredLoan"] = _context.Payments.Where(e => e.PayedDate.Month == DateTime.Now.Month).Where(e => e.LoanCovered == true).Count();
            //Monthly Closed Loans
            TempData["registedUsers"] = _context.Accounts.Where(e => e.IsAdmin == false).Count();

            //active loans
            TempData["activeLoans"] = _context.Payments.Where(e => e.PayedDate.Year == DateTime.Now.Year && e.LoanCovered == false).Count();

            return View();
        }
        public async Task<IActionResult> Loan()
        {
            ViewBag.LoanActive = "active";
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            return View(await _context.Loan.ToListAsync());
        }

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

            var loan = await _context.Loan.FirstOrDefaultAsync(m => m.Id == id);
            if (loan == null)
            {
                return NotFound();
            }

            return View(loan);
        }

        public async Task<IActionResult> AcceptLoan(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loan = await _context.Loan.FirstOrDefaultAsync(m => m.Id == id);
            if (loan == null)
            {
                return NotFound();
            }

            loan.LoanGrant = "ACCEPTED";
            loan.RejectionReason = "NONE";
            _context.Update(loan);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Loan));
        }

        public async Task<IActionResult> RejectLoan(int? id)
        {
            ViewBag.LoanActive = "active";
            var loan = await _context.Loan.FirstOrDefaultAsync(m => m.Id == id);
            return View(loan);
        }

        [HttpPost]
        public async Task<IActionResult> RejectLoan(Loan loan)
        {
           
            var loanUpdated = await _context.Loan.FirstOrDefaultAsync(m => m.Id == loan.Id);
            if (loan == null)
            {
                return NotFound();
            }

            loanUpdated.LoanGrant = "REJECTED";
            loanUpdated.RejectionReason = loan.RejectionReason;
            _context.Update(loanUpdated);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Loan));
        }

        public IActionResult Payment()
        {
            ViewBag.AdminPaymentActive = "active";
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            return View();
        }

        public async Task<IActionResult> PayeeCustomers()
        {
            ViewBag.AdminPaymentActive = "active";
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            return View(await _context.Accounts.ToListAsync());
        }

        public async Task<IActionResult> MakePayment(int id,Payment payment)
        {
            ViewBag.AdminPaymentActive = "active";

            if (!_context.Loan.Any(e => e.UserId == id))
            {
                TempData["AlertType"] = "danger";
                TempData["AlertMessage"] = "No Loan Has be Requested by this Customer yet";
                return RedirectToAction(nameof(PayeeCustomers));
            }

            if (_context.Payments.Any(e => e.UserId == id)) {
                //had previous payment
                payment.PayedDate = DateTime.Now;
                payment.PayedMonth = DateTime.Now;
                payment.PayedAmount = 0;
                var UserPayment = _context.Payments.OrderBy(e=>e.Id).LastOrDefault(e => e.UserId == id);
                payment.RemainingLoanAmount = UserPayment.RemainingLoanAmount;
                payment.RemainingMonthPayment = UserPayment.RemainingMonthPayment;
                payment.PenaltyPaymentAmount = UserPayment.PenaltyPaymentAmount;

                payment.NextPaymentDate = UserPayment.NextPaymentDate;
                payment.LoanStatus = UserPayment.LoanStatus;
                payment.LoanCovered = UserPayment.LoanCovered;
                payment.UserId = id;
            }
            else
            {
                //first payment
                payment.PayedDate = DateTime.Now;
                payment.PayedMonth = DateTime.Now;
                payment.PayedAmount = 0;
                var UserLoan = _context.Loan.FirstOrDefault(e => e.UserId == id && e.LoanGrant == "ACCEPTED");
                payment.RemainingLoanAmount = Convert.ToDouble(UserLoan.TotalPayableAmount);
                payment.RemainingMonthPayment = Convert.ToDouble(UserLoan.MonthlyPayableAmount);
                payment.PenaltyPaymentAmount = Convert.ToDouble(UserLoan.MonthlyPenalty);
                var current = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
                var next = current.AddMonths(1);
                payment.NextPaymentDate = next;
                payment.LoanStatus = "ACTIVE";
                payment.LoanCovered = false;
                payment.UserId = id;
            }

            return View(payment);
        }

        [HttpPost]
        public async Task<IActionResult> MakeFinalPayment(int id, Payment payment)
        {
        
            payment.RemainingLoanAmount = payment.RemainingLoanAmount - payment.PayedAmount;
            payment.RemainingMonthPayment = payment.RemainingMonthPayment - payment.PayedAmount;

            //check for penalty
            if (DateTime.Now > payment.NextPaymentDate )
            {
                int loanPlanId = _context.Loan.FirstOrDefault(e => e.UserId == payment.UserId).loanPlanId;
                decimal penaltyValue = _context.LoanPlans.FirstOrDefault(e => e.Id == loanPlanId).MonthlyOverDuePenalty;
                payment.PenaltyPaymentAmount = Convert.ToDouble(penaltyValue);
            }
            else
            {
                payment.PenaltyPaymentAmount = 0;

            }
            //check if loan is covered
            if (payment.RemainingLoanAmount <= 0)
            {
                payment.LoanStatus = "DEACTIVE";
                payment.LoanCovered = true;
            }
            else
            {
                payment.LoanStatus = "ACTIVE";
                payment.LoanCovered = false;
            }
            _context.Add(payment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(PayeeCustomers));
        }


        // GET: Users/Details/5
        public async Task<IActionResult> PaymentDetail(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments.FirstOrDefaultAsync(m => m.Id == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }
        [HttpGet]
        public async Task<IActionResult> ListPayments()
        {
            ViewBag.AdminPaymentActive = "active";
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            var payment = await _context.Payments.OrderByDescending(e=>e.Id).ToListAsync();
            if (payment == null)
            {
                return NotFound();
            }
            return View(payment);
        }










        public IActionResult LoanPlan()
        {
            ViewBag.LoanPlanActive = "active";
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            return View();
        }
        // GET: LoanPlan
        public async Task<IActionResult> ViewLoanPlan()
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            var loanPlans = await _context.LoanPlans.ToListAsync();
            return View(loanPlans);
        }


        // GET: LoanPlan/Details/5
        public async Task<IActionResult> DetailsLoanPlan(int? id)
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            if (id == null)
            {
                return NotFound();
            }

            var loanPlan = await _context.LoanPlans
                .FirstOrDefaultAsync(m => m.Id == id);
            if (loanPlan == null)
            {
                return NotFound();
            }

            return View(loanPlan);
        }

   
        // POST: LoanPlan/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLoanPlan([Bind("Id,Month,Interest,MonthlyOverDuePenalty")] LoanPlan loanPlan)
        {
            if (ModelState.IsValid)
            {
                _context.Add(loanPlan);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(loanPlan);
        }

        // GET: LoanPlan/Edit/5
        public async Task<IActionResult> EditLoanPlan(int? id)
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            if (id == null)
            {
                return NotFound();
            }

            var loanPlan = await _context.LoanPlans.FindAsync(id);
            if (loanPlan == null)
            {
                return NotFound();
            }
            return View(loanPlan);
        }

        // POST: LoanPlan/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLoanPlan(int id, [Bind("Id,Month,Interest,MonthlyOverDuePenalty")] LoanPlan loanPlan)
        {
            if (id != loanPlan.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(loanPlan);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LoanPlanExists(loanPlan.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ViewLoanPlan));
            }
            return View(loanPlan);
        }

        // GET: LoanPlan/Delete/5
        public async Task<IActionResult> DeleteLoanPlan(int? id)
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            if (id == null)
            {
                return NotFound();
            }

            var loanPlan = await _context.LoanPlans
                .FirstOrDefaultAsync(m => m.Id == id);
            if (loanPlan == null)
            {
                return NotFound();
            }

            return View(loanPlan);
        }

        // POST: LoanPlan/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLoanPlanConfirmed(int id)
        {
            var loanPlan = await _context.LoanPlans.FindAsync(id);
            _context.LoanPlans.Remove(loanPlan);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ViewLoanPlan));
        }

        private bool LoanPlanExists(int id)
        {
            return _context.LoanPlans.Any(e => e.Id == id);
        }

        public IActionResult LoanType()
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            return View();
        }



        // GET: LoanType
        public async Task<IActionResult> ViewLoanType()
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            return View(await _context.LoanTypes.ToListAsync());
        }

        // GET: LoanType/Details/5
        public async Task<IActionResult> DetailsLoanType(int? id)
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            if (id == null)
            {
                return NotFound();
            }

            var loanType = await _context.LoanTypes
                .FirstOrDefaultAsync(m => m.Id == id);
            if (loanType == null)
            {
                return NotFound();
            }

            return View(loanType);
        }

       
        // POST: LoanType/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLoanType([Bind("Id,LoanTypeName,LoanDescription")] LoanType loanType)
        {
            if (ModelState.IsValid)
            {
                _context.Add(loanType);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ViewLoanType));
            }
            return View(loanType);
        }

        // GET: LoanType/Edit/5
        public async Task<IActionResult> EditLoanType(int? id)
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            if (id == null)
            {
                return NotFound();
            }

            var loanType = await _context.LoanTypes.FindAsync(id);
            if (loanType == null)
            {
                return NotFound();
            }
            return View(loanType);
        }

        // POST: LoanType/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLoanType(int id, [Bind("Id,LoanTypeName,LoanDescription")] LoanType loanType)
        {
            if (id != loanType.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(loanType);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LoanTypeExists(loanType.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ViewLoanType));
            }
            return View(loanType);
        }

        // GET: LoanType/Delete/5
        public async Task<IActionResult> DeleteLoanType(int? id)
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            if (id == null)
            {
                return NotFound();
            }

            var loanType = await _context.LoanTypes
                .FirstOrDefaultAsync(m => m.Id == id);
            if (loanType == null)
            {
                return NotFound();
            }

            return View(loanType);
        }

        // POST: LoanType/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLoanTypeConfirmed(int id)
        {
            var loanType = await _context.LoanTypes.FindAsync(id);
            _context.LoanTypes.Remove(loanType);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ViewLoanType));
        }

        private bool LoanTypeExists(int id)
        {
            return _context.LoanTypes.Any(e => e.Id == id);
        }























       
        // GET: Users
        public async Task<IActionResult> Users()
        {
            ViewBag.UsersActive = "active";
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            return View(await _context.Accounts.ToListAsync());
        }


        // GET: Users/Details/5
        public async Task<IActionResult> DetailsUsers(int? id)
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            if (id == null)
            {
                return NotFound();
            }

            var userAccount = await _context.Accounts
                .FirstOrDefaultAsync(m => m.Id == id);
            if (userAccount == null)
            {
                return NotFound();
            }

            return View(userAccount);
        }

        // GET: Users/Create
        public IActionResult CreateUsers()
        {
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUsers([Bind("Id,User_Name,User_Password,IsAdmin")] UserAccount userAccount)
        {
            if (ModelState.IsValid)
            {
                ScryptEncoder encoder = new ScryptEncoder();
                userAccount.User_Password = encoder.Encode(userAccount.User_Password);
                _context.Add(userAccount);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Users));
            }
            return View(userAccount);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> EditUsers(int? id)
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            if (id == null)
            {
                return NotFound();
            }

            var userAccount = await _context.Accounts.FindAsync(id);
            if (userAccount == null)
            {
                return NotFound();
            }
            return View(userAccount);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUsers(int id, UserAccount userAccount)
        {
            if (id != userAccount.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    ScryptEncoder encoder = new ScryptEncoder();
                    userAccount.User_Password = encoder.Encode(userAccount.User_Password);
                    _context.Update(userAccount);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserAccountExists(userAccount.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Users));
            }
            return View(userAccount);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> DeleteUsers(int? id)
        {
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }
            if (id == null)
            {
                return NotFound();
            }

            var userAccount = await _context.Accounts
                .FirstOrDefaultAsync(m => m.Id == id);
            if (userAccount == null)
            {
                return NotFound();
            }

            return View(userAccount);
        }

        // POST: Users/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUsersConfirmed(int id)
        {
            var userAccount = await _context.Accounts.FindAsync(id);
            _context.Accounts.Remove(userAccount);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Users));
        }

        private bool UserAccountExists(int id)
        {
            return _context.Accounts.Any(e => e.Id == id);
        }


        //report 
        public async Task<IActionResult> Report()
        {
            ViewBag.AdminReportActive = "active";
            if (HttpContext.Session.GetInt32("userId") == null || HttpContext.Session.GetInt32("userId") == -1)
            {
                return RedirectToAction(actionName: "Login", controllerName: "Login");
            }

            //Monthly Loans

            TempData["MonthlyGivenOut"] = _context.Loan.Where(e => e.loanDate.Month == DateTime.Now.Month && e.loanDate.Year == DateTime.Now.Year).Select(t => t.loanAmount).Sum();
            //Monthly active Loans
            TempData["MonthlyAciveLoan"] = _context.Loan.Where(e => e.loanDate.Month == DateTime.Now.Month && e.loanDate.Year == DateTime.Now.Year).Where(e=>e.LoanGrant == "ACCEPTED").Count();
            //Monthly Closed Loans
            TempData["MonthlyClosedLoan"] = _context.Payments.Where(e => e.PayedDate.Month == DateTime.Now.Month).Where(e => e.LoanCovered == true).Count();
            //Monthly profit
            TempData["MonthlyProfit"] = _context.Loan.Where(e => e.loanDate.Month == DateTime.Now.Month && e.loanDate.Year == DateTime.Now.Year).Select(e => e.TotalPayableAmount).Sum() - _context.Loan.Where(e => e.loanDate.Month == DateTime.Now.Month && e.loanDate.Year == DateTime.Now.Year).Select(e => e.loanAmount).Sum();
            //Monthly Range
            //Monthly Active Users
            TempData["MonthlyActiveUsers"] = _context.Accounts.Where(e=>e.IsAdmin == false).Count();
             //Monthly Expected Payments
             TempData["MonthlyExpectedPayment"] = _context.Loan.Where(e => e.loanDate.Month == DateTime.Now.Month && e.loanDate.Year == DateTime.Now.Year).Where(e => e.LoanGrant == "ACCEPTED").Select(e=>e.MonthlyPayableAmount).Sum();
            //Monthly Payed Loans
            TempData["MonthlyPayedAmount"] = _context.Payments.Where(e => e.PayedDate.Month == DateTime.Now.Month && e.PayedDate.Year == DateTime.Now.Year).Select(e => e.PayedAmount).Sum();
            //Monthly Payed Penaltys
            TempData["MonthlyPayedPenalty"] = _context.Payments.Where(e => e.PayedDate.Month == DateTime.Now.Month && e.PayedDate.Year == DateTime.Now.Year).Select(e => e.PenaltyPaymentAmount).Sum();
            //yearly
            //Yearly Given out loans
            TempData["YearlyGivenOut"] = _context.Loan.Where(e=>e.loanDate.Year == DateTime.Now.Year).Select(t => t.loanAmount).Sum();
            //Yearly active Loans
            TempData["YearlyActiveLoan"] = _context.Loan.Where(e => e.LoanGrant == "ACCEPTED" && e.loanDate.Year == DateTime.Now.Year).Count();
            //Yearly Closed Loans
            TempData["YearlyClosedLoan"] = _context.Payments.Where(e => e.LoanCovered == true && e.PayedDate.Year == DateTime.Now.Year).Count();
            //Yearly profit
            TempData["YearlyProfit"] = _context.Loan.Where(e => e.loanDate.Year == DateTime.Now.Year).Select(e => e.TotalPayableAmount).Sum() - _context.Loan.Where(e => e.loanDate.Year == DateTime.Now.Year).Select(e => e.loanAmount).Sum();
            //Yearly Range
            //Yearly Active Users
            TempData["YearlyUsers"] = _context.Accounts.Where(e => e.IsAdmin == false).Count();
            //Yearly Expected Payments
            TempData["YearlyPayment"] = _context.Payments.Where(e=>e.PayedDate.Year == DateTime.Now.Year).Select(e => e.PayedAmount).Sum();


            return View(await _context.Accounts.ToListAsync());
        }



















        public RedirectToActionResult Logout()
        {
            HttpContext.Session.SetInt32("userId", -1);
            HttpContext.Session.SetInt32("isAdmin", -1);
            HttpContext.Session.SetString("username", "");
            SessionHelper.SetObjectAsJson(HttpContext.Session, "user", null);

            //Redirects to Index Action Method from HomeController
            return RedirectToAction(actionName: "Login", controllerName: "Login");


        }
    }
}
