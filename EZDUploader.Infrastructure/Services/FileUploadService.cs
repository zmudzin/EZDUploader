using EZDUploader.Core.Interfaces;
using EZDUploader.Core.Models;
using EZDUploader.Core.Interfaces;
using EZDUploader.Core.Models;
using EZDUploader.Core.Validators;
using System.Diagnostics;

namespace EZDUploader.Infrastructure.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IEzdApiService _ezdService;
        private readonly IFileValidator _fileValidator;
        private readonly List<UploadFile> _files = new();
        private int _currentSortOrder = 0;

        public FileUploadService(IEzdApiService ezdService, IFileValidator fileValidator)
        {
            _ezdService = ezdService;
            _fileValidator = fileValidator;
        }

        public IReadOnlyList<UploadFile> Files => _files.AsReadOnly();

        public async Task AddFiles(string[] filePaths)
        {
            Debug.WriteLine($"### FileUploadService.AddFiles ###");
            Debug.WriteLine($"Próba dodania {filePaths.Length} plików:");

            try
            {
                Debug.WriteLine($"Ścieżki plików do dodania:");
                foreach (var path in filePaths)
                {
                    Debug.WriteLine($"- {path}");
                }

                // Sprawdzamy duplikaty
                var existingPaths = _files.Select(f => f.FilePath).ToHashSet();
                Debug.WriteLine($"Istniejące pliki: {existingPaths.Count}");

                var newFiles = filePaths.Where(path => !existingPaths.Contains(path));
                Debug.WriteLine($"Nowe pliki do dodania: {newFiles.Count()}");

                foreach (var path in newFiles)
                {
                    try
                    {
                        Debug.WriteLine($"Próba dodania pliku: {path}");
                        var fileInfo = new FileInfo(path);
                        Debug.WriteLine($"FileInfo utworzony dla: {fileInfo.Name}");

                        _files.Add(new UploadFile
                        {
                            FilePath = path,
                            FileName = fileInfo.Name,
                            FileSize = fileInfo.Length,
                            AddedDate = DateTime.Now,
                            Status = UploadStatus.Pending,
                            SortOrder = _currentSortOrder++
                        });
                        Debug.WriteLine($"Plik dodany pomyślnie: {path}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"BŁĄD podczas dodawania pojedynczego pliku {path}: {ex}");
                        throw;
                    }
                }

                Debug.WriteLine($"Aktualna liczba plików w serwisie: {_files.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BŁĄD GŁÓWNY podczas AddFiles: {ex}");
                throw;
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

        public async Task UploadFiles(IEnumerable<UploadFile> files, IProgress<(int fileIndex, int totalFiles, int progress)> progress = null)
        {
            var filesToUpload = files.ToList();
            var errors = new List<(UploadFile File, Exception Error)>();

            for (int i = 0; i < filesToUpload.Count; i++)
            {
                var file = filesToUpload[i];
                try
                {
                    if (!file.KoszulkaId.HasValue)
                    {
                        throw new ArgumentException($"Nie wybrano koszulki dla pliku {file.FileName}");
                    }

                    file.Status = UploadStatus.Uploading;
                    progress?.Report((i + 1, filesToUpload.Count, 0));

                    byte[] content = await File.ReadAllBytesAsync(file.FilePath);
                    progress?.Report((i + 1, filesToUpload.Count, 30));

                    var idZalacznika = await _ezdService.DodajZalacznik(
                        content,
                        file.FileName,
                        _ezdService.CurrentUserId.Value
                    );
                    progress?.Report((i + 1, filesToUpload.Count, 60));

                    var dokument = await _ezdService.RejestrujDokument(
                        file.FileName,
                        file.KoszulkaId.Value,
                        idZalacznika,
                        _ezdService.CurrentUserId.Value,
                        file.BrakDaty,
                        file.BrakZnaku
                    );

                    if (!string.IsNullOrEmpty(file.DocumentType) || (!file.BrakDaty && file.AddedDate != default))
                    {
                        // Modyfikujemy dokument używając bezpośrednio jego właściwości
                        dokument.Rodzaj = file.DocumentType;
                        if (!file.BrakDaty)
                        {
                            dokument.DataDokumentu = file.AddedDate.ToString("yyyy-MM-dd");
                        }
                        if (!file.BrakZnaku)
                        {
                            dokument.Sygnatura = file.NumerPisma;
                        }

                        await _ezdService.AktualizujMetadaneDokumentu(dokument);
                    }

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
            _currentSortOrder = 0;
        }
    }
}
