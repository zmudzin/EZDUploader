using Microsoft.Extensions.DependencyInjection;
using EZDUploader.Core.Configuration;
using EZDUploader.Core.Interfaces;
using EZDUploader.Infrastructure.Services;
using System.Windows.Forms;
using EZDUploader.UI.Forms;
using EZDUploader.Core.Models;
using EZDUploader.Core.Validators;

namespace EZDUploader.UI;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var services = new ServiceCollection();
        ConfigureServices(services);

        using var serviceProvider = services.BuildServiceProvider();

        try
        {
            var ezdService = serviceProvider.GetRequiredService<IEzdApiService>();
            var fileUploadService = serviceProvider.GetRequiredService<IFileUploadService>();

            if (args?.Length > 0)
            {
                var fileValidator = serviceProvider.GetRequiredService<IFileValidator>();
                var uploadDialog = new Forms.UploadDialog(
                    ezdService,
                    fileUploadService,
                    serviceProvider.GetRequiredService<IFileValidator>(),
                    FileExtensions.ToUploadFiles(args)
                );
                Application.Run(uploadDialog);
            }
            else
            {
                var mainForm = serviceProvider.GetRequiredService<MainForm>();
                Application.Run(mainForm);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Wyst¹pi³ b³¹d podczas uruchamiania aplikacji: {ex.Message}",
                "B³¹d krytyczny", MessageBoxButtons.OK, MessageBoxIcon.Error);
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