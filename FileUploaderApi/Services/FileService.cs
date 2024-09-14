using FileUploaderApi.Data;
using FileUploaderApi.Dtos;
using FileUploaderApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FileUploaderApi.Services
{
    public class FileService : IFileService
    {
        private readonly ApiDbContext _dbContext;

        public FileService(ApiDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task PostFileAsync(IFormFile file, string userEmail)
        {
            if (string.IsNullOrEmpty(userEmail)) throw new ArgumentNullException(nameof(userEmail));
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            var fileDetails = new FileDetails
            {
                Name = file.FileName.Substring(0, file.FileName.LastIndexOf(".")),
                Extension = Path.GetExtension(file.FileName),
                UserId = user.Id
            };

            var existingFile = await _dbContext.Files
                .FirstOrDefaultAsync(f => f.Name == fileDetails.Name && f.Extension == fileDetails.Extension && f.UserId == fileDetails.UserId);
            if (existingFile != null) throw new InvalidOperationException("File with this name and extension already exists for the current user");

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                if (stream.Length == 0) throw new ArgumentException("File is empty");

                fileDetails.Data = stream.ToArray();
            }

            try
            {
                _dbContext.Files.Add(fileDetails);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IActionResult> PostMultipleAsync(List<IFormFile> files, string userEmail)
        {
            if (string.IsNullOrEmpty(userEmail)) return new BadRequestObjectResult(new { message = "User email cannot be null or empty" });

            if (files == null || files.Count == 0) return new BadRequestObjectResult(new { message = "No files were uploaded" });

            foreach (var file in files)
            {
                if (file.Length == 0) continue; // Skip empty files

                try
                {
                    await PostFileAsync(file, userEmail);
                }
                catch (InvalidOperationException ex)
                {
                    return new ConflictObjectResult(new { message = ex.Message });
                }
                catch (ArgumentException ex)
                {
                    return new BadRequestObjectResult(new { message = ex.Message });
                }
            }

            return new StatusCodeResult(StatusCodes.Status201Created);
        }


        public async Task<IEnumerable<FileDetailsDto>> GetFilesAsync(string userEmail)
        {
            if (string.IsNullOrEmpty(userEmail)) throw new ArgumentNullException(nameof(userEmail));
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            var userFiles = await _dbContext.Files.Where(f => f.UserId == user.Id)
                .Select(f => new FileDetailsDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Extension = f.Extension
                })
                .ToListAsync();
            return userFiles;
        }

        public async Task DeleteFileAsync(int fileId, string userEmail)
        {
            if (string.IsNullOrEmpty(userEmail)) throw new ArgumentNullException(nameof(userEmail));
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            var fileToDelete = _dbContext.Files.FirstOrDefault(f => f.Id == fileId);
            if (fileToDelete == null) throw new ArgumentException("File not found");
            if (fileToDelete.UserId != user.Id) throw new InvalidOperationException("Cannot delete files of other users");

            _dbContext.Files.Remove(fileToDelete);
            await _dbContext.SaveChangesAsync();
        }

    }
}
