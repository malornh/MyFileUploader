using FileUploaderApi.Dtos;
using FileUploaderApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileUploaderApi.Services
{
    public interface IFileService
    {
        public Task PostFileAsync(IFormFile file, string userEmail);
        public Task<IActionResult> PostMultipleAsync(List<IFormFile> files, string userEmail);
        public Task<IEnumerable<FileDetailsDto>> GetFilesAsync(string userEmail);
        public Task DeleteFileAsync(int fileId, string userEmail);
    }
}
