using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SmartDesktop.Core
{
    public class FileService : IFileService
    {
        private readonly IPermissionService _permissionService;
        private readonly IFileSystem _fileSystem;

        public FileService(IPermissionService permissionService, IFileSystem fileSystem)
        {
            _permissionService = permissionService;
            _fileSystem = fileSystem;
        }

        public async Task MoveFilesAsync(IEnumerable<string> filePaths, string destinationPath)
        {
            if (!_permissionService.HasWriteAccess(destinationPath))
            {
                throw new UnauthorizedAccessException($"Access denied to destination: {destinationPath}");
            }

            foreach (var file in filePaths)
            {
                if (!_fileSystem.Exists(file)) continue;

                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(destinationPath, fileName);

                try
                {
                    await Task.Run(() => _fileSystem.Move(file, destFile));
                }
                catch (UnauthorizedAccessException)
                {
                    throw;
                }
            }
        }

        public bool Exists(string path) => _fileSystem.Exists(path);
        
        public void CreateDirectory(string path) 
        { 
             // Ideally this would also be in IFileSystem, but keeping simple for now
             Directory.CreateDirectory(path); 
        }
    }
}
