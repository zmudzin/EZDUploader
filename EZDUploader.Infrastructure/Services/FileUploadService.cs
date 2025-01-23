using EZDUploader.Core.Interfaces;
using EZDUploader.Core.Models;
using EZDUploader.Core.Interfaces;
using EZDUploader.Core.Models;

namespace EZDUploader.Infrastructure.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IEzdApiService _ezdService;
        private readonly List<UploadFile> _files = new();

        public FileUploadService(IEzdApiService ezdService)
        {
            _ezdService = ezdService;
        }

        public IReadOnlyList<UploadFile> Files => _files.AsReadOnly();

        public async Task AddFiles(string[] filePaths)
        {
            foreach (var path in filePaths)
            {
                if (_files.Any(f => f.FilePath == path))
                    continue;

                var fileInfo = new FileInfo(path);
                _files.Add(new UploadFile
                {
                    FilePath = path,
                    FileName = fileInfo.Name,
                    FileSize = fileInfo.Length,
                    AddedDate = DateTime.Now,
                    Status = UploadStatus.Pending
                });
            }
        }
        public Task RemoveFiles(IEnumerable<UploadFile> files)
        {
            foreach (var file in files.ToList())
            {
                _files.Remove(file);
            }
            return Task.CompletedTask;
        }

        public async Task UploadFiles(int idKoszulki, IEnumerable<UploadFile> files, IProgress<(int fileIndex, int totalFiles, int progress)> progress = null)
        {
            var filesToUpload = files.ToList();
            for (int i = 0; i < filesToUpload.Count; i++)
            {
                var file = filesToUpload[i];
                try
                {
                    file.Status = UploadStatus.Uploading;
                    byte[] content = await File.ReadAllBytesAsync(file.FilePath);

                    var docId = await _ezdService.DodajZalacznik(content, file.FileName, _ezdService.CurrentUserId.Value);

                    file.Status = UploadStatus.Completed;
                    progress?.Report((i + 1, filesToUpload.Count, 100));
                }
                catch (Exception ex)
                {
                    file.Status = UploadStatus.Failed;
                    file.ErrorMessage = ex.Message;
                    throw;
                }
            }
        }

        public void ClearFiles()
        {
            _files.Clear();
        }
    }
}
