using Microsoft.Extensions.DependencyInjection;
using EZDUploader.Core.Configuration;
using EZDUploader.Core.Interfaces;
using EZDUploader.Infrastructure.Services;
using System.Windows.Forms;
using EZDUploader.UI.Forms;
using EZDUploader.Core.Models;
using EZDUploader.Core.Validators;
using System.IO.Pipes;
using System.Diagnostics;

namespace EZDUploader.UI;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Debug.WriteLine($"### START APLIKACJI ###");
        Debug.WriteLine($"Argumenty: {string.Join(", ", args ?? new string[0])}");

        var validArgs = args?.Where(arg => !string.IsNullOrEmpty(arg)).ToArray() ?? Array.Empty<string>();
        Debug.WriteLine($"Poprawne argumenty po filtracji: {string.Join(", ", validArgs)}");

        var mutexName = "Global\\EZDUploaderInstance";
        using (var mutex = new Mutex(false, mutexName, out bool createdNew))
        {
            Debug.WriteLine($"Mutex createdNew: {createdNew}");

            if (!createdNew)
            {
                Debug.WriteLine("Wykryto istniej�c� instancj�, pr�ba przekazania przez pipe...");
                try
                {
                    using (var pipe = new NamedPipeClientStream(".", "EZDUploaderPipe", PipeDirection.Out))
                    {
                        Debug.WriteLine("Pr�ba po��czenia z pipe...");
                        pipe.Connect(1000);
                        Debug.WriteLine("Po��czono z pipe!");

                        using (var writer = new StreamWriter(pipe))
                        {
                            foreach (var arg in validArgs)
                            {
                                Debug.WriteLine($"Wysy�am do pipe: {arg}");
                                writer.WriteLine(arg);
                            }
                            writer.Flush();
                            Debug.WriteLine("Zako�czono wysy�anie do pipe");
                        }
                    }
                    return;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"B��D przy pr�bie przekazania przez pipe: {ex}");
                    StartNormalApplication(validArgs);
                }
            }
            else
            {
                Debug.WriteLine("Uruchamiam jako nowa instancja");
                StartNormalApplication(validArgs);
            }
        }
    }

    static void StartNormalApplication(string[] args)
    {
        try
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

            // Uruchom serwer pipe w osobnym w�tku
            var pipeServerThread = new Thread(() => RunPipeServer(mainForm, fileUploadService));
            pipeServerThread.SetApartmentState(ApartmentState.STA);
            pipeServerThread.IsBackground = true;
            pipeServerThread.Start();

            // Asynchronicznie dodaj pliki je�li s�
            if (args.Length > 0)
            {
                mainForm.HandleCreated += async (s, e) =>
                {
                    Debug.WriteLine($"Dodawanie plik�w: {string.Join(", ", args)}");
                    await fileUploadService.AddFiles(args);
                    mainForm.RefreshFilesList();
                };
            }

            Application.Run(mainForm);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"B��d podczas startu aplikacji: {ex}");
            MessageBox.Show($"Wyst�pi� b��d podczas uruchamiania aplikacji: {ex.Message}",
                "B��d krytyczny", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    static void RunPipeServer(MainForm form, IFileUploadService fileUploadService)
    {
        Debug.WriteLine("### URUCHOMIONO PIPE SERVER ###");

        while (true)
        {
            try
            {
                Debug.WriteLine("Pipe server czeka na po��czenie...");
                using (var pipe = new NamedPipeServerStream("EZDUploaderPipe", PipeDirection.In))
                {
                    pipe.WaitForConnection();
                    Debug.WriteLine("Pipe server: po��czono!");

                    var files = new List<string>();
                    using (var reader = new StreamReader(pipe))
                    {
                        Debug.WriteLine("Pipe server: czytam dane...");
                        string fileName;
                        while ((fileName = reader.ReadLine()) != null)
                        {
                            Debug.WriteLine($"Pipe server odczyta�: {fileName}");
                            if (!string.IsNullOrWhiteSpace(fileName))
                            {
                                files.Add(fileName);
                            }
                        }
                    }

                    Debug.WriteLine($"Pipe server odczyta� {files.Count} plik�w");

                    if (files.Any())
                    {
                        Debug.WriteLine("Pipe server: pr�ba dodania plik�w...");
                        form.Invoke(() =>
                        {
                            Debug.WriteLine("Pipe server: wywo�uj� AddFiles...");
                            // Uruchom asynchroniczn� operacj� i poczekaj na jej zako�czenie
                            form.BeginInvoke(async () =>
                            {
                                try
                                {
                                    await fileUploadService.AddFiles(files.ToArray());
                                    form.RefreshFilesList();
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"B��D podczas dodawania plik�w w pipe server: {ex}");
                                    MessageBox.Show($"B��d podczas dodawania plik�w: {ex.Message}",
                                        "B��d", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            });
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"B��D w pipe server: {ex}");
                Thread.Sleep(100); // Kr�tkie op�nienie przed kolejn� pr�b�
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