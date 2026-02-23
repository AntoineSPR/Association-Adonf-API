using AssociationAdonfAPI.Models;
using AssociationAdonfAPI.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Security.Claims;

namespace AssociationAdonfAPI.Controllers;

[Route("[controller]")]
[ApiController]
public class FileController : ControllerBase
{
    private readonly IWebHostEnvironment env;

    public FileController(IWebHostEnvironment env)
    {
        this.env = env;
    }

    [HttpPost]
    public async Task<ActionResult<string>> UploadAvatar(
            IFormFile file
        )
    {
        if (file == null)
        {
            return BadRequest("Aucun fichier téléversé");
        }

        //verifier si le type est image
        var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/bmp", "image/webp" };
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };

        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (
            !allowedMimeTypes.Contains(file.ContentType)
            || !allowedExtensions.Contains(fileExtension)
        )
        {
            return BadRequest();
        }

            using var inputStream = file.OpenReadStream();
            using var image = await Image.LoadAsync(inputStream);

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(400, 600),
                Mode = ResizeMode.Max // Garde les proportions
            }));

            using var outputStream = new MemoryStream();
            //await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = 85 });
            await image.SaveAsWebpAsync(outputStream);
            outputStream.Seek(0, SeekOrigin.Begin);

            string fileName = Guid.NewGuid() + "_avatar.webp";// + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(env.WebRootPath, "Images", fileName); // wwwroot + images + filename ???

            using (var stream = System.IO.File.Create(filePath))
            {
                await outputStream.CopyToAsync(stream);
                //await file.CopyToAsync(stream);
            }

            var url = $"/images/{fileName}";

            return Ok(url);
        }
    
}
