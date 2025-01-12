using Moq;
using Newtonsoft.Json.Bson;
using PhotoLibrary.DB;
using PhotoLibrary.Settings;
using System.Windows.Controls.Primitives;

namespace PhotoLibraryTests;

[TestClass]
public class DatabaseTests
{
    private static readonly string _databaseFile = "test.db";

    [ClassInitialize]
    public static void ClassInit(TestContext ctx)
    {
        if (File.Exists(_databaseFile))
        {
            File.Delete(_databaseFile);
        }
    }

    [TestMethod]
    public void TestDatabaseCreation()
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
    public void TestInsertPhoto()
    {
        // Arrange
        var settingsMock = new Mock<IApplicationSettings>();
        settingsMock.Setup(lib => lib.DatabasePath).Returns(_databaseFile);
        PhotoDb db = new PhotoDb(settingsMock.Object);
        Assert.IsFalse(File.Exists(_databaseFile));
        db.Init();

        // Act
        bool inserted = false;
        // bool inserted = db.InsertPhotoRecord(new PhotoRecord("picture.png", 1024,));
        
        // Assert
        Assert.IsTrue(inserted);
    }
}