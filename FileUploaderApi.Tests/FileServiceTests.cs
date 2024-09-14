using Xunit;
using Moq;
using FileUploaderApi.Services;
using FileUploaderApi.Data;
using FileUploaderApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;


namespace FileUploaderApi.Tests
{
    public class FileServiceTests
    {
        private readonly Mock<ApiDbContext> _mockDbContext;
        private readonly FileService _fileService;

        public FileServiceTests()
        {
            _mockDbContext = new Mock<ApiDbContext>();
            _fileService = new FileService(_mockDbContext.Object);
        }

        [Fact]
        public async Task PostFileAsync_ShouldAddFileToDatabase_WhenFileIsValid()
        {
            // Arrange
            var mockUser = new User { Id = 1, Email = "test@example.com" };
            var mockFile = new Mock<IFormFile>();

            // Mock file
            var fileName = "testfile.txt";
            var memoryStream = new MemoryStream();
            var writer = new StreamWriter(memoryStream);
            writer.Write("Test file content");
            writer.Flush();
            memoryStream.Position = 0;

            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Mock DbContext
            _mockDbContext.Setup(db => db.Users.FirstOrDefaultAsync(
        It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
        It.IsAny<CancellationToken>()))
        .ReturnsAsync(mockUser);

            _mockDbContext.Setup(db => db.Files.Add(It.IsAny<FileDetails>())).Verifiable();
            _mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            await _fileService.PostFileAsync(mockFile.Object, "test@example.com");

            // Assert
            _mockDbContext.Verify(db => db.Files.Add(It.IsAny<FileDetails>()), Times.Once);
            _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PostFileAsync_ShouldThrowArgumentNullException_WhenUserEmailIsNull()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _fileService.PostFileAsync(mockFile.Object, null));
        }

        [Fact]
        public async Task PostFileAsync_ShouldThrowArgumentException_WhenFileIsEmpty()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("emptyfile.txt");
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var mockUser = new User { Id = 1, Email = "test@example.com" };

            _mockDbContext.Setup(db => db.Users.FirstOrDefaultAsync(
        It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
        It.IsAny<CancellationToken>()))
        .ReturnsAsync(mockUser);


            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _fileService.PostFileAsync(mockFile.Object, "test@example.com"));
        }

        [Fact]
        public async Task PostMultipleAsync_ShouldReturnCreatedStatus_WhenFilesAreValid()
        {
            // Arrange
            var mockFiles = new List<IFormFile>
    {
        new Mock<IFormFile>().Object,
        new Mock<IFormFile>().Object
    };

            var mockUser = new User { Id = 1, Email = "test@example.com" };

            _mockDbContext.Setup(db => db.Users.FirstOrDefaultAsync(
        It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
        It.IsAny<CancellationToken>()))
        .ReturnsAsync(mockUser);


            // Act
            var result = await _fileService.PostMultipleAsync(mockFiles, "test@example.com");

            // Assert
            Assert.IsType<StatusCodeResult>(result);
            var statusCodeResult = result as StatusCodeResult;
            Assert.Equal(StatusCodes.Status201Created, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task PostMultipleAsync_ShouldReturnBadRequest_WhenNoFilesProvided()
        {
            // Act
            var result = await _fileService.PostMultipleAsync(null, "test@example.com");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PostMultipleAsync_ShouldReturnBadRequest_WhenUserEmailIsNull()
        {
            // Arrange
            var mockFiles = new List<IFormFile> { new Mock<IFormFile>().Object };

            // Act
            var result = await _fileService.PostMultipleAsync(mockFiles, null);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }


        [Fact]
        public async Task GetFilesAsync_ShouldReturnFiles_WhenUserEmailIsValid()
        {
            // Arrange
            var mockUser = new User { Id = 1, Email = "test@example.com" };
            var mockFiles = new List<FileDetails>
    {
        new FileDetails { Id = 1, Name = "file1", Extension = ".txt", UserId = 1 },
        new FileDetails { Id = 2, Name = "file2", Extension = ".pdf", UserId = 1 }
    };

            _mockDbContext.Setup(db => db.Users.FirstOrDefaultAsync(
          It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
          It.IsAny<CancellationToken>()))
          .ReturnsAsync(mockUser);


            _mockDbContext.Setup(db => db.Files.Where(f => f.UserId == 1))
                          .Returns(mockFiles.AsQueryable());

            // Act
            var result = await _fileService.GetFilesAsync("test@example.com");

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, f => f.Name == "file1");
            Assert.Contains(result, f => f.Name == "file2");
        }


        [Fact]
        public async Task DeleteFileAsync_ShouldDeleteFile_WhenFileAndUserExist()
        {
            // Arrange
            var mockUser = new User { Id = 1, Email = "test@example.com" };
            var mockFile = new FileDetails { Id = 1, Name = "file1", UserId = 1 };

            _mockDbContext.Setup(db => db.Users.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser);


            _mockDbContext.Setup(db => db.Files.FirstOrDefault(f => f.Id == 1))
                      .Returns(mockFile);

            _mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
                          .ReturnsAsync(1);

            // Act
            await _fileService.DeleteFileAsync(1, "test@example.com");

            // Assert
            _mockDbContext.Verify(db => db.Files.Remove(mockFile), Times.Once);
            _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
