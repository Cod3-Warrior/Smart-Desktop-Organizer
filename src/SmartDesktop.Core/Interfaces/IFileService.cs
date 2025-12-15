using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartDesktop.Core
{
    public interface IFileService
    {
        Task MoveFilesAsync(IEnumerable<string> filePaths, string destinationPath);
        bool Exists(string path);
        void CreateDirectory(string path);
    }
}
