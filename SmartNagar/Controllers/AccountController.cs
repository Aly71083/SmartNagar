using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartNagar.Models;
using SmartNagar.Services;
using SmartNagar.ViewModels;

namespace SmartNagar.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IEmailService _emailService;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var email = (vm.Email ?? "").Trim().ToLower();

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || user.IsDeleted || !user.IsActive)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(vm);
            }

            if (!user.EmailConfirmed)
            {
                TempData["ErrorMsg"] = "Please verify your email first.";
                return RedirectToAction(nameof(VerifyEmailOtp), new { email = user.Email });
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                vm.Password,
                vm.RememberMe,
                lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(vm);
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return RedirectToAction("Dashboard", "Admin");

            if (await _userManager.IsInRoleAsync(user, "MunicipalOfficer"))
                return RedirectToAction("Dashboard", "Officer");

            if (await _userManager.IsInRoleAsync(user, "Citizen"))
                return RedirectToAction("Dashboard", "Citizen");

            await _signInManager.SignOutAsync();
            ModelState.AddModelError("", "Your account does not have a valid role assigned.");
            return View(vm);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var fullName = (vm.FullName ?? "").Trim();
            var username = (vm.Username ?? "").Trim();
            var email = (vm.Email ?? "").Trim().ToLower();
            var phoneNumber = (vm.PhoneNumber ?? "").Trim();

            var existingEmail = await _userManager.FindByEmailAsync(email);
            if (existingEmail != null)
            {
                ModelState.AddModelError("Email", "An account with this email already exists.");
                return View(vm);
            }

            var existingUsername = await _userManager.FindByNameAsync(username);
            if (existingUsername != null)
            {
                ModelState.AddModelError("Username", "This username is already taken. Please choose another one.");
                return View(vm);
            }

            var existingPhone = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

            if (existingPhone != null)
            {
                ModelState.AddModelError("PhoneNumber", "This phone number is already registered.");
                return View(vm);
            }

            var otp = GenerateOtp();

            var user = new User
            {
                FullName = fullName,
                UserName = username,
                Email = email,
                PhoneNumber = phoneNumber,
                Role = "Citizen",
                IsActive = true,
                IsDeleted = false,
                EmailConfirmed = false,
                EmailOtp = otp,
                EmailOtpExpiryUtc = DateTime.UtcNow.AddMinutes(10)
            };

            var result = await _userManager.CreateAsync(user, vm.Password);

            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError("", err.Description);

                return View(vm);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "Citizen");
            if (!roleResult.Succeeded)
            {
                foreach (var err in roleResult.Errors)
                    ModelState.AddModelError("", err.Description);

                await _userManager.DeleteAsync(user);
                return View(vm);
            }

            var subject = "Smart Nagar Email Verification OTP";
            var body = $@"
<div style='font-family:Segoe UI,Arial,sans-serif;line-height:1.7;color:#1f2937'>
    <h2 style='color:#0f172a'>Verify Your Email</h2>
    <p>Hello {user.FullName},</p>
    <p>Thank you for registering in Smart Nagar.</p>
    <p>Your OTP code is:</p>
    <div style='font-size:32px;font-weight:800;letter-spacing:6px;color:#7c4dff;margin:16px 0'>
        {otp}
    </div>
    <p>This OTP will expire in 10 minutes.</p>
