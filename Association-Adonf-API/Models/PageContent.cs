using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace AssociationAdonfAPI.Models
{
    public class PageContent : BaseModel
    {
        [Required]
        [MaxLength(255)]
        public string Slug { get; set; } = string.Empty;

        [Required]
        public JsonDocument Content { get; set; } = JsonDocument.Parse("{}");
    }
}
