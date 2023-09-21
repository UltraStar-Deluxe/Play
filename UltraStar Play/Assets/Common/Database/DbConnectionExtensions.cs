using System.Data;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public static class DbConnectionExtensions
{
    public static int ExecuteNonQuery(this IDbConnection dbConnection, string command)
    {
        IDbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = command;
        return dbCommand.ExecuteNonQuery();
    }

    public static IDataReader ExecuteQuery(this IDbConnection dbConnection, string query)
    {
        IDbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = query;
        IDataReader reader = dbCommand.ExecuteReader();
        return reader;
    }
}
