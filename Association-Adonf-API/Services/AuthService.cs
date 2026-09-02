using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using AssociationAdonfAPI.Context;
using AssociationAdonfAPI.Models;
using AssociationAdonfAPI.Utilities;

namespace AssociationAdonfAPI.Services
{
    public class AuthService
    {
        private readonly DataContext context;
        private readonly UserManager<UserApp> userManager;

        public AuthService(
            DataContext context,
            UserManager<UserApp> userManager
        )
        {
            this.context = context;
            this.userManager = userManager;
        }

        public async Task<UserResponseDTO?> Register(UserCreateDTO model)
        {
            // Vérifier si l'adresse e-mail est déjà utilisée
            bool isEmailAlreadyUsed = await IsEmailAlreadyUsedAsync(model.Email);
            if (isEmailAlreadyUsed)
            {
                throw new Exception("Email déjà utilisé");
            }

            try
            {
                // Créer un nouvel utilisateur en utilisant les données du modèle et la base de données contextuelle
                UserApp newUser = model.ToUserApp();

                // Tenter de créer un nouvel utilisateur avec le gestionnaire d'utilisateurs
                IdentityResult result = await userManager.CreateAsync(newUser, model.Password);


                // Vérifier si la création de l'utilisateur a échoué
                if (!result.Succeeded)
                {
                    // Si la création a échoué, ajouter les erreurs au modèle d'état et renvoyer une exception
                    var errors = Enumerable.Empty<string>();
                    foreach (var error in result.Errors)
                    {
                        errors.Append(error.Description);
                        throw new Exception(error.Description);
                    }
                }

                // Tenter d'ajouter l'utilisateur aux rôles spécifiés dans le modèle
                IdentityResult roleResult = await userManager.AddToRolesAsync(
                    user: newUser,
                    roles: ["Client"]
                );

                return newUser.ToUserResponseDTO();
            }
            catch
            {
                throw new Exception("Une erreur s'est produite");
            }
        }

        public async Task<UserResponseDTO> Update(UserCreateDTO model, ClaimsPrincipal UserPrincipal)
        {
            try
            {
                var user = UserService.GetUserFromClaim(UserPrincipal, context);
                if (user is null)
                {
                    throw new Exception("Compte introuvable");
                }

                user = model.ToSimpleUser(user);

                await context.SaveChangesAsync();

                return user.ToUserResponseDTO();
            }
            catch
            {
                throw;
            }
        }
        public async Task<bool> ChangePassword(ChangePasswordDTO passwordData, ClaimsPrincipal userPrincipal)
        {
            try
            {
                var user = UserService.GetUserFromClaim(userPrincipal, context);
                if (user is null)
                {
                    throw new Exception("Utilisateur non trouvé");
                }

                var result = await userManager.ChangePasswordAsync(user, passwordData.CurrentPassword, passwordData.NewPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Erreur lors du changement de mot de passe : {errors}");
                }

                return true;
            }
            catch
            {
                throw;
            }
        }


        public async Task<object> Login(UserLoginDTO model)
        {
            try
            {
                var user = await userManager.FindByEmailAsync(model.Email);

                // Message identique que l'email soit inconnu ou le mot de passe erroné,
                // pour ne pas révéler si un compte existe (et rester cohérent côté UI).
                if (user == null || !await userManager.CheckPasswordAsync(user, model.Password))
                {
                    throw new UnauthorizedAccessException("Email ou mot de passe incorrect.");
                }

                var userRoles = await userManager.GetRolesAsync(user);

                return new LoginResponseDTO
                {
                    Token = await GenerateAccessTokenAsync(user),
                    RefreshToken = await GenerateRefreshTokenAsync(user),
                    User = user.ToUserResponseDTO(userRoles.ToList()),
                };
            }
            catch
            {
                throw;
            }
        }

        public async Task<LoginResponseDTO> RefreshAsync(string rawRefreshToken)
        {
            if (string.IsNullOrWhiteSpace(rawRefreshToken))
            {
                throw new UnauthorizedAccessException("Session expirée, veuillez vous reconnecter");
            }

            var tokenHash = HashToken(rawRefreshToken);
            var existingToken = await context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

            if (existingToken == null || !existingToken.IsActive)
            {
                throw new UnauthorizedAccessException("Session expirée, veuillez vous reconnecter");
            }

            // Rotation : on révoque le jeton utilisé et on en émet un nouveau couple.
            existingToken.RevokedAt = DateTime.UtcNow;

            var user = existingToken.User;
            var userRoles = await userManager.GetRolesAsync(user);

            return new LoginResponseDTO
            {
                Token = await GenerateAccessTokenAsync(user),
                RefreshToken = await GenerateRefreshTokenAsync(user),
                User = user.ToUserResponseDTO(userRoles.ToList()),
            };
        }

        public async Task RevokeRefreshTokenAsync(string rawRefreshToken)
        {
            if (string.IsNullOrWhiteSpace(rawRefreshToken))
            {
                return;
            }

            var tokenHash = HashToken(rawRefreshToken);
            var existingToken = await context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

            if (existingToken != null && existingToken.RevokedAt == null)
            {
                existingToken.RevokedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
        }

        public async Task<string> GenerateRefreshTokenAsync(UserApp user)
        {
            var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            context.RefreshTokens.Add(new RefreshToken
            {
                TokenHash = HashToken(rawToken),
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(Env.REFRESH_TOKEN_VALIDITY_DAYS),
            });

            await context.SaveChangesAsync();

            return rawToken;
        }

        private static string HashToken(string token)
        {
            return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }

        public async Task<string> GenerateAccessTokenAsync(UserApp user)
        {
            try
            {
                var securityKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(Env.JWT_KEY)
                );
                var credentials = new SigningCredentials(
                    key: securityKey,
                    algorithm: SecurityAlgorithms.HmacSha256
                );

                var userRoles = await userManager.GetRolesAsync(user);

                var authClaims = new List<Claim>
            {
                new Claim(type: ClaimTypes.Email, value: user.Email ?? string.Empty),
                new Claim(type: ClaimTypes.NameIdentifier, value: user.Id.ToString()),
            };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(type: ClaimTypes.Role, value: userRole));
                }

                var token = new JwtSecurityToken(
                    issuer: Env.API_BACK_URL,
                    audience: Env.API_BACK_URL,
                    claims: authClaims,
                    expires: DateTime.UtcNow.AddMinutes(Env.ACCESS_TOKEN_VALIDITY_MINUTES),
                    signingCredentials: credentials
                );

                context.Entry(user).State = EntityState.Modified;

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch
            {
                throw;
            }
        }

        private async Task<bool> IsEmailAlreadyUsedAsync(string email)
        {
            var existingUser = await userManager.FindByEmailAsync(email);
            return existingUser != null;
        }

        public async Task<bool> ResetPassword(ResetPasswordDTO model)
        {
            try
            {
                var user = await userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    throw new Exception("Utilisateur non trouvé");
                }

                var result = await userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Erreur lors de la réinitialisation : {errors}");
                }

                return true;
            }
            catch
            {
                throw;
            }
        }               
    }
}
