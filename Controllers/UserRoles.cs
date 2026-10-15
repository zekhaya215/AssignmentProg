using AssignmentProg.Models;
using AssignmentProg.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentProg.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserRoles : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserRoles(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Display all users and their roles.
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var model = new List<UserRolesViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                model.Add(new UserRolesViewModel
                {
                    UserId = user.Id,
                    Email = user.Email ?? "",
                    UserName = user.UserName ?? "",
                    Roles = roles.ToList()
                });
            }

            return View(model);
        }

        // Display the role management page for a user.
        [HttpGet]
        public async Task<IActionResult> Manage(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest("User ID is required.");
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = _roleManager.Roles.ToList();

            var model = allRoles.Select(role =>
                new ManageUserRolesViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name ?? "",
                    IsSelected = role.Name != null &&
                                 userRoles.Contains(role.Name)
                }).ToList();

            ViewBag.UserId = user.Id;
            ViewBag.UserName = user.UserName;

            return View(model);
        }

        // Save the selected roles.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(
            string userId,
            List<ManageUserRolesViewModel> model)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest("User ID is required.");
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            var selectedRoleNames = model
                .Where(item => item.IsSelected)
                .Select(item => item.RoleName)
                .Distinct()
                .ToList();

            foreach (var roleName in selectedRoleNames)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    ModelState.AddModelError(
                        "",
                        $"The role '{roleName}' does not exist.");

                    return await ReloadManageView(user, model);
                }
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            // Prevent an administrator from removing their own Admin role.
            var isCurrentUser = user.Id == _userManager.GetUserId(User);

            if (isCurrentUser &&
                currentRoles.Contains("Admin") &&
                !selectedRoleNames.Contains("Admin"))
            {
                ModelState.AddModelError(
                    "",
                    "You cannot remove your own Admin role.");

                return await ReloadManageView(user, model);
            }

            var rolesToRemove = currentRoles
                .Except(selectedRoleNames)
                .ToList();

            var rolesToAdd = selectedRoleNames
                .Except(currentRoles)
                .ToList();

            if (rolesToRemove.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(
                    user, rolesToRemove);

                if (!removeResult.Succeeded)
                {
                    ModelState.AddModelError(
                        "",
                        string.Join("; ", removeResult.Errors
                            .Select(error => error.Description)));

                    return await ReloadManageView(user, model);
                }
            }

            if (rolesToAdd.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(
                    user, rolesToAdd);

                if (!addResult.Succeeded)
                {
                    ModelState.AddModelError(
                        "",
                        string.Join("; ", addResult.Errors
                            .Select(error => error.Description)));

                    return await ReloadManageView(user, model);
                }
            }

            TempData["Success"] = "User roles updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task<IActionResult> ReloadManageView(
            ApplicationUser user,
            List<ManageUserRolesViewModel> model)
        {
            ViewBag.UserId = user.Id;
            ViewBag.UserName = user.UserName;

            return View("Manage", model);
        }
    }
}