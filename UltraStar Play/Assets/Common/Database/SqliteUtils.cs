using System.Data;
using Mono.Data.Sqlite;

/**
 * Utility class to work with SQLite databases.
 * This file is currently only used for the USDX database mod, at runtime.
 */
public static class SqliteUtils
{
    public static IDbConnection OpenSqliteConnectionToFile(string filePath)
    {
        return OpenSqliteConnection($"URI=file:{filePath}");
    }

    public static IDbConnection OpenSqliteConnection(string connectionString)
    {
        IDbConnection dbConnection = new SqliteConnection(connectionString);
        dbConnection.Open();
        return dbConnection;
    }
}
