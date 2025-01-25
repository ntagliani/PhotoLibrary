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
    private FileDb db = null;

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

    private IFileDb.FileRecord GenerateRandomRecord()
    {
        var hash = GenerateRandomString(20, HASH_CHARACTER_SET);
        return new IFileDb.FileRecord(-1, GenerateRandomString(8, FILE_CHARACTER_SET), m_rnd.Next(4 * KiloByte, 100 * MegaByte), hash, hash.Substring(0, 2), GenerateRandomDate());
    }

    private int AddRecord(FileDb db, IFileDb.FileRecord record)
    {
        return AddRecord(db, record, Enumerable.Empty<int>());
    }
    private int AddRecord(FileDb db, IFileDb.FileRecord record, IEnumerable<int> events)
    {
        return db.AddFile(record.Filename, record.Size, record.Hash, record.Path, record.CreationDate, events);
    }

    [TestInitialize]
    public void TestInit()
    {
        var settingsMock = new Mock<IApplicationSettings>();
        settingsMock.Setup(lib => lib.DatabasePath).Returns(_databaseFile);
        db = new FileDb(settingsMock.Object);
        Assert.IsFalse(File.Exists(_databaseFile));
        db.Init();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        db.Deinit();
        File.Delete(_databaseFile);
    }
    [TestMethod]
    public void Test_DatabaseCreation()
    {
        // Assert
        Assert.IsTrue(File.Exists(_databaseFile));
    }

    [TestMethod]
    public void Test_InsertFile()
    {
        // Arrange

        // Act
        int fileId1 = AddRecord(db, GenerateRandomRecord());
        int fileId2 = AddRecord(db, GenerateRandomRecord());

        Assert.IsTrue(fileId1 != fileId2);
    }

    [TestMethod]
    public void Test_GetFiles()
    {
        // Arrange
        Dictionary<int, IFileDb.FileRecord> insertedRecords = new();
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
            insertedRecords.TryGetValue(record.Id, out IFileDb.FileRecord? value);

            Assert.IsNotNull(value);
            Assert.AreEqual(value.Filename, record.Filename);
            Assert.AreEqual(value.Size, record.Size);
            Assert.AreEqual(value.Hash, record.Hash);
            Assert.AreEqual(value.Path, record.Path);
            Assert.AreEqual(value.CreationDate, record.CreationDate);

            insertedRecords.Remove(record.Id);
        }
        Assert.IsTrue(insertedRecords.Count == 0);
    }

    [TestMethod]
    public void Test_DeleteFile()
    {
        // Arrange
        Dictionary<int, IFileDb.FileRecord> insertedRecords = [];
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
    }
        
    [TestMethod]
    public void Test_InsertEvent()
    {
        // Arrange

        // Act
        var eventName = GenerateRandomString(40, FILE_CHARACTER_SET);
        int eventId1 = db.AddEvent(eventName);
        int eventId2 = db.AddEvent(eventName);

        // Assert
        Assert.IsTrue(eventId1 != eventId2);
    }

    [TestMethod]
    public void Test_GetAllEvents()
    {
        // Arrange
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
    }

    [TestMethod]
    public void Test_DeleteEvent()
    {
        // Arrange
        int eventToInsert = 40;
        Dictionary<int, string> insertedEvents = [];
        for (int i = 0; i < eventToInsert; i++)
        {
            var eventName = GenerateRandomString(40, FILE_CHARACTER_SET);
            int eventId = db.AddEvent(eventName);
            insertedEvents.Add(eventId, eventName);
        }

        var eventKeys = insertedEvents.Keys.ToList();
        eventKeys.Shuffle();
        var firstHalf = eventKeys[0..(eventToInsert / 2)];
        var secondHalf = eventKeys[(eventToInsert / 2)..^0];


        // Act
        db.DeleteEvents(firstHalf);
        foreach (var recordKey in secondHalf)
        {
            db.DeleteEvent(recordKey);
        }

        // Assert
        var recordsAfterDeletion = db.GetAllEvents();
        Assert.IsNotNull(recordsAfterDeletion);
        Assert.IsTrue(recordsAfterDeletion.Count() == 0);
    }

    struct InsertedRecord
    {
        public int id;
        public IFileDb.FileRecord record;
    }

    [TestMethod]
    public void Test_AddFilesToEvent()
    {
        // Arrange
        var event1Id = db.AddEvent("Event1");
        var event2Id = db.AddEvent("Event2");

        List<InsertedRecord> insertedRecords = new();
        int recordCount = 20;
        int evt1Count = 13;
        int evt2Count = 12;
        List<int> events = [];
        for (int i = 0; i < recordCount; i++)
        {
            if (i < evt1Count)
            {
                events.Add(event1Id);
            }
            if ( i > recordCount - evt2Count -1)
            {
                events.Add(event2Id);
            }
            var record = GenerateRandomRecord();
            var id = AddRecord(db, record, events);
            Assert.IsTrue(insertedRecords.Count(r => r.id == id) == 0);
            insertedRecords.Add(new InsertedRecord { id = id, record = record });
            events.Clear();
        }

        // Act
        var recordsEvt1 = db.GetFilesByEventId(event1Id);
        var recordsEvt2 = db.GetFilesByEventId(event2Id);

        // Assert
        Assert.IsTrue(recordsEvt1.Count() == evt1Count);
        Assert.IsTrue(recordsEvt2.Count() == evt2Count);
        foreach (var insertedEvent in insertedRecords[0..evt1Count])
        {
            Assert.IsTrue(recordsEvt1.Count(r => r.Id == insertedEvent.id) == 1);
        }

        foreach (var insertedEvent in insertedRecords[^evt2Count..^0])
        {
            Assert.IsTrue(recordsEvt2.Count(r => r.Id == insertedEvent.id) == 1);
        }
    }

    [TestMethod]
    public void Test_AssignFilesToEvent()
    {
        // Arrange
        var event1Id = db.AddEvent("Event1");
        var event2Id = db.AddEvent("Event2");

        List<InsertedRecord> insertedRecords = new();
        int recordCount = 20;
        int evt1Count = 13;
        int evt2Count = 12;
        List<int> evt1Records = [];
        List<int> evt2Records = [];
        for (int i = 0; i < recordCount; i++)
        {
            var record = GenerateRandomRecord();
            var id = AddRecord(db, record);
            if (i < evt1Count)
            {
                evt1Records.Add(id);
            }
            if (i > recordCount - evt2Count - 1)
            {
                evt2Records.Add(id);
            }
        }

        // Act
        db.AssignFileToEvent(evt1Records[0], event1Id);
        db.AssignFilesToEvent(evt1Records[1..^0], event1Id);
        db.AssignFileToEvent(evt2Records[0], event2Id);
        db.AssignFilesToEvent(evt2Records[1..^0], event2Id);

        // Assert
        var recordsEvt1 = db.GetFilesByEventId(event1Id);
        var recordsEvt2 = db.GetFilesByEventId(event2Id);
        Assert.IsTrue(recordsEvt1.Count() == evt1Count);
        Assert.IsTrue(recordsEvt2.Count() == evt2Count);
        foreach (var insertedRecord in evt1Records)
        {
            Assert.IsTrue(recordsEvt1.Count(r => r.Id == insertedRecord) == 1);
        }

        foreach (var insertedRecord in evt2Records)
        {
            Assert.IsTrue(recordsEvt2.Count(r => r.Id == insertedRecord) == 1);
        }
    }

    [TestMethod]
    public void Test_RemoveFilesFromEvent()
    {
        // Arrange
        var event1Id = db.AddEvent("Event1");
        var event2Id = db.AddEvent("Event2");

        List<InsertedRecord> insertedRecords = new();
        int recordCount = 20;
        int evt1Count = 13;
        int evt2Count = 12;
        List<int> evt1Records = [];
        List<int> evt2Records = [];
        for (int i = 0; i < recordCount; i++)
        {
            var record = GenerateRandomRecord();
            var id = AddRecord(db, record);
            if (i < evt1Count)
            {
                evt1Records.Add(id);
            }
            if (i > recordCount - evt2Count - 1)
            {
                evt2Records.Add(id);
            }
        }
        db.AssignFilesToEvent(evt1Records, event1Id);
        db.AssignFilesToEvent(evt2Records, event2Id);
        evt1Records.Shuffle();
        evt2Records.Shuffle();

        // Act
        db.DeleteFileFromEvent(evt1Records[0], event1Id);
        db.DeleteFilesFromEvent(evt1Records[1..(evt1Records.Count()/2)], event1Id);
        evt1Records = evt1Records[0..(evt1Records.Count()/2)];
        db.DeleteFileFromEvent(evt2Records[0], event2Id);
        db.DeleteFilesFromEvent(evt2Records[1..(evt2Records.Count() / 2)], event2Id);
        evt2Records = evt2Records[0..(evt2Records.Count()/2)];

        // Assert
        var recordsEvt1 = db.GetFilesByEventId(event1Id);
        var recordsEvt2 = db.GetFilesByEventId(event2Id);
        Assert.IsTrue(recordsEvt1.Count() == evt1Count);
        Assert.IsTrue(recordsEvt2.Count() == evt2Count);
        foreach (var insertedRecord in evt1Records)
        {
            Assert.IsTrue(recordsEvt1.Count(r => r.Id == insertedRecord) == 1);
        }

        foreach (var insertedRecord in evt2Records)
        {
            Assert.IsTrue(recordsEvt2.Count(r => r.Id == insertedRecord) == 1);
        }
    }
}