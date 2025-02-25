using EZDUploader.Core.Interfaces;
using EZDUploader.Core.Models;
using EZDUploader.Core.Interfaces;
using EZDUploader.Core.Models;
using EZDUploader.Core.Validators;
using System.Diagnostics;
using System.ComponentModel.DataAnnotations;

namespace EZDUploader.Infrastructure.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IEzdApiService _ezdService;
        private readonly IFileValidator _fileValidator;
        private readonly List<UploadFile> _files = new();
        private int _currentSortOrder = 0;
        private bool _cancelRequested;


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

            if (filePaths == null || filePaths.Length == 0)
            {
                Debug.WriteLine("Brak plików do dodania");
                return;
            }

            try
            {
                // Zapamiętujemy początkowy stan listy plików
                var existingPaths = _files.Select(f => f.FilePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
                Debug.WriteLine($"Istniejące pliki przed dodaniem nowych: {existingPaths.Count}");

                // Filtrujemy tylko nowe pliki - bardzo ważne dla poprawnego działania
                var newFiles = filePaths
                    .Where(path => !string.IsNullOrEmpty(path) && !existingPaths.Contains(path))
                    .ToList();
                
                Debug.WriteLine($"Nowe pliki do dodania (po filtrowaniu duplikatów): {newFiles.Count}");
                
                // Rejestrujemy wszystkie ścieżki
                foreach (var path in filePaths)
                {
                    Debug.WriteLine($"Ścieżka do pliku: '{path}'");
                }

                if (newFiles.Count == 0)
                {
                    Debug.WriteLine("Nie znaleziono nowych plików do dodania po filtrowaniu");
                    return;
                }

                const int BATCH_SIZE = 10; // Zmniejszamy rozmiar partii dla lepszej kontroli
                var failedFiles = new List<string>();
                var addedFiles = new List<string>();

                for (int i = 0; i < newFiles.Count; i += BATCH_SIZE)
                {
                    var batch = newFiles.Skip(i).Take(BATCH_SIZE).ToList();
                    Debug.WriteLine($"Przetwarzanie partii {i/BATCH_SIZE + 1}, rozmiar: {batch.Count} plików");

                    foreach (var path in batch)
                    {
                        try
                        {
                            Debug.WriteLine($"Próba dodania pliku: {path}");
                            var fileInfo = new FileInfo(path);

                            // Dodatkowe sprawdzenie istnienia pliku
                            if (!fileInfo.Exists)
                            {
                                Debug.WriteLine($"Plik nie istnieje: {path}");
                                failedFiles.Add(path);
                                continue;
                            }

                            // Dodatkowe ograniczenie rozmiaru pliku (np. 100 MB)
                            if (fileInfo.Length > 100 * 1024 * 1024)
                            {
                                Debug.WriteLine($"Plik za duży: {path}");
                                failedFiles.Add(path);
                                continue;
                            }

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

                            addedFiles.Add(path);
                            Debug.WriteLine($"Plik dodany pomyślnie: {path}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"BŁĄD podczas dodawania pojedynczego pliku {path}: {ex}");
                            failedFiles.Add(path);
                        }
                    }

                    // Pauzujemy po każdej partii by dać szansę UI na zaktualizowanie
                    await Task.Delay(50); // Krótka pauza między partiami
                }

                Debug.WriteLine($"Po dodaniu wszystkich plików, łącznie w serwisie: {_files.Count}");


                Debug.WriteLine($"Aktualna liczba plików w serwisie: {_files.Count}");

                // Raportowanie nieudanych plików
                if (failedFiles.Any())
                {
                    Debug.WriteLine($"Nieudane pliki ({failedFiles.Count}):");
                    foreach (var failedFile in failedFiles)
                    {
                        Debug.WriteLine($"- {failedFile}");
                    }

                    // Dodaj bardziej szczegółowy mechanizm raportowania błędów
                    throw new AggregateException("Niektóre pliki nie mogły zostać dodane",
                        failedFiles.Select(f => new Exception($"Nie można dodać pliku: {f}")));
                }

                // Log dodanych plików
                Debug.WriteLine($"Dodane pliki ({addedFiles.Count}):");
                foreach (var addedFile in addedFiles)
                {
                    Debug.WriteLine($"- {addedFile}");
                }
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
            _cancelRequested = false;
            const int ROZMIAR_PACZKI = 20;
            var plikiDoWyslania = files.ToList();
            Debug.WriteLine($"Rozpoczęcie wysyłki {plikiDoWyslania.Count} plików");
            Debug.WriteLine($"Status plików przed wysyłką:");
            foreach (var plik in plikiDoWyslania)
            {
                Debug.WriteLine($"- {plik.FileName}: {plik.Status}");
            }
            var bledy = new List<(UploadFile Plik, Exception Blad)>();

            // Pełna walidacja przed rozpoczęciem wysyłki
            foreach (var plik in plikiDoWyslania)
            {
                if (string.IsNullOrWhiteSpace(plik.DocumentType))
                {
                    bledy.Add((plik, new ValidationException("Wymagane jest wybranie rodzaju dokumentu")));
                }
                if (!plik.KoszulkaId.HasValue && string.IsNullOrEmpty(plik.NowaKoszulkaNazwa))
                {
                    bledy.Add((plik, new ValidationException("Wymagane jest wybranie koszulki lub podanie nazwy nowej koszulki")));
                }
                if (!_fileValidator.ValidateFileName(plik.FileName))
                {
                    bledy.Add((plik, new ValidationException(_fileValidator.GetFileValidationError(plik.FileName))));
                }
            }

            // Jeśli są błędy, przerwij wysyłkę
            if (bledy.Any())
            {
                var errorsByFile = bledy
                    .GroupBy(b => b.Plik.FileName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(b => b.Blad.Message).ToList()
                    );

                var errorMessage = string.Join("\n", errorsByFile.Select(kvp =>
                    $"- {kvp.Key}:\n  " + string.Join("\n  ", kvp.Value)));

                foreach (var error in bledy)
                {
                    error.Plik.Status = UploadStatus.Failed;
                    error.Plik.ErrorMessage = string.Join("; ",
                        errorsByFile[error.Plik.FileName]);
                }

                throw new AggregateException($"Błędy walidacji:\n{errorMessage}",
                    bledy.Select(e => e.Blad));
            }

            try
            {
                // KROK 1: Walidacja przed wysyłką
                var plikiZBledami = plikiDoWyslania
                    .Where(p => string.IsNullOrWhiteSpace(p.DocumentType))
                    .ToList();

                if (plikiZBledami.Any())
                {
                    foreach (var plik in plikiZBledami)
                    {
                        plik.Status = UploadStatus.Failed;
                        plik.ErrorMessage = "Wymagane jest wybranie rodzaju dokumentu";
                        bledy.Add((plik, new ValidationException("Brak wybranego rodzaju dokumentu")));
                    }
                    var errorMessage = string.Join("\n", plikiZBledami.Select(p => $"- {p.FileName}: Wymagane jest wybranie rodzaju dokumentu"));
                    throw new AggregateException($"Błędy walidacji:\n{errorMessage}",
                        bledy.Select(e => e.Blad));
                }

                // KROK 2: Tworzenie koszulek (bez zmian)
                var grupowanePliki = plikiDoWyslania
                    .Where(f => !string.IsNullOrEmpty(f.NowaKoszulkaNazwa))
                    .GroupBy(f => f.NowaKoszulkaNazwa);

                foreach (var grupa in grupowanePliki)
                {
                    try
                    {
                        Debug.WriteLine($"Tworzenie nowej koszulki: {grupa.Key}");
                        var nowaKoszulka = await _ezdService.UtworzKoszulke(
                            grupa.Key,
                            _ezdService.CurrentUserId.Value
                        );

                        foreach (var plik in grupa)
                        {
                            plik.KoszulkaId = nowaKoszulka.ID;
                            plik.NowaKoszulkaNazwa = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        foreach (var plik in grupa)
                        {
                            plik.Status = UploadStatus.Failed;
                            plik.ErrorMessage = $"Błąd podczas tworzenia koszulki: {ex.Message}";
                            bledy.Add((plik, ex));
                        }
                    }
                }

                // KROK 3: Wysyłanie plików w paczkach (istniejąca logika)
                for (int i = 0; i < plikiDoWyslania.Count; i += ROZMIAR_PACZKI)
                {
                    if (_cancelRequested)
                    {
                        Debug.WriteLine("Anulowanie wysyłki - nie będą wysyłane kolejne paczki");
                        break;
                    }

                    var paczka = plikiDoWyslania
                        .Skip(i)
                        .Take(ROZMIAR_PACZKI)
                        .Where(f => f.Status != UploadStatus.Failed)
                        .ToList();

                    foreach (var plik in paczka)
                    {
                        try
                        {
                            if (_cancelRequested && plik.Status != UploadStatus.Uploading)
                            {
                                Debug.WriteLine($"Pomijanie pliku {plik.FileName} z powodu anulowania");
                                continue;
                            }

                            if (!plik.KoszulkaId.HasValue)
                            {
                                throw new ArgumentException($"Nie wybrano koszulki dla pliku {plik.FileName}");
                            }

                            plik.Status = UploadStatus.Uploading;
                            progress?.Report((i + paczka.IndexOf(plik) + 1, plikiDoWyslania.Count, 0));

                            byte[] zawartosc = await File.ReadAllBytesAsync(plik.FilePath);
                            progress?.Report((i + paczka.IndexOf(plik) + 1, plikiDoWyslania.Count, 30));

                            var idZalacznika = await _ezdService.DodajZalacznik(
                                zawartosc,
                                plik.FileName,
                                _ezdService.CurrentUserId.Value
                            );
                            progress?.Report((i + paczka.IndexOf(plik) + 1, plikiDoWyslania.Count, 60));

                            var dokument = await _ezdService.RejestrujDokument(
                                plik.FileName,
                                plik.KoszulkaId.Value,
                                idZalacznika,
                                _ezdService.CurrentUserId.Value,
                                plik.BrakDaty,
                                plik.BrakZnaku
                            );

                            if (!string.IsNullOrEmpty(plik.DocumentType) || (!plik.BrakDaty && plik.AddedDate != default))
                            {
                                dokument.Rodzaj = plik.DocumentType;
                                if (!plik.BrakDaty)
                                {
                                    dokument.DataDokumentu = plik.AddedDate.ToString("yyyy-MM-dd");
                                }
                                if (!plik.BrakZnaku)
                                {
                                    dokument.Sygnatura = plik.NumerPisma;
                                }

                                await _ezdService.AktualizujMetadaneDokumentu(dokument);
                            }

                            plik.Status = UploadStatus.Completed;
                            progress?.Report((i + paczka.IndexOf(plik) + 1, plikiDoWyslania.Count, 100));
                        }
                        catch (Exception ex)
                        {
                            plik.Status = UploadStatus.Failed;
                            plik.ErrorMessage = ex.Message;
                            bledy.Add((plik, ex));
                        }
                    }

                    if (i + ROZMIAR_PACZKI < plikiDoWyslania.Count)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2));
                    }

                    await Task.Delay(100);
                }

                if (bledy.Any())
                {
                    var errorMessage = string.Join("\n", bledy.Select(e => $"- {e.Plik.FileName}: {e.Blad.Message}"));
                    throw new AggregateException($"Błędy podczas wysyłania plików:\n{errorMessage}", bledy.Select(e => e.Blad));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd główny podczas uploadu: {ex}");
                throw;
            }
        }

        public void CancelUpload()
        {
            _cancelRequested = true;
            Debug.WriteLine("Zgłoszono żądanie anulowania uploadu");
        }

        public void ClearFiles()
        {
            _files.Clear();
            _currentSortOrder = 0;
        }
    }
}
