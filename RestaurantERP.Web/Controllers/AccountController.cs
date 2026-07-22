using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Web.Models;

namespace RestaurantERP.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _emailSender = emailSender;
        _logger = logger;
    }

    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var result = await _signInManager.PasswordSignInAsync(model.Email.Trim(), model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
            return LocalRedirect(SafeReturnUrl(returnUrl));

        if (result.IsLockedOut)
            ModelState.AddModelError("", "Account locked. Please try again later.");
        else
            ModelState.AddModelError("", "Invalid email or password.");

        return View(model);
    }

    public IActionResult Register(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var email = model.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = model.FullName.Trim(),
            PhoneNumber = model.PhoneNumber,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "Customer");
            await _signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(SafeReturnUrl(returnUrl));
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        return View(model);
    }

    /// <summary>Short signup: name + phone + mandatory email.</summary>
    [HttpGet]
    public IActionResult QuickJoin(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return LocalRedirect(SafeReturnUrl(returnUrl));

        return View(new QuickJoinViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickJoin(QuickJoinViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var phoneDigits = new string(model.PhoneNumber.Where(char.IsDigit).ToArray());
        if (phoneDigits.Length < 10)
        {
            ModelState.AddModelError(nameof(model.PhoneNumber), "Enter a valid 10-digit phone number.");
            return View(model);
        }

        var email = model.Email.Trim();
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing != null)
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists. Please login or reset password.");
            return View(model);
        }

        var password = $"Guest@{phoneDigits[^4..]}Aa1!";
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = model.FullName.Trim(),
            PhoneNumber = model.PhoneNumber.Trim(),
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, "Customer");
        await _signInManager.SignInAsync(user, isPersistent: true);
        TempData["Success"] = "Welcome! You're signed in — continue ordering.";
        return LocalRedirect(SafeReturnUrl(model.ReturnUrl));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var email = model.Email.Trim();
        var user = await _userManager.FindByEmailAsync(email);

        // Always show the same message to avoid account enumeration.
        if (user != null && !string.IsNullOrWhiteSpace(user.Email))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var callbackUrl = Url.Action(
                nameof(ResetPassword),
                "Account",
                new { email = user.Email, token = encodedToken },
                protocol: Request.Scheme)!;

            var siteName = "Restaurant ERP";
            var html = $@"
<p>Hello {(string.IsNullOrWhiteSpace(user.FullName) ? "there" : user.FullName)},</p>
<p>We received a request to reset your password for <strong>{HtmlEncoder.Default.Encode(siteName)}</strong>.</p>
<p><a href=""{HtmlEncoder.Default.Encode(callbackUrl)}"" style=""display:inline-block;padding:12px 20px;background:#E63946;color:#fff;text-decoration:none;border-radius:6px;font-weight:600;"">Reset password</a></p>
<p>Or copy this link:<br/><a href=""{HtmlEncoder.Default.Encode(callbackUrl)}"">{HtmlEncoder.Default.Encode(callbackUrl)}</a></p>
<p>If you did not request this, you can ignore this email.</p>";

            var sent = await _emailSender.SendAsync(
                user.Email,
                "Reset your password",
                html,
                $"Reset your password: {callbackUrl}");

            if (!sent)
                _logger.LogWarning("Failed to send password reset email to {Email}", user.Email);
        }

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? email = null, string? token = null)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            TempData["Error"] = "Invalid password reset link.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        return View(new ResetPasswordViewModel
        {
            Email = email,
            Token = token
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user == null)
        {
            // Don't reveal whether the user exists.
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
        }
        catch
        {
            ModelState.AddModelError("", "This reset link is invalid or has expired. Please request a new one.");
            return View(model);
        }

        var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);
        if (result.Succeeded)
            return RedirectToAction(nameof(ResetPasswordConfirmation));

        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);

        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    private static string SafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith('/'))
            return "/Menu";
        return returnUrl;
    }
}
