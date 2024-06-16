using System.Collections.Generic;
using System.Data;
using NUnit.Framework;
using UnityEngine;

public class SQLiteUtilsTest
{
    [Test]
    public void CreateDatabaseInsertAndRead()
    {
        string dbPath = $"{Application.persistentDataPath}/SQLiteTestDatabase.db";

        // Delete old database
        FileUtils.Delete(dbPath);

        // Open connection, auto close via using statement
        using IDbConnection dbConnection = SqliteUtils.OpenSqliteConnectionToFile(dbPath);

        // Insert data
        dbConnection.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS my_table (id INTEGER PRIMARY KEY, name STRING, age INTEGER)");
        dbConnection.ExecuteNonQuery("INSERT INTO my_table (id, name, age) VALUES (1, 'Alice', 42)");
        dbConnection.ExecuteNonQuery("INSERT INTO my_table (id, name, age) VALUES (2, 'Bob', 33)");

        // Read data
        IDataReader dataReader = dbConnection.ExecuteQuery("SELECT * FROM my_table");
        List<PersonDb> records = dataReader.ToList<PersonDb>();

        // Assert data is as expected
        if (records.Count != 2)
        {
            Assert.Fail("Unexpected number of rows returned");
        }
        PersonDb firstRecord = records[0];
        Assert.AreEqual(1, firstRecord.id);
        Assert.AreEqual("Alice", firstRecord.name);
        Assert.AreEqual(42, firstRecord.age);

        PersonDb secondRecord = records[1];
        Assert.AreEqual(2, secondRecord.id);
        Assert.AreEqual("Bob", secondRecord.name);
        Assert.AreEqual(33, secondRecord.age);
    }
}

public class PersonDb
{
    public long id;
    public string name;
    public long age;
}
