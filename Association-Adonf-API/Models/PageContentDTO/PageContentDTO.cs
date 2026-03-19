using System.Text.Json;

namespace AssociationAdonfAPI.Models.PageContentDTOs
{
    public class PageContentUpdateDto
    {
        public JsonDocument Content { get; set; } = JsonDocument.Parse("{}");
    }

    public class PageContentDto
    {
        public Guid Id { get; set; }
        public string Slug { get; set; } = string.Empty;
        public JsonDocument Content { get; set; } = JsonDocument.Parse("{}");
    }
}
