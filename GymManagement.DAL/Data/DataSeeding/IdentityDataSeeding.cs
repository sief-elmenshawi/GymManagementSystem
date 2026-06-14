using GymManagement.DAL.Data.Models;
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
        public static async Task SeedAsync(RoleManager<IdentityRole> roleManager , UserManager<ApplicationUser> userManager,ILogger logger , CancellationToken ct = default)
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

                    foreach (var roleName in roles.Select(r=>r.Name))
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
					var MainUser = new ApplicationUser()
					{
						FirstName = "Serag",
						LastName = "Ibrahim",
						UserName = "Sergio",
						Email = "Serag@gmail.com",
						PhoneNumber = "01123456789",

					};
					 var userResult = await userManager.CreateAsync(MainUser, "P@ssw0rd");
					await userManager.AddToRoleAsync(MainUser, "SuperAdmin");

					if(!userResult.Succeeded)
					{
						logger.LogError("Failed To Seed Users");
						return;
					}
				}
				return;
            }
			catch (Exception ex)
			{
				logger.LogError("Failed To Seed Identity Data");
				throw;
			}
        }
    }
}
