using Moq;
using PhotoLibrary.DB;
using PhotoLibrary.Settings;
using System.Diagnostics;

namespace PhotoLibraryTests;

[TestClass]
public class DatabaseTests
{
    [TestMethod]
    public void TestDatabaseCreation()
    {

        string testDbName = "test.db";

        var settingsMock = new Mock<IApplicationSettings>();
        settingsMock.Setup(lib => lib.DatabasePath).Returns(testDbName);
        PhotoDb db = new PhotoDb(settingsMock.Object);
        
        Assert.IsFalse(File.Exists(testDbName));

        db.Init();

        Assert.IsTrue(File.Exists(testDbName));
        db.Deinit();
        File.Delete(testDbName);
    }
}