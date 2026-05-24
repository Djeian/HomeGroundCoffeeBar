using Microsoft.AspNetCore.Mvc;
using HomeGroundCoffeeBar.Data;
using HomeGroundCoffeeBar.DTO;
using Microsoft.EntityFrameworkCore;
using HomeGroundCoffeeBar.Models;

namespace HomeGroundCoffeeBar.Controllers;

public class RiderController : Controller
{
    private readonly ApplicationDbContext _context;
    public RiderController(ApplicationDbContext context) => _context = context;

    public IActionResult Dashboard()
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Rider") return RedirectToAction("Home", "Home");
        return View();
    }

    // Orders assigned to rider (status = Preparing)
    [HttpGet]
    public async Task<IActionResult> GetAssignedOrders()
    {
        var orders = await _context.Orders
            .Where(o => o.Status == "Preparing" || o.Status == "Pickup" || o.Status == "Otw")
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Json(orders.Select(o => new
        {
            o.OrderId, o.FullName, o.Phone, o.Address,
            o.Total, o.Status,
            createdAt = o.CreatedAt.ToString("MMM dd, yyyy hh:mm tt"),
            items = o.Items
        }));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateStatusRequest request)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

        if (order == null)
            return NotFound(new { message = "Order not found." });

        // Rider can move through: Preparing → Pickup → Otw → Delivered
        var allowed = new[] { "Pickup", "Otw", "Delivered" };
        if (!allowed.Contains(request.Status))
            return BadRequest(new { message = "Invalid status for rider." });

        order.Status = request.Status;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Status updated." });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateLocation([FromBody] RiderLocationRequest request)
    {
        var existing = await _context.RiderLocations
            .FirstOrDefaultAsync(r => r.OrderId == request.OrderId);

        if (existing != null)
        {
            existing.Latitude  = request.Latitude;
            existing.Longitude = request.Longitude;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.RiderLocations.Add(new RiderLocation
            {
                OrderId   = request.OrderId,
                Latitude  = request.Latitude,
                Longitude = request.Longitude,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> GetLocation(string orderId)
    {
        var loc = await _context.RiderLocations
            .FirstOrDefaultAsync(r => r.OrderId == orderId);

        if (loc == null)
            return Json(new { found = false });

        return Json(new
        {
            found     = true,
            latitude  = loc.Latitude,
            longitude = loc.Longitude,
            updatedAt = loc.UpdatedAt.ToString("hh:mm:ss tt")
        });
    }
}