using System;
using System.Collections.Generic;
namespace PhotoLibrary.DB;

public interface IFileDb
{
    public record FileRecord(int Id, string Filename, int Size, string Hash, string Path, DateTime CreationDate);
    public record EventRecord(int Id, string Name);

    public void Init();
    public void Deinit();
    public int AddEvent(string eventName);
    public EventRecord GetEvent(int eventId);
    public void DeleteEvent(int eventId);

    public void DeleteEvents(IEnumerable<int> eventIds);
    public IEnumerable<EventRecord> GetAllEvents();

    public int AddFile(string filename, int size, string hash, string path, DateTime date, IEnumerable<int> events);
    public void AssignFileToEvent(int fileId, int eventId);
    public void AssignFilesToEvent(IEnumerable<int> files, int eventId);
    public void DeleteFileFromEvent(int fileId, int eventId);
    public void DeleteFilesFromEvent(IEnumerable<int> files, int eventId);
    public void DeleteFile(int fileId);
    public void DeleteFiles(IEnumerable<int> fileIds);
    public IEnumerable<FileRecord> GetFilesByEventId(int eventId);
    public IEnumerable<FileRecord> GetAllFiles();

}

