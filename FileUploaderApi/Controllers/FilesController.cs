using FileUploaderApi.Data;
using FileUploaderApi.Dtos;
using FileUploaderApi.Models;
using FileUploaderApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FileUploaderApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly IFileService _fileService;

        public FilesController(IFileService fileService) 
        {
            _fileService = fileService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Post([FromForm] IFormFile file)
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            try
            {
                await _fileService.PostFileAsync(file, userEmail);
                return StatusCode(StatusCodes.Status201Created);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }



        [HttpPost("multiple")]
        [Authorize]
        public async Task<IActionResult> PostMultiple([FromForm] List<IFormFile> files)
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            var result = await _fileService.PostMultipleAsync(files, userEmail);

            return result;
        }

        [HttpGet]
        [Authorize]
        public Task<IEnumerable<FileDetailsDto>> Get()
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            return _fileService.GetFilesAsync(userEmail);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            try
            {
                await _fileService.DeleteFileAsync(id, userEmail);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
