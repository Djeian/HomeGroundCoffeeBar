using Microsoft.AspNetCore.Mvc;
using HomeGroundCoffeeBar.Data;
using HomeGroundCoffeeBar.DTO;
using Microsoft.EntityFrameworkCore;
using Models; 

namespace HomeGroundCoffeeBar.Controllers;

public class SuperAdminController : Controller
{
    private readonly ApplicationDbContext _context;
    public SuperAdminController(ApplicationDbContext context) => _context = context;

    public IActionResult Dashboard()
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "SuperAdmin") return RedirectToAction("Home", "Home");
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetSalesData(string period = "day")
    {
        var now = DateTime.UtcNow;
        IQueryable<HomeGroundCoffeeBar.Models.Order> query = _context.Orders
            .Where(o => o.Status == "Completed");

        query = period switch
        {
            "day"   => query.Where(o => o.CreatedAt.Date == now.Date),
            "month" => query.Where(o => o.CreatedAt.Month == now.Month && o.CreatedAt.Year == now.Year),
            "year"  => query.Where(o => o.CreatedAt.Year == now.Year),
            _       => query
        };

        var orders   = await query.ToListAsync();
        var total    = orders.Sum(o => o.Total);
        var earnings = Math.Round(total * 0.03m, 2);

        return Json(new
        {
            period,
            totalOrders       = orders.Count,
            totalSales        = total,
            developerEarnings = earnings,
            orders = orders.Select(o => new
            {
                o.OrderId, o.FullName, o.Total,
                o.PaymentMethod,
                createdAt = o.CreatedAt.ToString("MMM dd, yyyy hh:mm tt")
            })
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.Users
            .Select(u => new
            {
                u.Id, u.Name, u.Phone, u.Role, u.Points,
                createdAt = u.CreatedAt.HasValue
                    ? u.CreatedAt.Value.ToString("MMM dd, yyyy")
                    : "—"
            })
            .ToListAsync();
        return Json(users);
    }

    [HttpPost]
    public async Task<IActionResult> AddUser([FromBody] AddUserRequest request)
    {
        var exists = await _context.Users.AnyAsync(u => u.Phone == request.Phone);
        if (exists)
            return BadRequest(new { message = "Phone number already exists." });

        var user = new UserModel
        {
            Name      = request.Name,
            Phone     = request.Phone,
            Password  = request.Password,
            Role      = request.Role,
            Points    = 0,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return Ok(new { message = "User added successfully.", userId = user.Id });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUser([FromBody] DeleteUserRequest request)
    {
        var user = await _context.Users.FindAsync(request.Id);
        if (user == null) return NotFound(new { message = "User not found." });
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return Ok(new { message = "User deleted." });
    }
}