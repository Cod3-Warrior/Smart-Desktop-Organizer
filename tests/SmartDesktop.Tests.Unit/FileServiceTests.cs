using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using SmartDesktop.Core;
using Xunit;

namespace SmartDesktop.Tests.Unit
{
    public class FileServiceTests
    {
        private readonly Mock<IPermissionService> _permissionMock;
        private readonly Mock<IFileSystem> _fileSystemMock;
        private readonly FileService _fileService;

        public FileServiceTests()
        {
            _permissionMock = new Mock<IPermissionService>();
            _fileSystemMock = new Mock<IFileSystem>();
            _fileService = new FileService(_permissionMock.Object, _fileSystemMock.Object);
        }

        [Fact]
        public async Task MoveFilesAsync_ShouldHandle500Files()
        {
            // Arrange
            var files = new List<string>();
            for (int i = 0; i < 500; i++)
            {
                files.Add($"C:\\Desktop\\shortcut{i}.lnk");
            }
            var destination = "C:\\TargetFolder";

            _permissionMock.Setup(p => p.HasWriteAccess(destination)).Returns(true);
            _fileSystemMock.Setup(fs => fs.Exists(It.IsAny<string>())).Returns(true);
            _fileSystemMock.Setup(fs => fs.Move(It.IsAny<string>(), It.IsAny<string>()));

            // Act
            await _fileService.MoveFilesAsync(files, destination);

            // Assert
            // Verify that Move was called 500 times
            _fileSystemMock.Verify(fs => fs.Move(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(500));
        }

        [Fact]
        public async Task MoveFilesAsync_ShouldThrowWhenPermissionDenied_AtDestination()
        {
            // Arrange
            var files = new List<string> { "C:\\Desktop\\file1.txt" };
            var destination = "C:\\Restricted";

            _permissionMock.Setup(p => p.HasWriteAccess(destination)).Returns(false);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
                _fileService.MoveFilesAsync(files, destination));
        }

        [Fact]
        public async Task MoveFilesAsync_ShouldThrowWhenPermissionDenied_ForSingleFile()
        {
            // Arrange
            var files = new List<string> { "C:\\Desktop\\locked.txt" };
            var destination = "C:\\Target";

            _permissionMock.Setup(p => p.HasWriteAccess(destination)).Returns(true);
            _fileSystemMock.Setup(fs => fs.Exists(It.IsAny<string>())).Returns(true);
            
            // Simulate unauthorized access on file move
            _fileSystemMock.Setup(fs => fs.Move(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new UnauthorizedAccessException("Locked"));

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
                _fileService.MoveFilesAsync(files, destination));
        }
    }
}
