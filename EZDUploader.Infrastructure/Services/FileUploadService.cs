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
            var errors = new List<(UploadFile File, Exception Error)>();

            for (int i = 0; i < filesToUpload.Count; i++)
            {
                var file = filesToUpload[i];
                try
                {
                    file.Status = UploadStatus.Uploading;
                    progress?.Report((i + 1, filesToUpload.Count, 0));

                    // 1. Wczytaj plik
                    byte[] content = await File.ReadAllBytesAsync(file.FilePath);
                    progress?.Report((i + 1, filesToUpload.Count, 30));

                    // 2. Dodaj załącznik (zwraca ContentId)
                    var idZalacznika = await _ezdService.DodajZalacznik(
                        content,
                        file.FileName,
                        _ezdService.CurrentUserId.Value
                    );
                    progress?.Report((i + 1, filesToUpload.Count, 60));

                    // 3. Zarejestruj dokument (wiąże załącznik z koszulką)
                    await _ezdService.RejestrujDokument(
                        file.FileName,
                        idKoszulki,
                        idZalacznika,
                        _ezdService.CurrentUserId.Value
                    );

                    file.Status = UploadStatus.Completed;
                    progress?.Report((i + 1, filesToUpload.Count, 100));
                }
                catch (Exception ex)
                {
                    file.Status = UploadStatus.Failed;
                    file.ErrorMessage = ex.Message;
                    errors.Add((file, ex));
                }
            }

            if (errors.Any())
            {
                var errorMessage = string.Join("\n", errors.Select(e => $"- {e.File.FileName}: {e.Error.Message}"));
                throw new AggregateException($"Błąd podczas wysyłania plików:\n{errorMessage}", errors.Select(e => e.Error));
            }
        }

        public void ClearFiles()
        {
            _files.Clear();
        }
    }
}
