#nullable disable
using Alumifix_Sis_adm.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using static Alumifix_Sis_adm.Models.LoginModel;
using Microsoft.AspNetCore.Authorization;

namespace Alumifix_Sis_adm.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    [TempData]
    public string ErrorMessage { get; set; }

    [HttpGet]
    [ActionName("Index")]
    [Authorize]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    [ActionName("Login")]
    public async Task<IActionResult> GetLogin(string returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        // Clear the existing external cookie
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return View();
    }

    [HttpPost]
    [ActionName("Login")]
    public async Task<IActionResult> PostLogin(string returnUrl = null)
    {
        if (ModelState.IsValid)
        {
            // Use Input.Email and Input.Password to authenticate the user
            // with your custom authentication logic.
            //
            // For demonstration purposes, the sample validates the user
            // on the email address maria.rodriguez@contoso.com with 
            // any password that passes model validation.

            var user = await AuthenticateUser(Input.Email, Input.Password);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim("FullName", user.FullName),
                new Claim(ClaimTypes.Role, "Administrator"),
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                //AllowRefresh = <bool>,
                // Refreshing the authentication session should be allowed.

                //ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10),
                // The time at which the authentication ticket expires. A 
                // value set here overrides the ExpireTimeSpan option of 
                // CookieAuthenticationOptions set with AddCookie.

                //IsPersistent = true,
                // Whether the authentication session is persisted across 
                // multiple requests. When used with cookies, controls
                // whether the cookie's lifetime is absolute (matching the
                // lifetime of the authentication ticket) or session-based.

                //IssuedUtc = <DateTimeOffset>,
                // The time at which the authentication ticket was issued.

                //RedirectUri = <string>
                // The full path or absolute URI to be used as an http 
                // redirect response value.
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation("User {Email} logged in at {Time}.",
                user.Email, DateTime.UtcNow);

            return LocalRedirect("/Home/Index");
        }

        // Something failed. Redisplay the form.
        return View();
    }

    private async Task<ApplicationUser> AuthenticateUser(string email, string password)
    {
        // For demonstration purposes, authenticate a user
        // with a static email address. Ignore the password.
        // Assume that checking the database takes 500ms

        await Task.Delay(500);

        if (email == "maria.rodriguez@contoso.com")
        {
            return new ApplicationUser()
            {
                Email = "maria.rodriguez@contoso.com",
                FullName = "Maria Rodriguez"
            };
        }
        else
        {
            return null;
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        _logger.LogInformation("User {Name} logged out at {Time}.",
            User.Identity.Name, DateTime.UtcNow);

        #region snippet1
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        #endregion

        return LocalRedirect("/Home/Login");
    }
}

