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
				LinksDvPage INTEGER NOT NULL DEFAULT 0,
				Status TEXT NOT NULL CHECK (Status IN ('Pending', 'Downloaded', 'Failed')),
				ErrorMessage TEXT NULL,
				LastScrapedAt TEXT NOT NULL,
				FOREIGN KEY (SourceSiteId) REFERENCES SourceSites(Id) ON DELETE CASCADE
			);
			""",
			"""
			CREATE TABLE IF NOT EXISTS RawContents (
				Id INTEGER PRIMARY KEY AUTOINCREMENT,
				IngestionLogId INTEGER NOT NULL,
				RawHtml TEXT NULL,
				ExtractedTitle TEXT NOT NULL,
				ExtractedDescription TEXT NOT NULL,
				ExtractedImageUrl TEXT NULL,
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
			""",
			"""
			CREATE UNIQUE INDEX IF NOT EXISTS IX_RawContents_IngestionLogId
				ON RawContents(IngestionLogId);
			"""
		};

		foreach (var statement in statements)
		{
			using var command = connection.CreateCommand();
			command.CommandText = statement;
			command.Prepare();
			command.ExecuteNonQuery();
		}

		EnsureLinksDvSchema(connection);
	}

	private static void EnsureLinksDvSchema(SqliteConnection connection)
	{
		EnsureIngestionLogsTable(connection);
		EnsureRawContentsTable(connection);
	}

	private static void EnsureIngestionLogsTable(SqliteConnection connection)
	{
		var columns = GetColumnNames(connection, "IngestionLogs");
		if (columns.Count == 0)
		{
			return;
		}

		if (columns.Contains("LinksDvPage", StringComparer.OrdinalIgnoreCase)
			&& !columns.Contains("ContentHash", StringComparer.OrdinalIgnoreCase))
		{
			return;
		}

		var hasLinksDvPage = columns.Contains("LinksDvPage", StringComparer.OrdinalIgnoreCase);
		var hasLastScrapedAt = columns.Contains("LastScrapedAt", StringComparer.OrdinalIgnoreCase);

		var migrationStatements = new[]
		{
			"""
			CREATE TABLE IngestionLogs_Tmp (
				Id INTEGER PRIMARY KEY AUTOINCREMENT,
				SourceSiteId INTEGER NOT NULL,
				TargetUrl TEXT NOT NULL,
				LinksDvPage INTEGER NOT NULL DEFAULT 0,
				Status TEXT NOT NULL CHECK (Status IN ('Pending', 'Downloaded', 'Failed')),
				ErrorMessage TEXT NULL,
				LastScrapedAt TEXT NOT NULL,
				FOREIGN KEY (SourceSiteId) REFERENCES SourceSites(Id) ON DELETE CASCADE
			);
			""",
			$"""
			INSERT INTO IngestionLogs_Tmp (Id, SourceSiteId, TargetUrl, LinksDvPage, Status, ErrorMessage, LastScrapedAt)
			SELECT Id,
			       SourceSiteId,
			       TargetUrl,
			       {(hasLinksDvPage ? "COALESCE(LinksDvPage, 0)" : "0")},
			       CASE WHEN Status IN ('Pending', 'Downloaded', 'Failed') THEN Status ELSE 'Pending' END,
			       ErrorMessage,
			       {(hasLastScrapedAt ? "LastScrapedAt" : "strftime('%Y-%m-%dT%H:%M:%fZ','now')")}
			FROM IngestionLogs;
			""",
			"DROP TABLE IngestionLogs;",
			"ALTER TABLE IngestionLogs_Tmp RENAME TO IngestionLogs;",
			"CREATE UNIQUE INDEX IF NOT EXISTS IX_IngestionLogs_TargetUrl ON IngestionLogs(TargetUrl);",
			"CREATE INDEX IF NOT EXISTS IX_IngestionLogs_Status ON IngestionLogs(Status);"
		};

		ExecuteStatements(connection, migrationStatements);
	}

	private static void EnsureRawContentsTable(SqliteConnection connection)
	{
		var columns = GetColumnNames(connection, "RawContents");
		if (columns.Count == 0)
		{
			return;
		}

		if (columns.Contains("ExtractedTitle", StringComparer.OrdinalIgnoreCase)
			&& columns.Contains("ExtractedDescription", StringComparer.OrdinalIgnoreCase)
			&& columns.Contains("ExtractedImageUrl", StringComparer.OrdinalIgnoreCase)
			&& !columns.Contains("CleanedText", StringComparer.OrdinalIgnoreCase))
		{
			return;
		}

		var hasRawHtml = columns.Contains("RawHtml", StringComparer.OrdinalIgnoreCase);
		var hasExtractedTitle = columns.Contains("ExtractedTitle", StringComparer.OrdinalIgnoreCase);
		var hasExtractedDescription = columns.Contains("ExtractedDescription", StringComparer.OrdinalIgnoreCase);
		var hasExtractedImageUrl = columns.Contains("ExtractedImageUrl", StringComparer.OrdinalIgnoreCase);

		var migrationStatements = new[]
		{
			"""
			CREATE TABLE RawContents_Tmp (
				Id INTEGER PRIMARY KEY AUTOINCREMENT,
				IngestionLogId INTEGER NOT NULL,
				RawHtml TEXT NULL,
				ExtractedTitle TEXT NOT NULL,
				ExtractedDescription TEXT NOT NULL,
				ExtractedImageUrl TEXT NULL,
				FOREIGN KEY (IngestionLogId) REFERENCES IngestionLogs(Id) ON DELETE CASCADE
			);
			""",
			$"""
			INSERT INTO RawContents_Tmp (Id, IngestionLogId, RawHtml, ExtractedTitle, ExtractedDescription, ExtractedImageUrl)
			SELECT Id,
			       IngestionLogId,
			       {(hasRawHtml ? "RawHtml" : "NULL")},
			       {(hasExtractedTitle ? "COALESCE(ExtractedTitle, '')" : "''")},
			       {(hasExtractedDescription ? "COALESCE(ExtractedDescription, '')" : "''")},
			       {(hasExtractedImageUrl ? "ExtractedImageUrl" : "NULL")}
			FROM RawContents;
			""",
			"DROP TABLE RawContents;",
			"ALTER TABLE RawContents_Tmp RENAME TO RawContents;",
			"CREATE UNIQUE INDEX IF NOT EXISTS IX_RawContents_IngestionLogId ON RawContents(IngestionLogId);"
		};

		ExecuteStatements(connection, migrationStatements);
	}

	private static HashSet<string> GetColumnNames(SqliteConnection connection, string tableName)
	{
		var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		using var command = connection.CreateCommand();
		command.CommandText = $"PRAGMA table_info({tableName});";
		command.Prepare();

		using var reader = command.ExecuteReader();
		while (reader.Read())
		{
			columns.Add(reader.GetString(1));
		}

		return columns;
	}

	private static void ExecuteStatements(SqliteConnection connection, IEnumerable<string> statements)
	{
		foreach (var statement in statements)
		{
			using var command = connection.CreateCommand();
			command.CommandText = statement;
			command.Prepare();
			command.ExecuteNonQuery();
		}
	}
}

