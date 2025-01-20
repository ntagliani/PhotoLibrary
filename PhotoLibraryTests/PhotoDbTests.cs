using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json.Bson;
using PhotoLibrary.DB;
using PhotoLibrary.Settings;
using System.Security.Cryptography;
using System.Windows.Controls.Primitives;

namespace PhotoLibraryTests;

public static class Extensions
{
    public static void Shuffle<T>(this IList<T> list)
    {
        Random rng = new Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}

[TestClass]
public class DatabaseTests
{
    private static readonly string _databaseFile = "test.db";
    private static readonly int KiloByte = 1024;
    private static readonly int MegaByte = 1024 * KiloByte;
    private static readonly int GigaByte = 1024 * MegaByte;
    private static readonly string HASH_CHARACTER_SET = "1234567890abcdef";
    private static readonly string FILE_CHARACTER_SET = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890";
    private Random m_rnd = new Random();

    [ClassInitialize]
    public static void ClassInit(TestContext ctx)
    {
        if (File.Exists(_databaseFile))
        {
            File.Delete(_databaseFile);
        }
    }

    private string GenerateRandomString(int length, string characterSet)
    {
        var strChar = new char[length];
        for (int i = 0; i < length; i++)
        {
            strChar[i] = characterSet[m_rnd.Next(characterSet.Length)];
        }
        return new string(strChar);
    }

    private DateTime GenerateRandomDate()
    {
        long startTick = 615044448; // =  var startDate = new DateTime(1950, 1,1,00,00,00,00).Tick / 1000000000;
        long endTick = 638712864; //  var endDate = new DateTime(2025, 1, 1, 00, 00, 00, 00).Tick / 1000000000;
        return new DateTime(m_rnd.NextInt64(startTick, endTick) * 1000000000); // make sure that the nanomicroseconds are 0
    }

    private PhotoDb.FileRecord GenerateRandomRecord()
    {
        var hash = GenerateRandomString(20, HASH_CHARACTER_SET);
        return new PhotoDb.FileRecord(-1, GenerateRandomString(8, FILE_CHARACTER_SET), m_rnd.Next(4 * KiloByte, 100 * MegaByte), hash, hash.Substring(0, 2), GenerateRandomDate());
    }

    private int AddRecord(PhotoDb db, PhotoDb.FileRecord record)
    {
        return db.AddFile(record.Filename, record.Size, record.Hash, record.Path, record.CreationDate, Enumerable.Empty<int>());
    }

    [TestMethod]
    public void Test_DatabaseCreation()
    {
        // Arrange
        var settingsMock = new Mock<IApplicationSettings>();
        settingsMock.Setup(lib => lib.DatabasePath).Returns(_databaseFile);
        PhotoDb db = new PhotoDb(settingsMock.Object);
        Assert.IsFalse(File.Exists(_databaseFile));

        // Act
        db.Init();

        // Assert
        Assert.IsTrue(File.Exists(_databaseFile));
        db.Deinit();
        File.Delete(_databaseFile);
    }

    [TestMethod]
    public void Test_InsertFile()
    {
        // Arrange
        var settingsMock = new Mock<IApplicationSettings>();
        settingsMock.Setup(lib => lib.DatabasePath).Returns(_databaseFile);
        PhotoDb db = new PhotoDb(settingsMock.Object);
        Assert.IsFalse(File.Exists(_databaseFile));
        db.Init();

        // Act
        int fileId1 = AddRecord(db, GenerateRandomRecord());
        int fileId2 = AddRecord(db, GenerateRandomRecord());

        Assert.IsTrue(fileId1 != fileId2);

        db.Deinit();
        File.Delete(_databaseFile);
    }

