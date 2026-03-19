using AssociationAdonfAPI.Context;
using AssociationAdonfAPI.Models;
using AssociationAdonfAPI.Models.PageContentDTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AssociationAdonfAPI.Services
{
    public class PageContentService
    {
        private readonly DataContext _context;

        public PageContentService(DataContext context)
        {
            _context = context;
        }

        public async Task<PageContentDto?> GetBySlugAsync(string slug)
        {
            var content = await _context.PageContents
                .Where(p => p.Slug == slug.ToLower() && !p.IsArchived)
                .FirstOrDefaultAsync();

            if (content == null) return null;

            return new PageContentDto
            {
                Id = content.Id,
                Slug = content.Slug,
                Content = content.Content
            };
        }

        public async Task<PageContentDto> CreateOrUpdateAsync(string slug, PageContentUpdateDto updateDto)
        {
            // Clean slug
            var cleanSlug = slug.ToLower().Trim();

            var existing = await _context.PageContents
                .Where(p => p.Slug == cleanSlug && !p.IsArchived)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                existing.Content = updateDto.Content;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                existing = new PageContent
                {
                    Slug = cleanSlug,
                    Content = updateDto.Content
                };
                _context.PageContents.Add(existing);
            }

            await _context.SaveChangesAsync();

            return new PageContentDto
            {
                Id = existing.Id,
                Slug = existing.Slug,
                Content = existing.Content
            };
        }
    }
}
