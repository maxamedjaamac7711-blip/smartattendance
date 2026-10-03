using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using SmartAttendanceSystem.Properties.Model;
using System.Security.Claims;

namespace SmartAttendanceSystem
{
    public class RoleClaimsTransformation : IClaimsTransformation
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public RoleClaimsTransformation(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (principal.Identity is not { IsAuthenticated: true })
                return principal;

            var identity = principal.Identity as ClaimsIdentity;
            var userId = identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return principal;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return principal;

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                if (!identity!.HasClaim(ClaimTypes.Role, role))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
                }
            }

            return principal;
        }
    }
}