    [TestMethod]
    public void Test_GetFiles()
    {
        // Arrange
        var settingsMock = new Mock<IApplicationSettings>();
        settingsMock.Setup(lib => lib.DatabasePath).Returns(_databaseFile);
        PhotoDb db = new PhotoDb(settingsMock.Object);
        Assert.IsFalse(File.Exists(_databaseFile));
        db.Init();
        Dictionary<int, PhotoDb.FileRecord> insertedRecords = new();
        int recordCount = 10;
        for (int i = 0; i < recordCount; i++)
        {
            var record = GenerateRandomRecord();
            var id = AddRecord(db, record);
            Assert.IsFalse(insertedRecords.ContainsKey(id));
            insertedRecords.Add(id, record);
        }

        // Act
        var records = db.GetAllFiles();

        // Assert
        Assert.IsNotNull(records);
        Assert.IsTrue(records.Count() == recordCount);
        foreach (var record in records)
        {
            Assert.IsTrue(insertedRecords.ContainsKey(record.Id));
            insertedRecords.TryGetValue(record.Id, out PhotoDb.FileRecord? value);

            Assert.IsNotNull(value);
            Assert.AreEqual(value.Filename, record.Filename);
            Assert.AreEqual(value.Size, record.Size);
            Assert.AreEqual(value.Hash, record.Hash);
            Assert.AreEqual(value.Path, record.Path);
            Assert.AreEqual(value.CreationDate, record.CreationDate);

            insertedRecords.Remove(record.Id);
        }
        Assert.IsTrue(insertedRecords.Count == 0);

        db.Deinit();
        File.Delete(_databaseFile);
    }

    [TestMethod]
    public void Test_RemoveFile()
    {
        // Arrange
        var settingsMock = new Mock<IApplicationSettings>();
        settingsMock.Setup(lib => lib.DatabasePath).Returns(_databaseFile);
        PhotoDb db = new PhotoDb(settingsMock.Object);
        Assert.IsFalse(File.Exists(_databaseFile));
        db.Init();
        Dictionary<int, PhotoDb.FileRecord> insertedRecords = new();
        int recordCount = 10;
        for (int i = 0; i < recordCount; i++)
        {
            var record = GenerateRandomRecord();
            var id = AddRecord(db, record);
            Assert.IsFalse(insertedRecords.ContainsKey(id));
            insertedRecords.Add(id, record);
        }
        var records = db.GetAllFiles();
        Assert.IsNotNull(records);
        Assert.IsTrue(records.Count() == recordCount);

        var keyList = insertedRecords.Keys.ToList();
        keyList.Shuffle();
        var firstHalf = keyList[0..(recordCount / 2)];
        var secondHalf = keyList[(recordCount / 2)..^0];


        // Act
        db.DeleteFiles(firstHalf);
        foreach (var recordKey in secondHalf)
        {
            db.DeleteFile(recordKey);
        }

        // Assert
        var recordsAfterDeletion = db.GetAllFiles();
        Assert.IsNotNull(recordsAfterDeletion);
        Assert.IsTrue(recordsAfterDeletion.Count() == 0);

        db.Deinit();
        File.Delete(_databaseFile);
    }



    [TestMethod]
    public void Test_InsertEvent()
    {
        // Arrange
        var settingsMock = new Mock<IApplicationSettings>();
        settingsMock.Setup(lib => lib.DatabasePath).Returns(_databaseFile);
        PhotoDb db = new PhotoDb(settingsMock.Object);
        Assert.IsFalse(File.Exists(_databaseFile));
        db.Init();

        // Act
        var eventName = GenerateRandomString(40, FILE_CHARACTER_SET);
        int eventId1 = db.AddEvent(eventName);
        int eventId2 = db.AddEvent(eventName);

        // Assert
        Assert.IsTrue(eventId1 != eventId2);

        db.Deinit();
        File.Delete(_databaseFile);
    }

    [TestMethod]
    public void Test_GetAllEvents()
    {
        // Arrange
        var settingsMock = new Mock<IApplicationSettings>();
        settingsMock.Setup(lib => lib.DatabasePath).Returns(_databaseFile);
        PhotoDb db = new PhotoDb(settingsMock.Object);
        Assert.IsFalse(File.Exists(_databaseFile));
        db.Init();

        int eventToInsert = 40;
        Dictionary<int, string> insertedEvents = [];
        for (int i = 0; i < eventToInsert; i++)
        {
            var eventName = GenerateRandomString(40, FILE_CHARACTER_SET);
            int eventId = db.AddEvent(eventName);
            insertedEvents.Add(eventId, eventName);
        }

        // Act
        var events = db.GetAllEvents();

        foreach (var evt in events)
        {
            Assert.IsTrue(insertedEvents.ContainsKey(evt.Id));
            Assert.AreEqual(insertedEvents[evt.Id], evt.Name);
            insertedEvents.Remove(evt.Id);
        }
        Assert.IsTrue(insertedEvents.Count == 0);

        db.Deinit();
        File.Delete(_databaseFile);
    }
}