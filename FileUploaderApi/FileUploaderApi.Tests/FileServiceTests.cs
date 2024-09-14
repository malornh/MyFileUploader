using FileUploaderApi.Data;
using FileUploaderApi.Models;
using FileUploaderApi.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Microsoft.AspNetCore.Mvc;

namespace FileUploaderApi.Tests.Services
{
    public class FileServiceTests
    {
        private readonly ApiDbContext _dbContext;
        private readonly FileService _fileService;

        public FileServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApiDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ApiDbContext(options);
            _fileService = new FileService(_dbContext);
        }

        private IFormFile CreateMockFile(string fileName, string fileContent)
        {
            var mockFile = new Mock<IFormFile>();
            var fileStream = new MemoryStream();
            var writer = new StreamWriter(fileStream);
            writer.Write(fileContent);
            writer.Flush();
            fileStream.Position = 0; // Reset the stream position

            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(fileStream.Length);
            mockFile.Setup(f => f.OpenReadStream()).Returns(fileStream);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                    .Returns((Stream targetStream, CancellationToken cancellationToken) =>
                    {
                        return fileStream.CopyToAsync(targetStream, cancellationToken);
                    });

            return mockFile.Object;
        }


        [Fact]
        public async Task PostFileAsync_WhenFileIsValid_ShouldAddFileToDb()
        {
            // Arrange
            var mockFile = CreateMockFile("file.txt", "Content for file");

            var userEmail = "test@test.com";
            var user = new User { Id = 1, Email = userEmail, Password = "123123" };
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // Act: Call the service method
            await _fileService.PostFileAsync(mockFile, userEmail);

            // Assert: Verify the file was added to the database
            var filesInDb = _dbContext.Files.ToList();
            Assert.Single(filesInDb);
            Assert.Equal(mockFile.FileName.Substring(0, mockFile.FileName.LastIndexOf(".")), filesInDb[0].Name);
        }


