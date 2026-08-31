using System.ComponentModel.DataAnnotations;

namespace AssociationAdonfAPI.Models
{
    public class RefreshToken : BaseModel
    {
        [Required]
        public string TokenHash { get; set; } = string.Empty;

        public Guid UserId { get; set; }
        public UserApp User { get; set; } = null!;

        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }

        public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
    }
}
