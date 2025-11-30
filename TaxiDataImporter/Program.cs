using Infrastructure.Repositories;
using Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using TaxiDataImporter.Services;
using TaxiDataImporter.Services.Interfaces;

namespace TaxiDataImporter;

public class Program
{
    private static async Task Main(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        IConfiguration configuration = builder.Build();

        var services = new ServiceCollection();

        services.AddSingleton(configuration);
        services.AddTransient<ITripRepository, TripRepository>();
        services.AddTransient<IImportService, ImportService>();

        var serviceProvider = services.BuildServiceProvider();

        Console.WriteLine("Initializing ETL process...");

        try
        {
            var service = serviceProvider.GetRequiredService<IImportService>();
            Console.WriteLine("Timer started.");

            var stopwatch = Stopwatch.StartNew();
            await service.Process();
            stopwatch.Stop();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n[BENCHMARK] Time elapsed: {stopwatch.Elapsed.TotalSeconds:N2} sec");
            Console.WriteLine($"[BENCHMARK] Exact time:   {stopwatch.Elapsed}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Critical Error: {ex.Message}");
            Console.ResetColor();
        }
        finally
        {
            if (serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}