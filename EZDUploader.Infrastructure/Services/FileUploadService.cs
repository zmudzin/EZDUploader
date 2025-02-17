﻿using EZDUploader.Core.Interfaces;
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

            // Najpierw sprawdźmy które pliki są nowe
            var existingPaths = _files.Select(f => f.FilePath).ToHashSet();
            var newFiles = filePaths.Where(path => !existingPaths.Contains(path)).ToList();

            Debug.WriteLine($"Znaleziono {newFiles.Count} nowych plików do dodania");

            foreach (var path in newFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(path);
                    Debug.WriteLine($"Dodaję plik: {fileInfo.Name}, rozmiar: {fileInfo.Length}");

                    _files.Add(new UploadFile
                    {
                        FilePath = path,
                        FileName = fileInfo.Name,
                        FileSize = fileInfo.Length,
                        AddedDate = DateTime.Now,
                        Status = UploadStatus.Pending
                    });
                    Debug.WriteLine($"Plik dodany pomyślnie: {path}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"BŁĄD podczas dodawania pliku {path}: {ex}");
                }
            }

            Debug.WriteLine($"Aktualna liczba plików w serwisie: {_files.Count}");
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
            foreach (var file in filesToUpload)
            {
                var validationError = _fileValidator.GetFileValidationError(file.FileName);
                if (validationError != null)
                {
                    throw new ArgumentException($"Błąd walidacji pliku {file.FileName}: {validationError}");
                }
            }
            var errors = new List<(UploadFile File, Exception Error)>();

            for (int i = 0; i < filesToUpload.Count; i++)
            {
                var file = filesToUpload[i];
                try
                {
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
                        idKoszulki,
                        idZalacznika,
                        _ezdService.CurrentUserId.Value
                    );

                    if (!string.IsNullOrEmpty(file.DocumentType) || file.AddedDate != default)
                    {
                        dokument.Rodzaj = file.DocumentType;
                        dokument.DataDokumentu = file.AddedDate.ToString("yyyy-MM-dd");
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
        }
    }
}
