using System.Security.Claims;
using AssociationAdonfAPI.Context;
using AssociationAdonfAPI.Models;

namespace AssociationAdonfAPI.Utilities
{
    public class CheckUser
    {
        public static Guid? GetUserIdFromClaim(ClaimsPrincipal userClaim)
        {
            var userIdString = userClaim.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdString, out var userId))
            {
                return userId;
            }
            return null;
        }

    }
}
