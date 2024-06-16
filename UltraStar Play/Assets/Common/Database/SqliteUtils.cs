using System.Data;
using Mono.Data.Sqlite;

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
