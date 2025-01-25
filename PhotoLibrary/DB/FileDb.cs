using Microsoft.Data.Sqlite;
using PhotoLibrary.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Transactions;
using System.Windows.Controls;

namespace PhotoLibrary.DB;


[Export(typeof(IFileDb))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class FileDb : IFileDb
{
    private IApplicationSettings _settings;
    private SqliteConnection _sqliteConnection;

    [ImportingConstructor]
    public FileDb(IApplicationSettings settings)
    {
        _settings = settings;
    }

    public void Init()
    {
        bool createTables = false;
        if (!File.Exists(_settings.DatabasePath))
        {
            CreateMissingFolders(_settings.DatabasePath);
            createTables = true;
        }
        string connectionString = $"Data Source={_settings.DatabasePath};";
        _sqliteConnection = new SqliteConnection(connectionString);
        _sqliteConnection.Open();
        if (createTables)
        {
            CreateTables(_sqliteConnection);
        }
    }

    public void Deinit()
    {
        _sqliteConnection?.Close();
        SqliteConnection.ClearAllPools();
    }

    public int AddEvent(string eventName)
    {
        var command = _sqliteConnection.CreateCommand();
        command.CommandText = $"INSERT INTO events (name) VALUES (@eventName)";
        command.Parameters.AddWithValue("@eventName", eventName);
        int rowsAffected = command.ExecuteNonQuery();
        if (rowsAffected == 0)
        {
            throw new Exception("Unable to insert event");
        }
        command.CommandText = "SELECT last_insert_rowid()";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public IFileDb.EventRecord GetEvent(int eventId)
    {
        var command = _sqliteConnection.CreateCommand();
        command.CommandText = $"SELECT FROM events WHERE id = {eventId}";
        var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return GetFullEventRecord(reader);
        }
        else return null;
    }
    public void DeleteEvent(int eventId)
    {
        DeleteEvents(new int[] { eventId });
    }

    public void DeleteEvents(IEnumerable<int> eventIds)
    {
        var command = _sqliteConnection.CreateCommand();
        List<SqliteParameter> parameters = [];
        command.CommandText = $"DELETE FROM events WHERE id IN ({String.Join(", ", eventIds)})";
        int rowsAffected = command.ExecuteNonQuery();
        if (rowsAffected != eventIds.Count())
        {
            throw new Exception("Not all the requested lines where deleted");
        }
    }

    public IEnumerable<IFileDb.EventRecord> GetAllEvents()
    {
        var command = _sqliteConnection.CreateCommand();
        command.CommandText = "SELECT * FROM events";
        var reader = command.ExecuteReader();
        List<IFileDb.EventRecord> events = [];
        while (reader.Read())
        {
            events.Add(GetFullEventRecord(reader));
        }
        return events;
    }

    public int AddFile(string filename, int size, string hash, string path, DateTime date, IEnumerable<int> events)
    {
        int fileId = -1;
        using (var transaction = _sqliteConnection.BeginTransaction())
        {
            fileId = AddFileInternal(filename, size, hash, path, date);

            foreach (var evt in events)
            {
                AssignFilesToEventInternal(new int[] { fileId }, evt, transaction);
            }
            transaction.Commit();
        }
        return fileId;
    }

    public void AssignFileToEvent(int fileId, int eventId)
    {
        AssignFilesToEvent(new int[] { fileId }, eventId);
    }

    public void AssignFilesToEvent(IEnumerable<int> files, int eventId)
    {
        using (SqliteTransaction transaction = _sqliteConnection.BeginTransaction())
        {
            AssignFilesToEventInternal(files, eventId, transaction);
            transaction.Commit();
        }
    }
    public void DeleteFileFromEvent(int fileId, int eventId)
    {
        DeleteFilesFromEvent(new int[] { fileId }, eventId);
    }
    public void DeleteFilesFromEvent(IEnumerable<int> files, int eventId)
    {
        using (var transaction = _sqliteConnection.BeginTransaction())
        {
            using (var command = _sqliteConnection.CreateCommand())
            {
                command.Transaction = transaction;


                StringBuilder builder = new StringBuilder();
                bool isFirst = true;
                int maxQueryLenght = 4096;
                var baseSqlQuery = "DELETE FROM fileEvent WHERE ";
                int rowsAffected = 0;
                foreach (int fileId in files)
                {
                    if (!isFirst)
                    {
                        builder.Append(" OR ");
                    }

                    builder.Append($"(fileId={fileId} AND eventId={eventId})");
                    isFirst = false;

                    if (builder.Length > maxQueryLenght)
                    {
                        command.CommandText = baseSqlQuery + builder.ToString();
                        rowsAffected += command.ExecuteNonQuery();
                        builder.Clear();
                    }
                }

                if (builder.Length > 0)
                {
                    command.CommandText = baseSqlQuery + builder.ToString();
                    rowsAffected += command.ExecuteNonQuery();
                }
            }
        }
    }

    public void DeleteFile(int fileId)
    {
        DeleteFiles(new int[] { fileId });
    }
    public void DeleteFiles(IEnumerable<int> fileIds)
    {
        var command = _sqliteConnection.CreateCommand();
        List<SqliteParameter> parameters = [];
        command.CommandText = $"DELETE FROM files WHERE id IN ({String.Join(", ", fileIds)})";
        int rowsAffected = command.ExecuteNonQuery();
        if (rowsAffected != fileIds.Count())
        {
            throw new Exception("Not all the requested lines where deleted");
        }
    }
    public IEnumerable<IFileDb.FileRecord> GetFilesByEventId(int eventId)
    {
        var command = _sqliteConnection.CreateCommand();
        List<IFileDb.FileRecord> records = [];
        command.CommandText = $"SELECT * FROM files WHERE id in (SELECT fileId FROM fileEvent WHERE eventId = {eventId})";
        var reader = command.ExecuteReader();
        while (reader.Read())
        {
            records.Add(GetFullFileRecord(reader));
        }
        return records;
    }

    public IEnumerable<IFileDb.FileRecord> GetAllFiles()
    {
        var command = _sqliteConnection.CreateCommand();
        List<IFileDb.FileRecord> records = [];
        command.CommandText = "SELECT * FROM files";
        var reader = command.ExecuteReader();
        while (reader.Read())
        {
            records.Add(GetFullFileRecord(reader));
        }

        return records;
    }


    #region Private functionalities
    private void AssignFilesToEventInternal(IEnumerable<int> files, int eventId, SqliteTransaction transaction)
    {
        using (var command = _sqliteConnection.CreateCommand())
        {
            if (transaction != null)
                command.Transaction = transaction;

            StringBuilder builder = new StringBuilder();
            bool isFirst = true;
            int maxQueryLenght = 4096;
            var baseSqlQuery = "INSERT INTO fileEvent (fileId, eventId) VALUES ";
            int rowsAffected = 0;
            foreach (int fileId in files)
            {
                if (!isFirst)
                {
                    builder.Append(", ");
                }

                builder.Append($"({fileId}, {eventId})");
                isFirst = false;

                if (builder.Length > maxQueryLenght)
                {
                    command.CommandText = baseSqlQuery + builder.ToString();
                    rowsAffected = command.ExecuteNonQuery();
                    builder.Clear();
                }
            }

            if (builder.Length > 0)
            {
                command.CommandText = baseSqlQuery + builder.ToString();
                rowsAffected += command.ExecuteNonQuery();
            }
        }
    }

    private static void CreateMissingFolders(string filePath)
    {
        var path = Path.GetDirectoryName(filePath);
        if (path != null && path != string.Empty)
            Directory.CreateDirectory(path);
    }
    private static void CreateTables(SqliteConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText =
            @"CREATE TABLE files (id INTEGER PRIMARY KEY ASC AUTOINCREMENT, filename VARCHAR(256) , size INT, hash CHARACTER(64), path VARCHAR(256), creation DATETIME)";
        command.ExecuteNonQuery();

        command.CommandText =
                    @"CREATE TABLE events (id INTEGER PRIMARY KEY ASC AUTOINCREMENT, name VARCHAR(256))";
        command.ExecuteNonQuery();

        command.CommandText =
                    @"CREATE TABLE fileEvent (fileId INTEGER REFERENCES files (id), eventId INTEGER REFERENCES events (id), PRIMARY KEY (fileId, eventId))";
        command.ExecuteNonQuery();
    }


    private static IFileDb.FileRecord GetFullFileRecord(SqliteDataReader reader)
    {
        DateTime creationDate = DateTime.Parse(reader["creation"].ToString());
        return new IFileDb.FileRecord(Convert.ToInt32(reader["id"]), reader["filename"].ToString(), Convert.ToInt32(reader["size"]), reader["hash"].ToString(), reader["path"].ToString(), creationDate);
    }
    private static IFileDb.EventRecord GetFullEventRecord(SqliteDataReader reader)
    {
        return new IFileDb.EventRecord(Convert.ToInt32(reader["Id"]), reader["name"].ToString());
    }

    private int AddFileInternal(string filename, int size, string hash, string path, DateTime date)
    {
        var command = _sqliteConnection.CreateCommand();
        command.CommandText = $"INSERT INTO files (filename, size, hash, path, creation) VALUES (@filename, @size, @hash, @path, @date)";
        command.Parameters.AddWithValue("@filename", filename);
        command.Parameters.AddWithValue("@size", size);
        command.Parameters.AddWithValue("@hash", hash);
        command.Parameters.AddWithValue("@path", path);
        command.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd HH:mm:ss"));
        int rowsAffected = command.ExecuteNonQuery();
        if (rowsAffected == 0)
        {
            throw new Exception("Unable to insert file");
        }
        command.CommandText = "SELECT last_insert_rowid()";
        return Convert.ToInt32(command.ExecuteScalar());
    }
    #endregion
}
