# InterfazHumana.IO

Infraestructura inicial para SQLite embebido con ADO.NET nativo (`Microsoft.Data.Sqlite`) en .NET 10.

## Que incluye

- Modelos de dominio en `Models/`.
- Inicializacion idempotente de schema en `Data/DatabaseInitializer.cs`.
- Contexto de conexion con `PRAGMA foreign_keys = ON` en `Data/DatabaseContext.cs`.
- Repositorios base para `SourceSites` e `IngestionLogs` con sentencias preparadas.
- Seed inicial del sitio `Fabio` en `Program.cs` si `SourceSites` esta vacia.

## Ejecutar

```bash
dotnet run --project ./InterfazHumana.IO/InterfazHumana.IO.csproj
```

Esto crea `interfazhumana.db` en el directorio actual del proceso.

