using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using TimeTrackingApp.Models.ViewModel;
using TimeTrackingApp.Services;
using TimeTrackingsApp.Models.Entities;

namespace TimeTrackingApp.Controllers
{
    public class Manager : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;
        public Manager(ApplicationDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
        }

        public async Task<IActionResult> ManagerPanel()
        {
            var manager = await _userManager.GetUserAsync(User);
            if (manager == null)
                return Unauthorized();

            var managerDept = manager.Department;

            var employees = await _userManager.Users
                .Where(u => u.Department == managerDept && u.IsActive)
                .ToListAsync();


            var model = new List<ManagerViewModel>();

            foreach (var emp in employees)
            {
                var entries = await _context.TimeEntries
                    .Where(t => t.userid == emp.Id
                        && t.entrydate.Month == DateTime.UtcNow.Month
                        && t.entrydate.Year == DateTime.UtcNow.Year)
                    .ToListAsync();

                model.Add(new ManagerViewModel
                {
                    UserId = emp.Id,
                    FullName = $"{emp.FirstName} {emp.LastName}",
                    Position = emp.Position,
                    Department = emp.Department,
                    TotalHoursThisMonth = entries.Sum(e => e.totalhours),
                    EntriesCount = entries.Count
                });
            }

            return View(model);
        }
        public async Task<IActionResult> History(string id)
        {
            var employee = await _userManager.FindByIdAsync(id);
            if (employee == null)
                return NotFound();

            var entries = await _context.TimeEntries
                .Where(t => t.userid == id)
                .OrderByDescending(t => t.entrydate)
                .ToListAsync();

            var model = entries.Select(e => new EditTimeEntriesViewModel
            {
                Id = e.id,
                Name = $"{employee.FirstName} {employee.LastName}",
                EntryDate = e.entrydate,
                StartTime = e.starttime,
                EndTime = e.endtime,
                TotalHours = e.totalhours,
                Note = e.note
            }).ToList();

            ViewData["EmployeeId"] = id;

            return View(model);
        }
        public async Task<IActionResult> EditEntries(int id)
        {
            var entry = await _context.TimeEntries.FindAsync(id);
            if (entry == null)
                return NotFound();

            var employee = await _userManager.FindByIdAsync(entry.userid);

            var model = new EditTimeEntriesViewModel
            {
                Id = entry.id,
                Name = $"{employee.FirstName} {employee.LastName}",
                EntryDate = entry.entrydate,
                StartTime = entry.starttime,
                EndTime = entry.endtime,
                TotalHours = entry.totalhours,
                Note = entry.note
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEntries(EditTimeEntriesViewModel model)
        {
            var entry = await _context.TimeEntries.FindAsync(model.Id);
            if (entry == null)
                return NotFound();

            entry.starttime = model.StartTime;
            entry.endtime = model.EndTime;
            entry.note = model.Note;
            entry.modifiedby = User.Identity.Name;

            if (entry.endtime != TimeSpan.Zero)
            {
                entry.totalhours = (entry.endtime - entry.starttime).TotalHours;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("History", new { id = entry.userid });
        }
        public async Task<IActionResult> LeaveRequests()
        {
            var requests = await _context.LeaveRequests
                .Include(l => l.User)
                .OrderByDescending(l => l.requestdate)
                .ToListAsync();

            return View(requests);
        }
        [HttpPost]

        public async Task<IActionResult> ApproveLeave(int id)
        {
            var request = await _context.LeaveRequests
    .Include(l => l.User)
    .FirstOrDefaultAsync(l => l.id == id);
            if (request == null)
                return NotFound();

            var managerId = User.Identity!.Name;

            request.status = "Approved";
            request.decisiondate = DateTime.UtcNow;
            request.reviewedby = managerId;

            await _context.SaveChangesAsync();
            await _emailService.SendEmailAsync(
                request.User.Email,
                " Wniosek urlopowy – zatwierdzony",
                $"<p>Twój wniosek urlopowy od <b>{request.startdate:dd.MM.yyyy}</b> " +
                $"do <b>{request.enddate:dd.MM.yyyy}</b> został <b>ZATWIERDZONY</b>.</p>");

            return RedirectToAction("LeaveRequests");
        }
        [HttpPost]
        public async Task<IActionResult> RejectLeave(int id)
        {
            var request = await _context.LeaveRequests
    .Include(l => l.User)
    .FirstOrDefaultAsync(l => l.id == id);
            if (request == null)
                return NotFound();

            var managerId = User.Identity!.Name;

            request.status = "Rejected";
            request.decisiondate = DateTime.UtcNow;
            request.reviewedby = managerId;

            await _context.SaveChangesAsync();
            await _emailService.SendEmailAsync(
                request.User.Email,
                "Wniosek urlopowy – odrzucony",
                $"<p>Twój wniosek urlopowy od <b>{request.startdate:dd.MM.yyyy}</b> " +
                $"do <b>{request.enddate:dd.MM.yyyy}</b> został <b>ODRZUCONY</b>.</p>");

            return RedirectToAction("LeaveRequests");
        }

    }
}