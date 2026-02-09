using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartNagar.Models;

namespace SmartNagar.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();

            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<User>>();

            // ✅ Roles
            string[] roles = { "Citizen", "Admin", "MunicipalOfficer" };

            foreach (var r in roles)
            {
                if (!await roleManager.RoleExistsAsync(r))
                    await roleManager.CreateAsync(new IdentityRole(r));
            }

            // ✅ Admin (fixed)
            await EnsureUserAsync(
                userManager,
                email: "admin@smartnagar.com",
                password: "Admin@123",
                fullName: "System Administrator",
                role: "Admin"
            );

            // ✅ Municipal Officer 1 (fixed)
            await EnsureUserAsync(
                userManager,
                email: "officer1@smartnagar.com",
                password: "Officer@123",
                fullName: "Municipal Officer 1",
                role: "MunicipalOfficer"
            );

            // ✅ Municipal Officer 2 (fixed)
            await EnsureUserAsync(
                userManager,
                email: "officer2@smartnagar.com",
                password: "Officer@123",
                fullName: "Municipal Officer 2",
                role: "MunicipalOfficer"
            );
        }

        private static async Task EnsureUserAsync(
            UserManager<User> userManager,
            string email,
            string password,
            string fullName,
            string role)
        {
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new User
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    Role = role,          // ✅ REQUIRED (fixes your error)
                    IsActive = true,
                    EmailConfirmed = true
                };

                var created = await userManager.CreateAsync(user, password);
                if (!created.Succeeded)
                {
                    // If you want to debug, you can throw detailed errors:
                    // var msg = string.Join(", ", created.Errors.Select(e => e.Description));
                    // throw new Exception(msg);
                    return;
                }
            }
            else
            {
                // ✅ Ensure fields are not null for existing seeded users
                bool updated = false;

                if (string.IsNullOrWhiteSpace(user.FullName))
                {
                    user.FullName = fullName;
                    updated = true;
                }

                if (string.IsNullOrWhiteSpace(user.Role))
                {
                    user.Role = role;
                    updated = true;
                }

                if (!user.IsActive)
                {
                    user.IsActive = true;
                    updated = true;
                }

                if (updated)
                    await userManager.UpdateAsync(user);
            } 

            // ✅ Ensure Identity Role mapping exists
            if (!await userManager.IsInRoleAsync(user, role))
                await userManager.AddToRoleAsync(user, role);
        }
    }
}
