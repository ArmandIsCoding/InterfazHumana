using System.Text.Json;

namespace InterfazHumana.IO.Config;

public static class AppSettingsLoader
{
    public static AppSettings Load(string basePath)
    {
        var configPath = Path.Combine(basePath, "appsettings.json");
        if (!File.Exists(configPath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(configPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return settings ?? new AppSettings();
        }
        catch
        {
            // Si el JSON esta corrupto, seguimos con defaults para no romper el pipeline.
            return new AppSettings();
        }
    }
}

