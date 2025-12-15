using System.Collections.Generic;
using System.IO;

namespace SmartDesktop.Core
{
    public interface IFileSystem
    {
        bool Exists(string path);
        void Move(string source, string dest);
        bool IsDirectory(string path);
        string[] GetFiles(string path);
    }

    public class FileSystemWrapper : IFileSystem
    {
        public bool Exists(string path) => File.Exists(path);
        public void Move(string source, string dest) => File.Move(source, dest);
        public bool IsDirectory(string path) => Directory.Exists(path);
        public string[] GetFiles(string path) => Directory.GetFiles(path);
    }
}
