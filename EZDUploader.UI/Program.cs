using Microsoft.Extensions.DependencyInjection;
using EZDUploader.Core.Configuration;
using EZDUploader.Core.Interfaces;
using EZDUploader.Infrastructure.Services;
using EZDUploader.Core.Validators;
using System.IO.Pipes;
using System.Diagnostics;
using System.Text;

namespace EZDUploader.UI;

static class FileUploadConstants
{
    public const int MAX_FILES_LIMIT = 100;
}

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // Unikalna nazwa mutex dla całej aplikacji
        const string MutexName = "Global\\EZDUploaderSingleInstanceMutex";

        // Flaga określająca, czy jesteśmy pierwszą instancją
        bool createdNew;
        using (var mutex = new Mutex(true, MutexName, out createdNew))
        {
            if (createdNew)
            {
                // Pierwsza instancja
                RunFirstInstance(args);
            }
            else
            {
                // Próba komunikacji z istniejącą instancją
                SendFilesToExistingInstance(args);
            }
        }
    }

    static void RunFirstInstance(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var services = new ServiceCollection();
        ConfigureServices(services);

        using var serviceProvider = services.BuildServiceProvider();
        var ezdService = serviceProvider.GetRequiredService<IEzdApiService>();
        var fileUploadService = serviceProvider.GetRequiredService<IFileUploadService>();
        var mainForm = serviceProvider.GetRequiredService<MainForm>();

        // Uruchom serwer IPC w osobnym wątku
        var ipcServerThread = new Thread(() => RunIPCServer(mainForm, fileUploadService));
        ipcServerThread.SetApartmentState(ApartmentState.STA);
        ipcServerThread.IsBackground = true;
        ipcServerThread.Start();

        // Dodaj pliki przekazane podczas uruchomienia
        if (args.Length > 0)
        {
            // Sprawdź limit plików - pokaż ostrzeżenie jeśli przekroczono limit
            if (args.Length > FileUploadConstants.MAX_FILES_LIMIT)
            {
                var message = $"Przekroczono maksymalną liczbę plików. Limit wynosi {FileUploadConstants.MAX_FILES_LIMIT} plików.\n" +
                          $"Wybrano {args.Length} plików, zostanie przesłanych pierwszych {FileUploadConstants.MAX_FILES_LIMIT}.";
                
                MessageBox.Show(message, "Przekroczono limit plików", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                
                // Przytnij tablicę do maksymalnego limitu
                args = args.Take(FileUploadConstants.MAX_FILES_LIMIT).ToArray();
            }

            mainForm.HandleCreated += async (s, e) =>
            {
                try
                {
                    await fileUploadService.AddFiles(args);
                    mainForm.RefreshFilesList();
                }
                catch (AggregateException aex)
                {
                    // Pokaż czytelny komunikat o nieprzesłanych plikach
                    var sb = new StringBuilder();
                    sb.AppendLine("Nie wszystkie pliki zostały dodane do listy:");
                    sb.AppendLine();
                    
                    foreach (var ex in aex.InnerExceptions)
                    {
                        sb.AppendLine($"- {ex.Message}");
                    }
                    
                    MessageBox.Show(sb.ToString(), "Błąd dodawania plików", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    mainForm.RefreshFilesList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd podczas dodawania plików: {ex.Message}", 
                        "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    mainForm.RefreshFilesList();
                }
            };
        }

        Application.Run(mainForm);
    }

    static void SendFilesToExistingInstance(string[] args)
    {
        try
        {
            // Sprawdź limit plików - pokaż ostrzeżenie jeśli przekroczono limit
            string[] filesToSend = args;
            if (args.Length > FileUploadConstants.MAX_FILES_LIMIT)
            {
                var message = $"Przekroczono maksymalną liczbę plików. Limit wynosi {FileUploadConstants.MAX_FILES_LIMIT} plików.\n" +
                          $"Wybrano {args.Length} plików, zostanie przesłanych pierwszych {FileUploadConstants.MAX_FILES_LIMIT}.";

                MessageBox.Show(message, "Przekroczono limit plików", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                // Przytnij tablicę do maksymalnego limitu - używamy ToArray() aby utworzyć nową tablicę
                filesToSend = args.Take(FileUploadConstants.MAX_FILES_LIMIT).ToArray();
                Debug.WriteLine($"IPC: Przycięto listę plików z {args.Length} do {filesToSend.Length}");
            }

            using (var clientChannel = new NamedPipeClientStream(".", "EZDUploaderIPCChannel", PipeDirection.Out))
            {
                clientChannel.Connect(10000); // Zwiększamy timeout do 10 sekund

                using (var writer = new StreamWriter(clientChannel))
                {
                    // Wyślij liczbę plików jako pierwszą informację - używamy filesToSend.Length, nie args.Length
                    writer.WriteLine("FILES_TRANSFER");
                    writer.WriteLine(filesToSend.Length.ToString());
                    writer.Flush(); // Ważne - flush po liczbie plików

                    Debug.WriteLine($"IPC: Wysyłanie {filesToSend.Length} plików");

                    // Teraz wysyłamy pliki z przyciętej tablicy
                    foreach (var file in filesToSend)
                    {
                        writer.WriteLine(file);
                    }
                    // Pojedynczy flush po wszystkich plikach
                    writer.Flush();
                    writer.WriteLine("FILES_END");
                    writer.Flush();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Nie można przesłać plików do istniejącej instancji: {ex.Message}",
                "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    static void RunIPCServer(MainForm mainForm, IFileUploadService fileUploadService)
    {
        while (true)
        {
            try
            {
                using (var serverChannel = new NamedPipeServerStream("EZDUploaderIPCChannel",
                    PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                {
                    serverChannel.WaitForConnection();
                    Debug.WriteLine("IPC: Połączenie klienta nawiązane");

                    using (var reader = new StreamReader(serverChannel))
                    {
                        var header = reader.ReadLine();
                        Debug.WriteLine($"IPC: Otrzymano nagłówek: {header}");
                        
                        if (header != "FILES_TRANSFER")
                            continue;

                        var filesToAdd = new List<string>();
                        
                        // Odczytujemy liczbę plików, które mają być przesłane
                        var filesCountStr = reader.ReadLine();
                        Debug.WriteLine($"IPC: Liczba plików do odebrania: {filesCountStr}");
                        
                        if (!int.TryParse(filesCountStr, out int expectedFilesCount))
                        {
                            Debug.WriteLine("IPC: Nieprawidłowy format liczby plików");
                            continue;
                        }
                        
                        // Przygotowujemy listę o odpowiednim rozmiarze
                        filesToAdd = new List<string>(expectedFilesCount);
                        
                        // Odczytujemy pliki do osiągnięcia końca lub oczekiwanej liczby plików
                        string line;
                        int readFilesCount = 0;
                        
                        while ((line = reader.ReadLine()) != null && line != "FILES_END" && readFilesCount < expectedFilesCount * 2) 
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                filesToAdd.Add(line);
                                Debug.WriteLine($"IPC: Dodano plik: {line}");
                                readFilesCount++;
                            }
                        }
                        
                        Debug.WriteLine($"IPC: Odczytano {filesToAdd.Count} plików z {expectedFilesCount} oczekiwanych");

                        if (filesToAdd.Any())
                        {
                            var filesArray = filesToAdd.ToArray();
                            Debug.WriteLine($"IPC: Przekazywanie {filesArray.Length} plików do serwisu");
                            
                            mainForm.Invoke(() =>
                            {
                                mainForm.BeginInvoke(async () =>
                                {
                                    try
                                    {
                                        // Sprawdź limit plików - pokaż ostrzeżenie jeśli przekroczono limit
                                        if (filesArray.Length > FileUploadConstants.MAX_FILES_LIMIT)
                                        {
                                            var message = $"Przekroczono maksymalną liczbę plików. Limit wynosi {FileUploadConstants.MAX_FILES_LIMIT} plików.\n" +
                                                      $"Wybrano {filesArray.Length} plików, zostanie przesłanych pierwszych {FileUploadConstants.MAX_FILES_LIMIT}.";
                                            
                                            MessageBox.Show(message, "Przekroczono limit plików", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                            
                                            // Przytnij tablicę do maksymalnego limitu
                                            filesArray = filesArray.Take(FileUploadConstants.MAX_FILES_LIMIT).ToArray();
                                        }

                                        await fileUploadService.AddFiles(filesArray);
                                        mainForm.RefreshFilesList();
                                        Debug.WriteLine("IPC: Pliki zostały dodane i lista odświeżona");
                                    }
                                    catch (AggregateException aex)
                                    {
                                        // Pokaż czytelny komunikat o nieprzesłanych plikach
                                        var sb = new StringBuilder();
                                        sb.AppendLine("Nie wszystkie pliki zostały dodane do listy:");
                                        sb.AppendLine();
                                        
                                        foreach (var ex in aex.InnerExceptions)
                                        {
                                            sb.AppendLine($"- {ex.Message}");
                                        }
                                        
                                        Debug.WriteLine($"IPC: Błąd podczas dodawania plików: {aex.Message}");
                                        MessageBox.Show(sb.ToString(), "Błąd dodawania plików", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        mainForm.RefreshFilesList();
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"IPC: Błąd podczas dodawania plików: {ex.Message}");
                                        MessageBox.Show($"Błąd podczas dodawania plików: {ex.Message}",
                                            "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                });
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log błędu, ale nie zatrzymuj serwera
                Debug.WriteLine($"Błąd w IPC Server: {ex.Message}");
                Thread.Sleep(100);
            }
        }
    }


    private static void ConfigureServices(ServiceCollection services)
    {
        // Konfiguracja - najpierw rejestrujemy ustawienia!
        var settings = ConfigurationManager.LoadSettings() ?? new ApiSettings();
        services.AddSingleton(settings);

        // Serwisy
        services.AddSingleton<IEzdApiService, EzdApiService>();
        services.AddSingleton<IFileUploadService, FileUploadService>();
        services.AddSingleton<IFileValidator, FileValidator>();

        // Forms
        services.AddTransient<MainForm>();
        services.AddTransient<SettingsForm>();
    }
}