using InterfazHumana.IO.Data;
using InterfazHumana.IO.Models;

namespace InterfazHumana.IO;

internal static class Program
{
    private const string ConnectionString = "Data Source=interfazhumana.db;Cache=Shared;";

    private static void Main()
    {
        DatabaseInitializer.Initialize(ConnectionString);

        var dbContext = new DatabaseContext(ConnectionString);
        var sourceSiteRepository = new SourceSiteRepository(dbContext);

        if (sourceSiteRepository.Count() == 0)
        {
            sourceSiteRepository.Add(new SourceSite(
                Id: 0,
                Name: "Fabio",
                BaseUrl: "https://fabio.com.ar",
                IsActive: true,
                CreatedAt: DateTime.UtcNow));
        }

        var dbPath = Path.GetFullPath("interfazhumana.db");
        Console.WriteLine($"SQLite inicializado correctamente. Archivo: {dbPath}");
    }
}