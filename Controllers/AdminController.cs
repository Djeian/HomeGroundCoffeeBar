using Microsoft.AspNetCore.Mvc;
using HomeGroundCoffeeBar.Models;
using HomeGroundCoffeeBar.Data;
using Microsoft.EntityFrameworkCore;
using Models;
using HomeGroundCoffeeBar.DTO;

namespace HomeGroundCoffeeBar.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult AdminHomePage()
        {
            var users = _context.Users.ToList();
            return View(users);
        }

        [HttpPost]
        public IActionResult EditUser([FromBody] UserModel user)
        {
            if (string.IsNullOrWhiteSpace(user.Name))
                return BadRequest(new { message = "Name is required." });

            if (user.Name.Length < 3)
                return BadRequest(new { message = "Name must be at least 3 characters." });

            if (user.Name.Length > 18)
                return BadRequest(new { message = "Name must not exceed 18 characters." });

            if (string.IsNullOrEmpty(user.Phone) || user.Phone.Length != 11)
                return BadRequest(new { message = "Phone number must be exactly 11 digits." });

            // Check for duplicates
            var duplicate = _context.Users
                .Any(u => (u.Phone == user.Phone || u.Name == user.Name) && u.Id != user.Id);

            if (duplicate)
                return Conflict(new { message = "Duplicate Name or Phone detected!" });

            var existing = _context.Users.Find(user.Id);
            if (existing == null)
                return NotFound(new { message = "User not found." });

            existing.Name     = user.Name;
            existing.Phone    = user.Phone;
            existing.Password = user.Password;
            existing.Role     = user.Role;

            _context.SaveChanges();

            return Ok(new { message = "User updated successfully!" });
        }

        [HttpPost]
        public IActionResult DeleteUser([FromBody] UserModel user)
        {
            if (user == null)
                return BadRequest(new { message = "Invalid user Id." });

            var existing = _context.Users.Find(user.Id);
            if (existing == null)
                return NotFound(new { message = "User not found." });

            _context.Users.Remove(existing);
            _context.SaveChanges();

            return Ok(new { message = "User deleted successfully!" });
        }

        public async Task<IActionResult> GetOrders()
{
    var orders = await _context.Orders
        .OrderByDescending(o => o.CreatedAt)
        .ToListAsync();

    return Json(orders.Select(o => new
    {
        o.Id,
        o.OrderId,
        o.FullName,
        o.Phone,
        o.Address,
        o.PaymentMethod,
        o.DeliveryNotes,
        o.Subtotal,
        o.DeliveryFee,
        o.Total,
        o.PointsEarned,
        o.Status,
        createdAt = o.CreatedAt.ToString("MMM dd, yyyy hh:mm tt"),
        items     = o.Items
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

    // Award points when admin marks as Completed
    if (request.Status == "Completed")
    {
        var user = await _context.Users.FindAsync(order.UserId);
        if (user != null)
            user.Points += order.PointsEarned;
    }

    await _context.SaveChangesAsync();

    return Ok(new { message = "Status updated." });
}

public IActionResult Dashboard()
{
    var role = HttpContext.Session.GetString("Role");
    if (role != "Admin") return RedirectToAction("Home", "Home");
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
        "month" => query.Where(o => o.CreatedAt.Month == now.Month
                                 && o.CreatedAt.Year  == now.Year),
        "year"  => query.Where(o => o.CreatedAt.Year == now.Year),
        _       => query
    };

    var orders = await query.ToListAsync();
    var total  = orders.Sum(o => o.Total);

    return Json(new
    {
        period,
        totalOrders = orders.Count,
        totalSales  = total,
        orders = orders.Select(o => new
        {
            o.OrderId, o.FullName, o.Total,
            o.PaymentMethod,
            createdAt = o.CreatedAt.ToString("MMM dd, yyyy hh:mm tt")
        })
    });
}

[HttpGet]
public async Task<IActionResult> GetProducts()
{
    var products = await _context.Products
        .OrderBy(p => p.Category).ThenBy(p => p.Name)
        .ToListAsync();

    return Json(products.Select(p => new
    {
        p.Id,
        p.Name,
        price    = (double)p.Price,  // ← cast to double
        p.Image,
        p.Category,
        p.Stock,
        p.IsActive
    }));
}

[HttpPost]
public async Task<IActionResult> AddProduct([FromBody] ProductRequest request)
{
    var product = new Product
    {
        Name      = request.Name,
        Price     = request.Price,
        Image     = request.Image,
        Category  = request.Category,
        Stock     = request.Stock,
        IsActive  = true,
        CreatedAt = DateTime.UtcNow
    };

    _context.Products.Add(product);
    await _context.SaveChangesAsync();
    return Ok(new { message = "Product added.", product.Id });
}

[HttpPost]
public async Task<IActionResult> UpdateProduct([FromBody] ProductRequest request)
{
    var product = await _context.Products.FindAsync(request.Id);
    if (product == null) return NotFound(new { message = "Product not found." });

    product.Name     = request.Name;
    product.Price    = request.Price;
    product.Image    = request.Image;
    product.Category = request.Category;
    product.Stock    = request.Stock;
    product.IsActive = request.IsActive;

    await _context.SaveChangesAsync();
    return Ok(new { message = "Product updated." });
}

[HttpPost]
public async Task<IActionResult> DeleteProduct([FromBody] DeleteUserRequest request)
{
    var product = await _context.Products.FindAsync(request.Id);
    if (product == null) return NotFound(new { message = "Product not found." });
    _context.Products.Remove(product);
    await _context.SaveChangesAsync();
    return Ok(new { message = "Product deleted." });
}


[HttpPost]
public async Task<IActionResult> SeedProducts()
{
    if (await _context.Products.AnyAsync())
        return Ok(new { message = "Products already seeded." });

    var products = new List<Product>
    {
        new Product { Name = "Blueberry",           Price = 120, Image = "/img/Product/blueberry.png",              Category = "cold",   Stock = 50 },
        new Product { Name = "Caramel Latte",        Price = 170, Image = "/img/Product/caramel_latte.png",          Category = "cold",   Stock = 50 },
        new Product { Name = "Classic Aloha",        Price = 190, Image = "/img/Product/classic_aloha_burger.png",   Category = "burger", Stock = 50 },
        new Product { Name = "Classic Burger",       Price = 210, Image = "/img/Product/classic_burger.png",         Category = "burger", Stock = 50 },
        new Product { Name = "Dirty Matcha Latte",   Price = 200, Image = "/img/Product/dirty_matcha_latte.png",     Category = "cold",   Stock = 50 },
        new Product { Name = "Green Apple",          Price = 120, Image = "/img/Product/green_apple.png",            Category = "cold",   Stock = 50 },
        new Product { Name = "Hot Milk Chocolate",   Price = 170, Image = "/img/Product/hot_milk_chocolate.png",     Category = "hot",    Stock = 50 },
        new Product { Name = "Iced Milk Chocolate",  Price = 170, Image = "/img/Product/iced_milk_chocolate.png",    Category = "cold",   Stock = 50 },
        new Product { Name = "Iced Milk Strawberry", Price = 170, Image = "/img/Product/iced_milk_strawberry.png",   Category = "cold",   Stock = 50 },
        new Product { Name = "Matcha Latte Oat",     Price = 210, Image = "/img/Product/matcha_latte_oat.png",       Category = "cold",   Stock = 50 },
        new Product { Name = "Peach",                Price = 120, Image = "/img/Product/peach.png",                  Category = "cold",   Stock = 50 },
        new Product { Name = "Signature Barbeque",   Price = 230, Image = "/img/Product/signature_barbeque.png",     Category = "burger", Stock = 50 },
        new Product { Name = "Spanish Latte",        Price = 180, Image = "/img/Product/spanish_latte.png",          Category = "cold",   Stock = 50 },
        new Product { Name = "White Mocha Latte",    Price = 180, Image = "/img/Product/white_mocha_latte.png",      Category = "cold",   Stock = 50 },
    };

    _context.Products.AddRange(products);
    await _context.SaveChangesAsync();
    return Ok(new { message = "Products seeded successfully." });
}

[HttpPost]
public async Task<IActionResult> AddUser([FromBody] AddUserRequest request)
{
    // Admin can only add Employee or Rider
    if (request.Role != "Employee" && request.Role != "Rider")
        return BadRequest(new { message = "Admin can only add Employee or Rider." });

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
    return Ok(new { message = "User added successfully." });
}

[HttpGet]
public async Task<IActionResult> GetStaff()
{
    var staff = await _context.Users
        .Where(u => u.Role == "Employee" || u.Role == "Rider")
        .Select(u => new
        {
            u.Id, u.Name, u.Phone, u.Role,
            createdAt = u.CreatedAt.HasValue
                ? u.CreatedAt.Value.ToString("MMM dd, yyyy") : "—"
        })
        .ToListAsync();
    return Json(staff);
}


[HttpGet]
public async Task<IActionResult> GetSettings()
{
    var settings = await _context.AppSettings.ToListAsync();
    return Json(settings.ToDictionary(s => s.Key, s => s.Value));
}

[HttpPost]
public async Task<IActionResult> UpdateSetting([FromBody] UpdateSettingRequest request)
{
    var setting = await _context.AppSettings
        .FirstOrDefaultAsync(s => s.Key == request.Key);

    if (setting == null)
    {
        _context.AppSettings.Add(new AppSetting
        {
            Key   = request.Key,
            Value = request.Value
        });
    }
    else
    {
        setting.Value = request.Value;
    }

    await _context.SaveChangesAsync();
    return Ok(new { message = "Setting updated." });
}

    }

    

    
}

