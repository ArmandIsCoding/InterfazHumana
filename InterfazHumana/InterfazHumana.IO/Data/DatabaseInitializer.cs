using Microsoft.Data.Sqlite;

namespace InterfazHumana.IO.Data;

public static class DatabaseInitializer
{
	public static void Initialize(string connectionString)
	{
		using var connection = new SqliteConnection(connectionString);
		connection.Open();

		using var pragmaCommand = connection.CreateCommand();
		pragmaCommand.CommandText = "PRAGMA foreign_keys = ON;";
		pragmaCommand.Prepare();
		pragmaCommand.ExecuteNonQuery();

		var statements = new[]
		{
			"""
			CREATE TABLE IF NOT EXISTS SourceSites (
				Id INTEGER PRIMARY KEY AUTOINCREMENT,
				Name TEXT NOT NULL,
				BaseUrl TEXT NOT NULL,
				IsActive INTEGER NOT NULL,
				CreatedAt TEXT NOT NULL
			);
			""",
			"""
			CREATE TABLE IF NOT EXISTS IngestionLogs (
				Id INTEGER PRIMARY KEY AUTOINCREMENT,
				SourceSiteId INTEGER NOT NULL,
				TargetUrl TEXT NOT NULL,
				ContentHash TEXT NOT NULL,
				Status TEXT NOT NULL CHECK (Status IN ('Pending', 'Downloaded', 'Processed', 'Failed')),
				ErrorMessage TEXT NULL,
				LastScrapedAt TEXT NOT NULL,
				FOREIGN KEY (SourceSiteId) REFERENCES SourceSites(Id) ON DELETE CASCADE
			);
			""",
			"""
			CREATE TABLE IF NOT EXISTS RawContents (
				Id INTEGER PRIMARY KEY AUTOINCREMENT,
				IngestionLogId INTEGER NOT NULL,
				RawHtml TEXT NOT NULL,
				CleanedText TEXT NOT NULL,
				ExtractedImages TEXT NOT NULL,
				FOREIGN KEY (IngestionLogId) REFERENCES IngestionLogs(Id) ON DELETE CASCADE
			);
			""",
			"""
			CREATE TABLE IF NOT EXISTS Categories (
				Id INTEGER PRIMARY KEY AUTOINCREMENT,
				Name TEXT NOT NULL,
				Slug TEXT NOT NULL,
				CONSTRAINT UQ_Categories_Name UNIQUE (Name)
			);
			""",
			"""
			CREATE TABLE IF NOT EXISTS ProcessedPosts (
				Id INTEGER PRIMARY KEY AUTOINCREMENT,
				IngestionLogId INTEGER NOT NULL,
				CategoryId INTEGER NULL,
				Title TEXT NOT NULL,
				Content TEXT NOT NULL,
				PublishedAt TEXT NOT NULL,
				FOREIGN KEY (IngestionLogId) REFERENCES IngestionLogs(Id) ON DELETE CASCADE,
				FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE SET NULL
			);
			""",
			"""
			CREATE UNIQUE INDEX IF NOT EXISTS IX_IngestionLogs_TargetUrl
				ON IngestionLogs(TargetUrl);
			""",
			"""
			CREATE INDEX IF NOT EXISTS IX_IngestionLogs_Status
				ON IngestionLogs(Status);
			"""
		};

		foreach (var statement in statements)
		{
			using var command = connection.CreateCommand();
			command.CommandText = statement;
			command.Prepare();
			command.ExecuteNonQuery();
		}
	}
}

