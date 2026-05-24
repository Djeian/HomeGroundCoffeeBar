using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HomeGroundCoffeeBar.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text.Json;
using HomeGroundCoffeeBar.Data;
using HomeGroundCoffeeBar.DTO;
using Microsoft.EntityFrameworkCore;



namespace HomeGroundCoffeeBar.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    private readonly ApplicationDbContext _context;

public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
{
    _logger = logger;
    _context = context;
}

    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Signin", "Home");
    }

    public IActionResult Home()
    {
        // REMOVE THE CODE HERE, accessible naamn si session sa .cshtml files, rather than ipapasa mo pa as a viewbag, unnecessary and matrabaho ba
        return View();
    }

    public IActionResult Privacy() => View();
    public async Task<IActionResult> Menu()
    {
        var products = await _context.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .ToListAsync();

        return View(products);
    }
    public IActionResult Signin() => View();
    public IActionResult Signup() => View();
    public IActionResult Cart() => View();
    public IActionResult Location() => View();
    public IActionResult AboutUs() => View();
    public IActionResult Checkout() => View();
    public IActionResult PaymentMethods() => View();

    // =============================
    // ORDER SUBMISSION & RECEIPT
    // =============================
    [HttpPost]
    public async Task<IActionResult> SubmitOrder([FromBody] SubmitOrderRequest request)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr))
            return Json(new { success = false, message = "Not logged in" });

        string userId = userIdStr;

        var cartItems = await _context.Cart
            .Where(c => c.UserId == userId)
            .ToListAsync();

        if (!cartItems.Any())
            return Json(new { success = false, message = "Cart is empty." });

        var user = await _context.Users.FindAsync(int.Parse(userId));
        if (user == null)
            return Json(new { success = false, message = "User not found." });

        decimal subtotal = cartItems.Sum(i => i.Price * i.Quantity);
        decimal delivery = 50m;
        decimal total    = subtotal + delivery;
        int points       = (int)Math.Floor(total * 0.05m);

        string orderId = "ORD-" + Guid.NewGuid().ToString("N")[..8].ToUpper();
        string address = $"{request.StreetAddress}, {request.City}, {request.State} {request.ZipCode}";

        var order = new Order
        {
            OrderId       = orderId,
            UserId        = int.Parse(userId),
            FullName      = request.FullName,
            Phone         = user.Phone,
            Address       = address,
            PaymentMethod = request.PaymentMethod,
            DeliveryNotes = request.DeliveryNotes,
            Subtotal      = subtotal,
            DeliveryFee   = delivery,
            Total         = total,
            PointsEarned  = points,
            Status        = "Pending",
            CreatedAt     = DateTime.UtcNow,
            Items = cartItems.Select(c => new OrderItemSnapshot
            {
                ProductName = c.ProductName,
                Price       = c.Price,
                Quantity    = c.Quantity,
                Image       = c.Image
            }).ToList()
        };

        foreach (var cartItem in cartItems)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Name == cartItem.ProductName);

            if (product != null && product.Stock > 0)
                product.Stock -= cartItem.Quantity;
        }

        await _context.SaveChangesAsync();

        _context.Orders.Add(order);

        // Only award points and clear cart for COD (instant)
        // For GCash/Maya, do it after payment succeeds
            if (request.PaymentMethod == "cod")
            {
                _context.Cart.RemoveRange(cartItems);  // ← points removed
            }

        await _context.SaveChangesAsync();

        // Redirect based on payment method
        string? redirectUrl = request.PaymentMethod switch
        {
            "gcash" => Url.Action("GCashMain", "GCash", new { amount = (int)total, orderId }),
            "maya"  => Url.Action("PayMayaMain", "PayMaya", new { amount = (int)total, orderId }),
            _       => null  // COD — no redirect
        };

        return Json(new { success = true, orderId, pointsEarned = points, redirectUrl });
    }
    /*[HttpPost]
    public IActionResult sendCartData([FromBody])
    {
        
    }*/

[HttpGet]
public async Task<IActionResult> GetPaymentSettings()
{
    var codSetting = await _context.AppSettings
        .FirstOrDefaultAsync(s => s.Key == "cod_enabled");

    bool codEnabled = codSetting == null || codSetting.Value == "true";
    return Json(new { codEnabled });
}
    


        public IActionResult Redeem()
    {
        var rewards = new List<RewardModel>
        {
            new RewardModel{ Id=1, Name="Homeground Coffee Club Cap", PointsRequired=250, Image="/img/rewards/cap.png", Description="Stylish Homeground cap perfect for coffee lovers who want to represent their favorite brew anywhere."},
            new RewardModel{ Id=2, Name="Homeground Barista Insulated Mug", PointsRequired=400, Image="/img/rewards/insulatedmug.png", Description="Premium insulated mug designed to keep your coffee hot while you enjoy every sip."},
            new RewardModel{ Id=3, Name="Homeground Brew Hoodie", PointsRequired=300, Image="/img/rewards/hoodie.png", Description="Comfortable Homeground hoodie inspired by cozy coffee shop vibes."},
            new RewardModel{ Id=4, Name="Homeground Notebook", PointsRequired=100, Image="/img/rewards/notebook.png", Description="Homeground notebook for jotting down your ideas."}

        };

        return View(rewards);
    }


