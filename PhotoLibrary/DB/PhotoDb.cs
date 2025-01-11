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

    private void CreateTables(SqliteConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText =
            @"CREATE TABLE Photos (filename VARCHAR(20), size INT)";
        command.ExecuteNonQuery();
    }

    public void Deinit()
    {
        _sqliteConnection?.Close();
    }


}
