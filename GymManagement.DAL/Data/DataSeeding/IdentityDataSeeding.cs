using GymManagement.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GymManagement.DAL.Data.DataSeeding
{
    public static class IdentityDataSeeding
    {
        public static async Task SeedAsync(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, ILogger logger, string? adminPassword, CancellationToken ct = default)
        {
			try
			{
				bool HasUser = userManager.Users.Any();
				bool HasRoles = roleManager.Roles.Any();

				if (HasUser && HasRoles) return;

				if(!HasRoles)
				{
					var roles = new List<IdentityRole>()
					{
						new IdentityRole() { Name = "SuperAdmin"},
						new IdentityRole() {Name = "Admin"},
					};

                    foreach (var roleName in roles.Select(r=>r.Name!))
                    {
                        if(!await roleManager.RoleExistsAsync(roleName))
						{
							var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));

							if (!roleResult.Succeeded)
								logger.LogError("Failed to Create Role ...");
						}
                    }
                }
				if(!HasUser)
				{
					if (string.IsNullOrWhiteSpace(adminPassword))
					{
						logger.LogWarning("Default user password is not configured. Skipping user seeding. Set SeedUsers:Password or GYM_SEED_PASSWORD.");
						return;
					}

					var MainUser = new ApplicationUser()
					{
						FirstName = "Serag",
						LastName = "Ibrahim",
						UserName = "Sergio",
						Email = "Serag@gmail.com",
						PhoneNumber = "01123456789",

					}; 
                    var MainuserResult = await userManager.CreateAsync(MainUser, adminPassword);

					if(!MainuserResult.Succeeded)
					{
						logger.LogError($"Failed To Seed MainUser {MainUser.UserName}");
						return;
					}
					await userManager.AddToRoleAsync(MainUser, "SuperAdmin");

					var User = new ApplicationUser()
                    {
                        FirstName = "Nader",
                        LastName = "Ibrahim",
                        UserName = "Nader",
                        Email = "Nader@gmail.com",
                        PhoneNumber = "01123457789",

                    };
					var userResult = await userManager.CreateAsync(User, adminPassword);

                    if (!userResult.Succeeded)
                    {
                        logger.LogError($"Failed To Seed User {User.UserName}"); 
                        return;
                    }
					await userManager.AddToRoleAsync(User, "Admin");
                }
				return;
            }
			catch (Exception ex)
			{
				logger.LogError(ex,"Failed To Seed Identity Data");
				return;
			}
        }
    }
}
