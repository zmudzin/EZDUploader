using Microsoft.Extensions.DependencyInjection;
using EZDUploader.Core.Configuration;
using EZDUploader.Core.Interfaces;
using EZDUploader.Infrastructure.Services;
using EZDUploader.Core.Validators;
using System.IO.Pipes;
using System.Diagnostics;

namespace EZDUploader.UI;

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
            mainForm.HandleCreated += async (s, e) =>
            {
                await fileUploadService.AddFiles(args);
                mainForm.RefreshFilesList();
            };
        }

        Application.Run(mainForm);
    }

    static void SendFilesToExistingInstance(string[] args)
    {
        try
        {
            using (var clientChannel = new NamedPipeClientStream(".", "EZDUploaderIPCChannel", PipeDirection.Out))
            {
                clientChannel.Connect(5000); // Zwiększamy timeout do 5 sekund

                using (var writer = new StreamWriter(clientChannel))
                {
                    // Wyślij liczbę plików jako pierwszą informację
                    writer.WriteLine("FILES_TRANSFER");
                    writer.WriteLine(args.Length.ToString());
                    writer.Flush(); // Ważne - flush po każdej ważnej operacji
                    
                    // Teraz wysyłamy każdy plik oddzielnie z dodatkowym flush
                    foreach (var file in args)
                    {
                        writer.WriteLine(file);
                        writer.Flush();
                    }
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
                                        await fileUploadService.AddFiles(filesArray);
                                        mainForm.RefreshFilesList();
                                        Debug.WriteLine("IPC: Pliki zostały dodane i lista odświeżona");
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