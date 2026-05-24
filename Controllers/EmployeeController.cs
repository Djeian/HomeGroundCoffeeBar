using Microsoft.AspNetCore.Mvc;
using HomeGroundCoffeeBar.Data;
using HomeGroundCoffeeBar.DTO;
using Microsoft.EntityFrameworkCore;

namespace HomeGroundCoffeeBar.Controllers;

public class EmployeeController : Controller
{
    private readonly ApplicationDbContext _context;
    public EmployeeController(ApplicationDbContext context) => _context = context;

    public IActionResult Dashboard()
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Employee") return RedirectToAction("Home", "Home");
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var today = DateTime.UtcNow.Date;

        var orders = await _context.Orders
            .Where(o => o.CreatedAt.Date == today)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Json(orders.Select(o => new
        {
            o.OrderId, o.FullName, o.Phone, o.Address,
            o.PaymentMethod, o.DeliveryNotes,
            subtotal    = (double)o.Subtotal,
            deliveryFee = (double)o.DeliveryFee,
            total       = (double)o.Total,
            o.PointsEarned,
            o.Status,
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

        order.Status = request.Status;

        if (request.Status == "Completed")
        {
            var user = await _context.Users.FindAsync(order.UserId);
            if (user != null) user.Points += order.PointsEarned;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Status updated." });
    }

    [HttpGet]
public async Task<IActionResult> GetProducts()
{
    var products = await _context.Products
        .OrderBy(p => p.Category).ThenBy(p => p.Name)
        .ToListAsync();

    return Json(products.Select(p => new
    {
        p.Id, p.Name,
        price    = (double)p.Price,
        p.Image, p.Category, p.Stock, p.IsActive
    }));
}

[HttpPost]
public async Task<IActionResult> UpdateStock([FromBody] UpdateStockRequest request)
{
    var product = await _context.Products.FindAsync(request.Id);
    if (product == null)
        return NotFound(new { message = "Product not found." });

    product.Stock = request.Stock;
    await _context.SaveChangesAsync();
    return Ok(new { message = "Stock updated." });
}
}