        [Fact]
        public async Task PostFileAsync_WhenUserIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.txt");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _fileService.PostFileAsync(mockFile.Object, null));
        }

        [Fact]
        public async Task PostFileAsync_WhenFileIsEmpty_ShouldThrowArgumentException()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            var fileName = "emptyfile.txt";

            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(0); // File length is zero

            var userEmail = "test@test.com";
            var user = new User
            {
                Id = 1,
                Email = userEmail,
                Password = "123123" // Make sure to include a password if it's required by the model
            };
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _fileService.PostFileAsync(mockFile.Object, userEmail));

            // Verify that the exception message matches the expected output
            Assert.Equal("File is empty", exception.Message);
        }

        [Fact]
        public async Task DeleteFileAsync_WhenFileBelongsToAnotherUser_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var mockUser1 = new User { Id = 1, Email = "user1@test.com", Password = "123123" };
            var mockUser2 = new User { Id = 2, Email = "user2@test.com", Password = "123123" };
            var mockFile = new FileDetails { Id = 1, Name = "file1", UserId = 2, Extension = ".txt", Data = [1,2] }; // File belongs to user2

            await _dbContext.Users.AddRangeAsync(mockUser1, mockUser2);
            await _dbContext.Files.AddAsync(mockFile);
            await _dbContext.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _fileService.DeleteFileAsync(1, "user1@test.com"));

            // Verify file still exists in the database
            Assert.NotNull(await _dbContext.Files.FindAsync(1));
        }

        [Fact]
        public async Task GetFilesAsync_WhenUserExists_ShouldReturnFiles()
        {
            // Arrange
            var mockUser = new User { Id = 1, Email = "test@example.com" , Password = "123123" };
            var mockFiles = new List<FileDetails>
            {
                new FileDetails { Id = 1, Name = "file1", Extension = ".txt", UserId = 1, Data = [1, 2] },
                new FileDetails { Id = 2, Name = "file2", Extension = ".pdf", UserId = 1, Data = [1, 2] }
            };

            await _dbContext.Users.AddAsync(mockUser);
            await _dbContext.Files.AddRangeAsync(mockFiles);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _fileService.GetFilesAsync("test@example.com");

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, f => f.Name == "file1");
            Assert.Contains(result, f => f.Name == "file2");
        }

        [Fact]
        public async Task PostMultipleAsync_WhenFilesAreValid_ShouldReturn201Created()
        {
            // Arrange
            var files = new List<IFormFile>
    {
        CreateMockFile("file1.txt", "Content for file1"),
        CreateMockFile("file2.txt", "Content for file2")
    };
            var userEmail = "test@test.com";

            var user = new User { Id = 1, Email = userEmail, Password = "123123" };
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _fileService.PostMultipleAsync(files, userEmail);

            // Assert
            Assert.IsType<StatusCodeResult>(result);
            var statusCodeResult = result as StatusCodeResult;
            Assert.Equal(StatusCodes.Status201Created, statusCodeResult.StatusCode);

            // Verify files were added to the database
            var filesInDb = _dbContext.Files.ToList();
            Assert.Equal(2, filesInDb.Count);
            Assert.Contains(filesInDb, f => f.Name == "file1");
            Assert.Contains(filesInDb, f => f.Name == "file2");
        }

        [Fact]
        public async Task PostMultipleAsync_WithSomeEmptyFiles_ShouldProcessValidFiles()
        {
            // Arrange
            var files = new List<IFormFile>
    {
        CreateMockFile("file1.txt", "Content for file1"),
        CreateMockFile("emptyfile.txt", "") // This file is empty
    };
            var userEmail = "test@test.com";

            var user = new User { Id = 1, Email = userEmail, Password = "123123" };
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _fileService.PostMultipleAsync(files, userEmail);

            // Assert
            Assert.IsType<StatusCodeResult>(result);
            var statusCodeResult = result as StatusCodeResult;
            Assert.Equal(StatusCodes.Status201Created, statusCodeResult.StatusCode);

            // Verify that only valid files are processed
            var filesInDb = _dbContext.Files.ToList();
            Assert.Single(filesInDb);
            Assert.Equal("file1", filesInDb[0].Name);
        }

        [Fact]
        public async Task PostMultipleAsync_WhenUserEmailIsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var files = new List<IFormFile> { CreateMockFile("file.txt", "Content") };
            string userEmail = null; // Null user email

            // Act
            var result = await _fileService.PostMultipleAsync(files, userEmail);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            var badRequestResult = result as BadRequestObjectResult;
            Assert.Equal("User email cannot be null or empty", (badRequestResult.Value as dynamic).message);
        }

        [Fact]
        public async Task PostMultipleAsync_WhenNoFilesUploaded_ShouldReturnBadRequest()
        {
            // Arrange
            var files = new List<IFormFile>(); // Empty file list
            var userEmail = "test@test.com";

            // Act
            var result = await _fileService.PostMultipleAsync(files, userEmail);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            var badRequestResult = result as BadRequestObjectResult;
            Assert.Equal("No files were uploaded", (badRequestResult.Value as dynamic).message);
        }

        [Fact]
        public async Task PostMultipleAsync_WhenFileConflicts_ShouldReturnConflict()
        {
            // Arrange
            var fileName = "conflictfile.txt";
            var files = new List<IFormFile> { CreateMockFile(fileName, "Content") };
            var userEmail = "test@test.com";

            var user = new User { Id = 1, Email = userEmail, Password = "123123" };
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // Add a file to simulate a conflict
            await _fileService.PostFileAsync(CreateMockFile(fileName, "Existing Content"), userEmail);

            // Act
            var result = await _fileService.PostMultipleAsync(files, userEmail);

            // Assert
            Assert.IsType<ConflictObjectResult>(result);
            var conflictResult = result as ConflictObjectResult;
            Assert.Equal("File with this name and extension already exists for the current user", (conflictResult.Value as dynamic).message);
        }
    }
}
