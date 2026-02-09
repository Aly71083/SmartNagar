using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartNagar.Models;
using SmartNagar.ViewModels;

namespace SmartNagar.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // =========================
        // LOGIN
        // =========================

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginVM());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = await _userManager.FindByEmailAsync(vm.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(vm);
            }

            // Block inactive users
            if (!user.IsActive)
            {
                ModelState.AddModelError("", "Your account is inactive. Please contact admin.");
                return View(vm);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                vm.Password,
                false,
                false
            );

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(vm);
            }

            // =========================
            // ROLE-BASED REDIRECT
            // =========================

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            if (await _userManager.IsInRoleAsync(user, "MunicipalOfficer"))
            {
                // later we will create Officer dashboard
                return RedirectToAction("Index", "Home");
            }

            // Citizen
            return RedirectToAction("Index", "Home");
        }

        // =========================
        // REGISTER (CITIZEN ONLY)
        // =========================

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterVM());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var existingUser = await _userManager.FindByEmailAsync(vm.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "Email already exists.");
                return View(vm);
            }

            var user = new User
            {
                UserName = vm.Email,
                Email = vm.Email,
                FullName = vm.FullName,
                Role = "Citizen",
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, vm.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(vm);
            }

            await _userManager.AddToRoleAsync(user, "Citizen");
            await _signInManager.SignInAsync(user, false);

            return RedirectToAction("Index", "Home");
        }

        // =========================
        // LOGOUT
        // =========================

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        // =========================
        // ACCESS DENIED
        // =========================

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