[HttpPost]
public IActionResult RedeemReward([FromBody] RedeemRequest request)
{
    int rewardId = request.RewardId;

    var name = HttpContext.Session.GetString("Name");
    if (string.IsNullOrEmpty(name))
        return Unauthorized();

    var user = _context.Users.FirstOrDefault(u => u.Name == name);
    if (user == null)
        return NotFound();

    int requiredPoints = rewardId switch
    {
        1 => 100,
        2 => 300,
        3 => 500,
        _ => 0
    };

    if (user.Points < requiredPoints)
        return BadRequest("Not enough points");

    user.Points -= requiredPoints;

    _context.SaveChanges();

    return Json(new { success = true, remainingPoints = user.Points });
}

public async Task<IActionResult> ConfirmAndReceipt(string orderId)
{
    var userIdStr = HttpContext.Session.GetString("UserId");
    if (string.IsNullOrEmpty(userIdStr))
        return RedirectToAction("Signin");

    var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
    if (order == null)
        return RedirectToAction("Menu");

    var user = await _context.Users.FindAsync(int.Parse(userIdStr));

    // Clear cart now that payment is confirmed
    var cartItems = await _context.Cart
        .Where(c => c.UserId == userIdStr)
        .ToListAsync();

    _context.Cart.RemoveRange(cartItems);
    await _context.SaveChangesAsync();

    // Pass orderId to receipt via TempData so JS can pick it up
    TempData["ConfirmedOrderId"] = orderId;
    return RedirectToAction("Receipt");
}

public IActionResult Receipt()
{
    return View();
}

[HttpGet]
public async Task<IActionResult> GetOrder(string orderId)
{
    var order = await _context.Orders
        .FirstOrDefaultAsync(o => o.OrderId == orderId);

    if (order == null)
        return Json(new { success = false, message = "Order not found." });

    return Json(new
    {
        success      = true,
        orderId      = order.OrderId,
        fullName     = order.FullName,
        paymentMethod = order.PaymentMethod,
        subtotal     = order.Subtotal,
        deliveryFee  = order.DeliveryFee,
        total        = order.Total,
        pointsEarned = order.PointsEarned,
        createdAt    = order.CreatedAt.ToString("MMMM dd, yyyy hh:mm tt"),
        items        = order.Items
    });
}

[HttpPost]
public async Task<IActionResult> ConfirmOrder(string orderId)
{
    var userIdStr = HttpContext.Session.GetString("UserId");
    if (string.IsNullOrEmpty(userIdStr))
        return Json(new { success = false, message = "Not logged in" });

    int userId = int.Parse(userIdStr);

    var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
    if (order == null)
        return Json(new { success = false, message = "Order not found." });

    var user = await _context.Users.FindAsync(userId);
    if (user != null)
        user.Points += order.PointsEarned;

    // Clear cart now that payment is done
    var cartItems = await _context.Cart
        .Where(c => c.UserId == userIdStr)
        .ToListAsync();

    _context.Cart.RemoveRange(cartItems);

    await _context.SaveChangesAsync();

    return Json(new { success = true });
}



public async Task<IActionResult> OrderHistory()
{
    var userIdStr = HttpContext.Session.GetString("UserId");
    if (string.IsNullOrEmpty(userIdStr))
        return RedirectToAction("Signin");

    int userId = int.Parse(userIdStr);

    var orders = await _context.Orders
        .Where(o => o.UserId == userId)
        .OrderByDescending(o => o.CreatedAt)
        .ToListAsync();

    return View(orders);
}

[HttpPost]
public async Task<IActionResult> UpdateMyOrderStatus([FromBody] UpdateStatusRequest request)
{
    var userIdStr = HttpContext.Session.GetString("UserId");
    if (string.IsNullOrEmpty(userIdStr))
        return Json(new { success = false, message = "Not logged in" });

    var order = await _context.Orders
        .FirstOrDefaultAsync(o => o.OrderId == request.OrderId
                               && o.UserId == int.Parse(userIdStr));

    if (order == null)
        return Json(new { success = false, message = "Order not found." });

    if (request.Status == "Cancelled" && order.Status != "Pending")
        return Json(new { success = false, message = "Only pending orders can be cancelled." });

    if (request.Status == "Completed" && order.Status != "Delivered")
        return Json(new { success = false, message = "Order must be delivered first." });

    order.Status = request.Status;

    // Award points when completed
    if (request.Status == "Completed")
    {
        var user = await _context.Users.FindAsync(int.Parse(userIdStr));
        if (user != null)
            user.Points += order.PointsEarned;
    }

    await _context.SaveChangesAsync();

    return Json(new { success = true });
}

[HttpGet]
public async Task<IActionResult> GetRiderLocation(string orderId)
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

// =============================
// DTO & Model Classes
// =============================


// TODO move this in a DTO folder

public class OrderDto
{
    public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    public decimal DeliveryFee { get; set; } = 50; // default
    public string PaymentMethod { get; set; } = "Cash";
}

public class OrderModel
{
    public string OrderNumber { get; set; }
    public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    public decimal DeliveryFee { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; }
}

public class OrderItem
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}



