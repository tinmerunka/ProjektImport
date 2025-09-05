using InventoryManagementAPI.DTOs.InventoryManagementAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Add this if you want authentication
    public class ImagesController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public ImagesController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpPost("upload")]
        public async Task<ActionResult<ApiResponse<string>>> UploadImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "No file provided"
                    });

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Invalid file type. Allowed: JPG, PNG, GIF, WEBP"
                    });

                // Validate file size (max 5MB)
                if (file.Length > 5 * 1024 * 1024)
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "File too large. Maximum size is 5MB."
                    });

                // Create the full path for the images directory
                var imagesPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "images", "products");

                // Create directory if it doesn't exist
                if (!Directory.Exists(imagesPath))
                {
                    Directory.CreateDirectory(imagesPath);
                    Console.WriteLine($"Created directory: {imagesPath}");
                }

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(imagesPath, fileName);

                Console.WriteLine($"Saving file to: {filePath}");

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return relative URL that frontend can use
                var imageUrl = $"/images/products/{fileName}";

                Console.WriteLine($"File saved successfully. Returning URL: {imageUrl}");

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Data = imageUrl,
                    Message = "Image uploaded successfully"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading image: {ex.Message}");
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Upload failed: {ex.Message}"
                });
            }
        }

        [HttpGet("serve/{fileName}")]
        public ActionResult ServeImage(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_environment.WebRootPath, "images", "products", fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound("Image not found");
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                var contentType = GetContentType(fileName);

                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serving image: {ex.Message}");
                return NotFound();
            }
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }
    }
}