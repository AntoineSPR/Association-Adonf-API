using AssociationAdonfAPI.Models.PageContentDTOs;
using AssociationAdonfAPI.Services;
using AssociationAdonfAPI.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssociationAdonfAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PageContentController : ControllerBase
    {
        private readonly PageContentService _pageContentService;

        public PageContentController(PageContentService pageContentService)
        {
            _pageContentService = pageContentService;
        }

        [HttpGet("{slug}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPageContent(string slug)
        {
            var content = await _pageContentService.GetBySlugAsync(slug);
            if (content == null)
            {
                // Return an empty object instead of 404 so that the frontend can build 
                // the UI for new pages natively without getting fetch errors.
                return Ok(new PageContentDto { Slug = slug.ToLower() });
            }
            return Ok(content);
        }

        [HttpPut("{slug}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePageContent(string slug, [FromBody] PageContentUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _pageContentService.CreateOrUpdateAsync(slug, dto);
            return Ok(result);
        }
    }
}
