using AuthAPI.Model;
using ECommerce.Shared.Authorization;
using Microsoft.AspNetCore.Identity;

namespace AuthAPI.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await SeedUserAsync(configuration, logger, userManager, roleManager,
                section: "SeedAdmin", role: UserRoles.Admin, defaultUserName: "kamalesh", defaultFullName: "Kamalesh");

            await SeedUserAsync(configuration, logger, userManager, roleManager,
                section: "SeedShipment", role: UserRoles.Shipment, defaultUserName: "shipment", defaultFullName: "Shipment User");

            await SeedUserAsync(configuration, logger, userManager, roleManager,
                section: "SeedCustomer", role: UserRoles.Customer, defaultUserName: "customer", defaultFullName: "Customer User");
        }

        private static async Task SeedUserAsync(
            IConfiguration configuration,
            ILogger logger,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            string section,
            string role,
            string defaultUserName,
            string defaultFullName)
        {
            var email = configuration[$"{section}:Email"];
            var password = configuration[$"{section}:Password"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("{Section}:Email or {Section}:Password is not configured. Skipping {Role} seed.", section, section, role);
                return;
            }

            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }

            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new User
                {
                    UserName = configuration[$"{section}:UserName"] ?? defaultUserName,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = configuration[$"{section}:FullName"] ?? defaultFullName,
                    PhoneNumber = configuration[$"{section}:PhoneNumber"],
                    Address = configuration[$"{section}:Address"] ?? string.Empty
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    logger.LogError("Could not seed {Role} user: {Errors}", role,
                        string.Join(" ", createResult.Errors.Select(e => e.Description)));
                    return;
                }

                logger.LogInformation("Seeded {Role} user {Email}.", role, email);
            }

            // Users seeded before PhoneNumber existed: fill it in without overwriting a number that is already set.
            var seedPhone = configuration[$"{section}:PhoneNumber"];
            if (!string.IsNullOrWhiteSpace(seedPhone) && string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                await userManager.SetPhoneNumberAsync(user, seedPhone);
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}
