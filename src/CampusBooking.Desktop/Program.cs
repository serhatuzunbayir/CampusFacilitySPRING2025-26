using CampusBooking.Desktop.Config;
using CampusBooking.Desktop.Services;
using CampusBooking.Desktop.UI.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CampusBooking.Desktop;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
            MessageBox.Show(e.Exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => c.AddJsonFile("appsettings.json", optional: false))
            .ConfigureServices((ctx, services) =>
            {
                services.Configure<ApiOptions>(ctx.Configuration.GetSection("Api"));

                services.AddSingleton<UserSession>();
                services.AddSingleton<NotificationPoller>();

                services.AddHttpClient<ApiClient>((sp, http) =>
                {
                    var opts = sp.GetRequiredService<IOptions<ApiOptions>>().Value;
                    http.BaseAddress = new Uri(opts.BaseUrl);
                })
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                });

                services.AddTransient<LoginForm>();
                services.AddTransient<MainForm>();
                services.AddTransient<AddFacilityForm>();
                services.AddTransient<CreateBookingForm>();
            })
            .Build();

        var login = host.Services.GetRequiredService<LoginForm>();
        Application.Run(login);
    }
}
