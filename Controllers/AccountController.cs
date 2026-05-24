using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using HomeGroundCoffeeBar.Data;
using HomeGroundCoffeeBar.Models;
using Microsoft.EntityFrameworkCore;
using Models;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;

    public AccountController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ================================
    // LOGOUT
    // ================================
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Signin", "Home");
    }

    // ================================
    // GOOGLE LOGIN
    // ================================
    public IActionResult GoogleLogin()
    {
        var redirectUrl = Url.Action("GoogleResponse", "Account");
        var properties  = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    public async Task<IActionResult> GoogleResponse()
    {
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var claims = result.Principal.Identities.FirstOrDefault()?.Claims;

        var googleId   = claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var name       = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        var email      = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var profilePic = claims?.FirstOrDefault(c => c.Type == "picture")?.Value;

        var user = await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);

        if (user == null)
        {
            user = new UserModel
            {
                GoogleId   = googleId ?? "",
                Name       = name ?? "",
                ProfilePic = profilePic ?? "",
                Role       = "User",
                CreatedAt  = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        HttpContext.Session.SetString("UserId",     user.Id.ToString());
        HttpContext.Session.SetString("GoogleId",   googleId ?? "");
        HttpContext.Session.SetString("Name",       name ?? "");
        HttpContext.Session.SetString("ProfilePic", profilePic ?? "");
        HttpContext.Session.SetString("Role",       user.Role ?? "User");

        var userClaims = new List<Claim>
        {
            new Claim("UserId",          user.Id.ToString()),
            new Claim(ClaimTypes.Name,   name ?? ""),
            new Claim("picture",         profilePic ?? ""),
            new Claim(ClaimTypes.Role,   user.Role ?? "User")
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme))
        );

        return RedirectByRole(user.Role);
    }

    // ================================
    // SIGN UP
    // ================================
    [HttpPost]
    public async Task<IActionResult> Signup(string Name, string Phone, string Password)
    {
        try
        {
            var exists = await _context.Users.AnyAsync(u => u.Phone == Phone);
            if (exists)
            {
                TempData["SignupError"] = "Phone number already exists!";
                return RedirectToAction("Signup", "Home");
            }

            var user = new UserModel
            {
                Name      = Name,
                Phone     = Phone,
                Password  = Password,
                Role      = "User",
                Points    = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SignupSuccess"] = true;
            return RedirectToAction("Signup", "Home");
        }
        catch (Exception ex)
        {
            TempData["SignupError"] = ex.Message;
            return RedirectToAction("Signup", "Home");
        }
    }

    // ================================
    // SIGN IN
    // ================================
    [HttpPost]
    public async Task<IActionResult> Signin(string Phone, string Password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Phone == Phone);

        if (user == null)
        {
            TempData["ErrorMessage"] = "Phone number not found!";
            return RedirectToAction("Signin", "Home");
        }

        if (user.Password != Password)
        {
            TempData["ErrorMessage"] = "Incorrect password!";
            return RedirectToAction("Signin", "Home");
        }

        var profilePic = string.IsNullOrWhiteSpace(user.ProfilePic) || user.ProfilePic == "1"
            ? "" : user.ProfilePic;

        HttpContext.Session.SetString("UserId",     user.Id.ToString());
        HttpContext.Session.SetString("Phone",      Phone);
        HttpContext.Session.SetString("Name",       user.Name ?? "");
        HttpContext.Session.SetString("ProfilePic", profilePic);
        HttpContext.Session.SetString("Role",       user.Role ?? "User");

        var claims = new List<Claim>
        {
            new Claim("UserId",          user.Id.ToString()),
            new Claim("Phone",           Phone),
            new Claim(ClaimTypes.Name,   user.Name ?? ""),
            new Claim("picture",         profilePic),
            new Claim(ClaimTypes.Role,   user.Role ?? "User")
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme))
        );

        return RedirectByRole(user.Role);
    }

    // ================================
    // ROLE-BASED REDIRECT
    // ================================
    private IActionResult RedirectByRole(string? role) => role switch
    {
        "SuperAdmin" => RedirectToAction("Dashboard", "SuperAdmin"),
        "Admin"      => RedirectToAction("Dashboard", "Admin"),
        "Employee"   => RedirectToAction("Dashboard", "Employee"),
        "Rider"      => RedirectToAction("Dashboard", "Rider"),
        _            => RedirectToAction("Home", "Home")
    };

    // ================================
    // CART — using EF Core
    // ================================
    [HttpPost]
    public async Task<IActionResult> AddToCart([FromBody] CartItem item)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return Json(new { success = false, message = "Not logged in" });

        // Check stock
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Name == item.name && p.IsActive);

        if (product == null)
            return Json(new { success = false, message = "Product not found." });

        if (product.Stock <= 0)
            return Json(new { success = false, message = "Sorry, this item is out of stock." });

        var existing = await _context.Cart
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductName == item.name);

        if (existing != null)
        {
            // Check if adding more would exceed stock
            if (existing.Quantity + item.quantity > product.Stock)
                return Json(new { success = false, message = $"Only {product.Stock} left in stock." });

            existing.Quantity += item.quantity;
        }
        else
        {
            if (item.quantity > product.Stock)
                return Json(new { success = false, message = $"Only {product.Stock} left in stock." });

            _context.Cart.Add(new Cart
            {
                UserId      = userId,
                ProductName = item.name,
                Price       = item.price,
                Quantity    = item.quantity,
                Image       = item.image,
                CreatedAt   = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        var cart = await GetCartList(userId);
        return Json(new { success = true, cart });
    }

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return Json(new List<object>());

        var cart = await GetCartList(userId);
        return Json(cart);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityRequest data)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return Json(new { success = false, message = "Not logged in" });

        var item = await _context.Cart
            .FirstOrDefaultAsync(c => c.CartId == data.cartId && c.UserId == userId);

        if (item == null)
            return Json(new { success = false, message = "Item not found" });

        if (data.action == "increase")
        {
            item.Quantity++;
        }
        else if (data.action == "decrease")
        {
            if (item.Quantity <= 1)
                _context.Cart.Remove(item);
            else
                item.Quantity--;
        }

        await _context.SaveChangesAsync();

        var cart = await GetCartList(userId);
        return Json(new { success = true, cart });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateQuantityInput([FromBody] UpdateQuantityInputRequest data)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return Json(new { success = false, message = "Not logged in" });

        var item = await _context.Cart
            .FirstOrDefaultAsync(c => c.CartId == data.cartId && c.UserId == userId);

        if (item == null)
            return Json(new { success = false, message = "Item not found" });

        item.Quantity = data.quantity;
        await _context.SaveChangesAsync();

        var cart = await GetCartList(userId);
        return Json(new { success = true, cart });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveItem([FromBody] RemoveRequest data)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return Json(new { success = false, message = "Not logged in" });

        var item = await _context.Cart
            .FirstOrDefaultAsync(c => c.CartId == data.CartId && c.UserId == userId);

        if (item != null)
        {
            _context.Cart.Remove(item);
            await _context.SaveChangesAsync();
        }

        return Json(new { success = true });
    }

    [HttpGet("/api/user/status")]
    public IActionResult GetStatus()
    {
        if (User.Identity.IsAuthenticated)
            return Ok(new { loggedIn = true, username = User.Identity.Name });
        return Ok(new { loggedIn = false });
    }

    // ================================
    // HELPER
    // ================================
    private async Task<List<object>> GetCartList(string userId)
    {
        return await _context.Cart
            .Where(c => c.UserId == userId)
            .Select(c => (object)new
            {
                cartId   = c.CartId,
                name     = c.ProductName,
                price    = c.Price,
                quantity = c.Quantity,
                image    = c.Image
            })
            .ToListAsync();
    }
}