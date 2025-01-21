using Microsoft.Extensions.DependencyInjection;
using EZDUploader.Core.Configuration;
using EZDUploader.Core.Interfaces;
using EZDUploader.Infrastructure.Services;

namespace EZDUploader.UI;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        RunAsync().GetAwaiter().GetResult();
    }

    private static async Task RunAsync()
    {
        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);

            using var serviceProvider = services.BuildServiceProvider();
            var ezdService = serviceProvider.GetRequiredService<IEzdApiService>();

            // SprawdŸ ustawienia i spróbuj zalogowaæ
            while (!ezdService.IsAuthenticated)
            {
                using var settingsForm = new SettingsForm(ezdService.Settings);
                if (settingsForm.ShowDialog() != DialogResult.OK)
                {
                    return; // U¿ytkownik anulowa³
                }

                // Próba automatycznego logowania po zapisaniu ustawieñ
                if (!await TryAuthenticate(ezdService))
                {
                    MessageBox.Show("Nie uda³o siê zalogowaæ. SprawdŸ ustawienia.",
                        "B³¹d logowania", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }
            }

            // Jeœli logowanie siê powiod³o, pokazujemy g³ówne okno
            Application.Run(serviceProvider.GetRequiredService<MainForm>());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Wyst¹pi³ b³¹d: {ex.Message}", "B³¹d",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static async Task<bool> TryAuthenticate(IEzdApiService ezdService)
    {
        try
        {
            if (ezdService.Settings.AuthType == AuthenticationType.Token)
            {
                ezdService.SetupTokenAuth(ezdService.Settings.ApplicationToken);
                return ezdService.IsAuthenticated;
            }
            else
            {
                return await ezdService.LoginAsync(
                    ezdService.Settings.Login,
                    ezdService.Settings.Password);
            }
        }
        catch
        {
            return false;
        }
    }

    private static void ConfigureServices(ServiceCollection services)
    {
        var settings = ConfigurationManager.LoadSettings();
        services.AddSingleton(settings);
        services.AddScoped<IEzdApiService, EzdApiService>();
        services.AddTransient<MainForm>();
    }
}