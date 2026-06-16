# InterfazHumana.IO

Infraestructura inicial para SQLite embebido y motor de ingesta web con ADO.NET nativo (`Microsoft.Data.Sqlite`) y `HtmlAgilityPack` en .NET 10.

## Que incluye

- Modelos de dominio en `Models/`.
- Inicializacion idempotente de schema en `Data/DatabaseInitializer.cs`.
- Contexto de conexion con `PRAGMA foreign_keys = ON` en `Data/DatabaseContext.cs`.
- Repositorios base para `SourceSites`, `IngestionLogs` y `RawContents` con sentencias preparadas.
- Seed inicial del sitio `LinksDV` en `Program.cs` si `SourceSites` esta vacia.
- Scraper especializado de grilla LinksDV (`Services/LinksDvScraper.cs`) con descubrimiento por `index?p=N`.
- Coordinador de pipeline incremental (`Services/IngestionEngine.cs`) para:
  - descubrir URLs nuevas;
  - guardar pendientes sin duplicar;
  - descargar lote de pendientes;
  - guardar contenido crudo y actualizar estado de ingesta.
- Configuracion por archivo `appsettings.json` para controlar:
  - `Ingestion.DiscoveryPages` (cantidad de paginas de LinksDV a escanear por corrida);
  - `Ingestion.BatchSize` (tamano del lote de pendientes);
  - `Scrapers.LinksDv.MaxPages` (paginas maximas a recorrer en discovery).

## Ejecutar

```bash
dotnet run --project ./InterfazHumana.IO/InterfazHumana.IO.csproj
```

Esto crea `interfazhumana.db` en el directorio actual del proceso.

