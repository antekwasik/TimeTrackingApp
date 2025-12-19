using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeTrackingsApp.Models.Entities;
using TimeTrackingApp.Services;


namespace TimeTrackingApp.Controllers
{
    [Authorize]
    public class EmployeeController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly INotificationEmailService _notificationEmailService;

        public EmployeeController(UserManager<User> userManager, ApplicationDbContext context, INotificationEmailService notificationEmailService)
        {
            _userManager = userManager;
            _context = context;
            _notificationEmailService = notificationEmailService;
        }

        public async Task<IActionResult> Panel()
        {
            var user = await _userManager.GetUserAsync(User);


            var today = DateTime.UtcNow.Date;

            var openEntry = await _context.TimeEntries
                .FirstOrDefaultAsync(t =>
                    t.userid == user.Id &&
                    t.entrydate == today &&
                    t.endtime == TimeSpan.Zero);

            ViewBag.HasStartedWork = openEntry != null;

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> StartWork()
        {
            var user = await _userManager.GetUserAsync(User);
            var today = DateTime.UtcNow.Date;

            var existing = await _context.TimeEntries
                .FirstOrDefaultAsync(t => t.userid == user.Id && t.entrydate == today);

            if (existing != null)
            {
                TempData["error"] = "Już rozpocząłeś pracę dzisiaj.";
                return RedirectToAction("Panel");
            }

            var entry = new TimeEntry
            {
                userid = user.Id,
                entrydate = today,
                starttime = DateTime.UtcNow.TimeOfDay,
                endtime = TimeSpan.Zero,
                totalhours = 0,
                createdat = DateTime.UtcNow
            };

            _context.TimeEntries.Add(entry);
            await _context.SaveChangesAsync();

            TempData["success"] = "Rozpoczęcie pracy zarejestrowane.";
            return RedirectToAction("Panel");
        }


        [HttpPost]
        public async Task<IActionResult> EndWork()
        {
            var user = await _userManager.GetUserAsync(User);
            var today = DateTime.UtcNow.Date;

            var entry = await _context.TimeEntries
                .FirstOrDefaultAsync(t =>
                    t.userid == user.Id &&
                    t.entrydate == today &&
                    t.endtime == TimeSpan.Zero);

            if (entry == null)
            {
                TempData["error"] = "Nie rozpocząłeś jeszcze pracy.";
                return RedirectToAction("Panel");
            }

            entry.endtime = DateTime.UtcNow.TimeOfDay;

            entry.totalhours = (entry.endtime - entry.starttime).TotalHours;

            entry.modifiedby = user.Id;

            await _context.SaveChangesAsync();

            TempData["success"] = "Zakończenie pracy zarejestrowane.";
            return RedirectToAction("Panel");
        }

        public async Task<IActionResult> History()
        {
            var user = await _userManager.GetUserAsync(User);

            var entries = await _context.TimeEntries
                .Where(t => t.userid == user.Id)
                .OrderByDescending(t => t.entrydate)
                .ToListAsync();

            return View(entries);
        }



        public async Task<IActionResult> Summary(int? year, int? month)
        {
            var user = await _userManager.GetUserAsync(User);

            year ??= DateTime.UtcNow.Year;
            month ??= DateTime.UtcNow.Month;

            var entries = await _context.TimeEntries
                .Where(t =>
                    t.userid == user.Id &&
                    t.entrydate.Year == year &&
                    t.entrydate.Month == month)
                .ToListAsync();

            double total = entries.Sum(e => e.totalhours);

            ViewBag.Year = year;
            ViewBag.Month = month;
            ViewBag.TotalHours = total;

            return View(entries);

        }
        public IActionResult LeaveRequest()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LeaveRequest(
            string leavetype,
            DateTime startdate,
            DateTime enddate,
            string? reason)
        {
            var user = await _userManager.GetUserAsync(User);

            if (startdate > enddate)
            {
                TempData["error"] = "Data początkowa nie może być później niż końcowa.";
                return RedirectToAction("LeaveRequest");
            }
            var request = new LeaveRequest
            {
                userid = user.Id,
                leavetype = leavetype,

                startdate = DateTime.SpecifyKind(startdate, DateTimeKind.Utc),
                enddate = DateTime.SpecifyKind(enddate, DateTimeKind.Utc),

                dayscount = (enddate.Date - startdate.Date).Days + 1,
                reason = reason,
                status = "Pending",
                requestdate = DateTime.UtcNow
            };


            _context.LeaveRequests.Add(request);
            await _context.SaveChangesAsync();

            await _notificationEmailService.SendLeaveRequestSubmittedEmail(user);

            TempData["success"] = "Wniosek urlopowy został zapisany.";
            return RedirectToAction("Panel");
        }
        [HttpGet]
        public async Task<IActionResult> GetLeavesForCalendar()
        {
            var leaves = await _context.LeaveRequests
                .Include(l => l.User)
                .Select(l => new
                {
                    title = $"{l.User.FirstName} {l.User.LastName} ({l.leavetype})",
                    start = l.startdate.ToString("yyyy-MM-dd"),
                    end = l.enddate.AddDays(1).ToString("yyyy-MM-dd"),

                    backgroundColor =
                        l.leavetype == "Wypoczynkowy" ? "#10274e" : 
                        l.leavetype == "Bezpłatny" ? "#3d5eae" :     
                        l.leavetype == "Na żądanie" ? "#7c98fb" :     
                        "#0d6efd",                                      

                    borderColor =
                        l.leavetype == "Wypoczynkowy" ? "#10274e" :
                        l.leavetype == "Bezpłatny" ? "#3d5eae" :
                        l.leavetype == "Na żądanie" ? "#7c98fb" :
                        "#0d6efd"
                })
                .ToListAsync();

            return Json(leaves);
        }



    }
}
