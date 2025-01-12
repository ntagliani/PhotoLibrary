using Microsoft.Data.Sqlite;
using PhotoLibrary.Settings;
using System.ComponentModel.Composition;
using System.IO;

namespace PhotoLibrary.DB;

[Export(typeof(PhotoDb))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class PhotoDb
{
    private IApplicationSettings _settings;

    private SqliteConnection _sqliteConnection;

    [ImportingConstructor]
    public PhotoDb(IApplicationSettings settings)
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
    private void CreateMissingFolders(string filePath)
    {
        var path = Path.GetDirectoryName(filePath);
        if (path != null && path != string.Empty)
            Directory.CreateDirectory(path);
    }
    private void CreateTables(SqliteConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText =
            @"CREATE TABLE photos (id INTEGER PRIMARY KEY ASC AUTOINCREMENT, filename VARCHAR(256) , size INT, hash CHARACTER(64), created INT, path VARCHAR(256), creation DATETIME)";
        command.ExecuteNonQuery();

        command.CommandText =
                    @"CREATE TABLE events (id INTEGER PRIMARY KEY ASC AUTOINCREMENT, name VARCHAR(256))";
        command.ExecuteNonQuery();

        command.CommandText =
                    @"CREATE TABLE photoEvent (photoId INTEGER REFERENCES photos (id), eventId INTEGER REFERENCES events (id))";
        command.ExecuteNonQuery(); 
    }
    public void Deinit()
    {
        _sqliteConnection?.Close();
        SqliteConnection.ClearAllPools(); 
    }


}
