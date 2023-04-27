using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace PhotoLibrary.DB
{
    public class DeviceEntry
    {
        static DeviceEntry Create(String path)
        {
            var info = new FileInfo(path);
            if (!info.Exists)
            {
                throw new FileNotFoundException(Resources.Messages_EN.FileNotFound, path);
            }

            var ret = new DeviceEntry();
            ret.FileSize = (UInt64)info.Length;
            ret.LastModifiedTime = info.LastWriteTimeUtc;
            ret.Path = path;
            return ret;
        }

        public String Path { get; private set; }
        public DateTime LastModifiedTime { get; private set; }
        public UInt64 FileSize { get; private set; }
    }
}
