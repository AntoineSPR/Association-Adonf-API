using Microsoft.AspNetCore.Identity;

namespace AssociationAdonfAPI.Utilities
{
    /// <summary>
    /// Traduit en français les messages d'erreur par défaut (en anglais) d'ASP.NET Core Identity.
    /// </summary>
    public class FrenchIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DefaultError() => new()
        {
            Code = nameof(DefaultError),
            Description = "Une erreur inconnue s'est produite.",
        };

        public override IdentityError ConcurrencyFailure() => new()
        {
            Code = nameof(ConcurrencyFailure),
            Description = "Les données ont été modifiées entre-temps. Veuillez réessayer.",
        };

        public override IdentityError PasswordMismatch() => new()
        {
            Code = nameof(PasswordMismatch),
            Description = "Mot de passe incorrect.",
        };

        public override IdentityError InvalidToken() => new()
        {
            Code = nameof(InvalidToken),
            Description = "Ce lien n'est plus valide. Veuillez refaire une demande.",
        };

        public override IdentityError RecoveryCodeRedemptionFailed() => new()
        {
            Code = nameof(RecoveryCodeRedemptionFailed),
            Description = "Échec de l'utilisation du code de récupération.",
        };

        public override IdentityError LoginAlreadyAssociated() => new()
        {
            Code = nameof(LoginAlreadyAssociated),
            Description = "Un utilisateur avec cette identité de connexion existe déjà.",
        };

        public override IdentityError InvalidUserName(string? userName) => new()
        {
            Code = nameof(InvalidUserName),
            Description = $"Le nom d'utilisateur '{userName}' n'est pas valide.",
        };

        public override IdentityError InvalidEmail(string? email) => new()
        {
            Code = nameof(InvalidEmail),
            Description = $"L'email '{email}' n'est pas valide.",
        };

        public override IdentityError DuplicateUserName(string userName) => new()
        {
            Code = nameof(DuplicateUserName),
            Description = $"Le nom d'utilisateur '{userName}' est déjà utilisé.",
        };

        public override IdentityError DuplicateEmail(string email) => new()
        {
            Code = nameof(DuplicateEmail),
            Description = $"L'email '{email}' est déjà utilisé.",
        };

        public override IdentityError InvalidRoleName(string? role) => new()
        {
            Code = nameof(InvalidRoleName),
            Description = $"Le rôle '{role}' n'est pas valide.",
        };

        public override IdentityError DuplicateRoleName(string role) => new()
        {
            Code = nameof(DuplicateRoleName),
            Description = $"Le rôle '{role}' existe déjà.",
        };

        public override IdentityError UserAlreadyHasPassword() => new()
        {
            Code = nameof(UserAlreadyHasPassword),
            Description = "Cet utilisateur possède déjà un mot de passe défini.",
        };

        public override IdentityError UserLockoutNotEnabled() => new()
        {
            Code = nameof(UserLockoutNotEnabled),
            Description = "Le verrouillage n'est pas activé pour cet utilisateur.",
        };

        public override IdentityError UserAlreadyInRole(string role) => new()
        {
            Code = nameof(UserAlreadyInRole),
            Description = $"L'utilisateur possède déjà le rôle '{role}'.",
        };

        public override IdentityError UserNotInRole(string role) => new()
        {
            Code = nameof(UserNotInRole),
            Description = $"L'utilisateur ne possède pas le rôle '{role}'.",
        };

        public override IdentityError PasswordTooShort(int length) => new()
        {
            Code = nameof(PasswordTooShort),
            Description = $"Le mot de passe doit contenir au moins {length} caractères.",
        };

        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new()
        {
            Code = nameof(PasswordRequiresUniqueChars),
            Description = $"Le mot de passe doit contenir au moins {uniqueChars} caractère(s) distinct(s).",
        };

        public override IdentityError PasswordRequiresNonAlphanumeric() => new()
        {
            Code = nameof(PasswordRequiresNonAlphanumeric),
            Description = "Le mot de passe doit contenir au moins un caractère spécial (ex: !?*.).",
        };

        public override IdentityError PasswordRequiresDigit() => new()
        {
            Code = nameof(PasswordRequiresDigit),
            Description = "Le mot de passe doit contenir au moins un chiffre (0-9).",
        };

        public override IdentityError PasswordRequiresLower() => new()
        {
            Code = nameof(PasswordRequiresLower),
            Description = "Le mot de passe doit contenir au moins une minuscule (a-z).",
        };

        public override IdentityError PasswordRequiresUpper() => new()
        {
            Code = nameof(PasswordRequiresUpper),
            Description = "Le mot de passe doit contenir au moins une majuscule (A-Z).",
        };
    }
}