</div>";

            await _emailService.SendEmailAsync(user.Email!, subject, body);

            TempData["SuccessMsg"] = "Registration successful. OTP has been sent to your email.";
            return RedirectToAction(nameof(VerifyEmailOtp), new { email = user.Email });
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult VerifyEmailOtp(string email)
        {
            var vm = new VerifyEmailOtpVM
            {
                Email = email ?? ""
            };

            return View(vm);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmailOtp(VerifyEmailOtpVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var email = (vm.Email ?? "").Trim().ToLower();
            var otpCode = (vm.OtpCode ?? "").Trim();

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || user.IsDeleted)
            {
                ModelState.AddModelError("", "Invalid verification request.");
                return View(vm);
            }

            if (user.EmailConfirmed)
            {
                TempData["SuccessMsg"] = "Email already verified. Please login.";
                return RedirectToAction(nameof(Login));
            }

            if (string.IsNullOrWhiteSpace(user.EmailOtp) || !user.EmailOtpExpiryUtc.HasValue)
            {
                ModelState.AddModelError("", "OTP not found. Please request a new OTP.");
                return View(vm);
            }

            if (user.EmailOtpExpiryUtc.Value < DateTime.UtcNow)
            {
                ModelState.AddModelError("", "OTP has expired. Please request a new OTP.");
                return View(vm);
            }

            if (user.EmailOtp != otpCode)
            {
                ModelState.AddModelError("OtpCode", "Invalid OTP.");
                return View(vm);
            }

            user.EmailConfirmed = true;
            user.EmailVerifiedAtUtc = DateTime.UtcNow;
            user.EmailOtp = null;
            user.EmailOtpExpiryUtc = null;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var err in updateResult.Errors)
                    ModelState.AddModelError("", err.Description);

                return View(vm);
            }

            TempData["SuccessMsg"] = "Email verified successfully. Please login.";
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendEmailOtp(string email)
        {
            email = (email ?? "").Trim().ToLower();

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMsg"] = "Email is required.";
                return RedirectToAction(nameof(Register));
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || user.IsDeleted)
            {
                TempData["ErrorMsg"] = "User not found.";
                return RedirectToAction(nameof(Register));
            }

            if (user.EmailConfirmed)
            {
                TempData["SuccessMsg"] = "Email already verified. Please login.";
                return RedirectToAction(nameof(Login));
            }

            var otp = GenerateOtp();
            user.EmailOtp = otp;
            user.EmailOtpExpiryUtc = DateTime.UtcNow.AddMinutes(10);

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                TempData["ErrorMsg"] = "Could not resend OTP right now.";
                return RedirectToAction(nameof(VerifyEmailOtp), new { email });
            }

            var subject = "Smart Nagar Email Verification OTP";
            var body = $@"
<div style='font-family:Segoe UI,Arial,sans-serif;line-height:1.7;color:#1f2937'>
    <h2 style='color:#0f172a'>Your New OTP</h2>
    <p>Hello {user.FullName},</p>
    <p>Your new OTP code is:</p>
    <div style='font-size:32px;font-weight:800;letter-spacing:6px;color:#7c4dff;margin:16px 0'>
        {otp}
    </div>
    <p>This OTP will expire in 10 minutes.</p>
</div>";

            await _emailService.SendEmailAsync(user.Email!, subject, body);

            TempData["SuccessMsg"] = "A new OTP has been sent to your email.";
            return RedirectToAction(nameof(VerifyEmailOtp), new { email });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var email = (vm.Email ?? "").Trim().ToLower();
            var user = await _userManager.FindByEmailAsync(email);

            if (user != null && !user.IsDeleted)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedToken = WebUtility.UrlEncode(token);

                var resetLink = Url.Action(
                    nameof(ResetPassword),
                    "Account",
                    new { email = user.Email, token = encodedToken },
                    Request.Scheme);

                var subject = "Reset your Smart Nagar password";

                var body = $@"
<div style='font-family:Segoe UI,Arial,sans-serif;line-height:1.7;color:#1f2937'>
    <h2 style='color:#0f172a'>Smart Nagar Password Reset</h2>
    <p>Hello {user.FullName},</p>
    <p>We received a request to reset your password.</p>
    <p>
        <a href='{resetLink}' style='display:inline-block;background:#7c4dff;color:#ffffff;text-decoration:none;padding:12px 18px;border-radius:8px;font-weight:bold;'>
            Reset Password
        </a>
    </p>
    <p>If the button does not work, copy and paste this link into your browser:</p>
    <p>{resetLink}</p>
    <p>If you did not request this, you can safely ignore this email.</p>
</div>";

                await _emailService.SendEmailAsync(user.Email!, subject, body);
            }

            TempData["Msg"] = "If the email exists in our system, a reset link has been sent.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
                return RedirectToAction(nameof(ForgotPassword));

            var vm = new ResetPasswordVM
            {
                Email = email,
                Token = token
            };

            return View(vm);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = await _userManager.FindByEmailAsync(vm.Email);
            if (user == null || user.IsDeleted)
            {
                ModelState.AddModelError("", "Invalid password reset request.");
                return View(vm);
            }

            var decodedToken = WebUtility.UrlDecode(vm.Token);
            var result = await _userManager.ResetPasswordAsync(user, decodedToken!, vm.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError("", err.Description);

                return View(vm);
            }

            TempData["SuccessMsg"] = "Password has been reset successfully. Please login.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private static string GenerateOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}