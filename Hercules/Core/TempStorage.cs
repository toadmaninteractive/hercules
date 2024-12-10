using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace Hercules
{
    public interface IFile
    {
        string? FileName { get; } // Maybe null if not loaded

        [MemberNotNullWhen(returnValue: true, member: nameof(FileName))]
        bool IsLoaded { get; }

        Task LoadAsync();
    }

    public class TempFile : IFile
    {
        public string FileName { get; }

        public bool IsLoaded => true;

        public long Length => new FileInfo(FileName).Length;

        public Task LoadAsync() => Task.CompletedTask;

        public TempFile(string fileName)
        {
            FileName = fileName;
        }
    }

    public sealed class TempStorage : IDisposable
    {
        public string TempPath { get; }

        bool disposed;

        public TempStorage(string tempPath)
        {
            TempPath = tempPath;
        }

        public TempFile CreateFile(string fileName, Stream stream)
        {
            var targetDir = Path.Combine(TempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(targetDir);
            var newPath = Path.Combine(targetDir, fileName);
            using (var fileStream = File.Create(newPath))
            {
                stream.CopyTo(fileStream);
            }
            return new TempFile(newPath);
        }

        public TempFile CreateFile(string fileName, string sourceFilePath)
        {
            var targetDir = Path.Combine(TempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(targetDir);
            var newPath = Path.Combine(targetDir, fileName);
            File.Copy(sourceFilePath, newPath);
            return new TempFile(newPath);
        }

        public TempFile CreateFile(string sourceFilePath)
        {
            return CreateFile(Path.GetFileName(sourceFilePath), sourceFilePath);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                try
                {
                    if (Directory.Exists(TempPath))
                        Directory.Delete(TempPath, true);
                }
                catch
                {
                }
                GC.SuppressFinalize(this);
            }
        }

        ~TempStorage()
        {
            Dispose();
        }

        public static TempStorage Create()
        {
            return new TempStorage(Path.Combine(Path.GetTempPath(), "Hercules", Path.GetRandomFileName()));
        }
    }
}
