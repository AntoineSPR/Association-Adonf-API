using Microsoft.AspNetCore.Identity;
using AssociationAdonfAPI.Context;
using AssociationAdonfAPI.Models;
using System.Security.Claims;

namespace AssociationAdonfAPI.Services
{
    public class UserService(
            DataContext context,
            UserManager<UserApp> userManager
                )
    {
        public async Task<UserResponseDTO?> GetUserByEmail(string email)
        {
            UserApp? user = context.Users.FirstOrDefault(x => x.Email == email);
            List<string> roles = new();
            if (user != null)
            {
                roles = (await userManager.GetRolesAsync(user)).ToList();
            }
            UserResponseDTO? userResponse = user?.ToUserResponseDTO(roles);
            return userResponse;
        }
        public static UserApp? GetUserFromClaim(
            ClaimsPrincipal userClaim,
            DataContext context
        )
        {
            string? userEmail = userClaim
                .Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)
                ?.Value;
            if (userEmail == null)
                return null;
            var user = context
                .Users
                .FirstOrDefault(x => x.Email == userEmail);

            if (user == null)
                return null;
            return user;
        }

        public static async Task<(UserApp? user, bool isNull)> CheckUserNullByEmail(
            string email,
            UserManager<UserApp> _userManager
        )
        {
            var user = await _userManager.FindByEmailAsync(email);

            return (user, user is null);
        }

        public static async Task<(UserApp? user, bool isNull)> CheckUserNullByUserId(
            string id,
            UserManager<UserApp> _userManager
        )
        {
            var user = await _userManager.FindByIdAsync(id);

            return (user, user is null);
        }
    }
}
