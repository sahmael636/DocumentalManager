using DocumentalManager.Services;
using DocumentalManager.ViewModels;
using DocumentalManager.Views;
using Microsoft.Extensions.Logging;

namespace DocumentalManager
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register Services
            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<ExcelService>();

            // Register ViewModels
            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddSingleton<TablasViewModel>();
            builder.Services.AddTransient<ListaViewModel>();
            builder.Services.AddTransient<FormularioViewModel>();
            builder.Services.AddSingleton<ConsultaViewModel>();

            // Register Views
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<TablasPage>();
            builder.Services.AddTransient<ListaPage>();
            builder.Services.AddTransient<FormularioPage>();
            builder.Services.AddSingleton<ConsultaPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
