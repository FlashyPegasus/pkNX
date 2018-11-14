﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace pkNX.Containers
{
    public class FolderContainer : IFileContainer
    {
        private readonly List<string> Paths = new List<string>();
        private readonly List<byte[]> Data = new List<byte[]>();
        private readonly List<bool> TrackModify = new List<bool>();

        public FolderContainer() { }
        public FolderContainer(IEnumerable<string> files) => AddFiles(files);

        public FolderContainer(string path)
        {
            FilePath = path;
            AddFiles(Directory.GetFiles(path, "*", SearchOption.AllDirectories));
        }

        public void AddFile(string file, byte[] data = null)
        {
            Paths.Add(file);
            Data.Add(data);
            TrackModify.Add(false);
        }

        public void AddFiles(IEnumerable<string> files)
        {
            foreach (var f in files)
                AddFile(f);
        }

        public byte[] GetFileData(string file)
        {
            var index = Paths.IndexOf(file);
            if (index < 0)
                return null;
            return Data[index] ?? (Data[index] = File.ReadAllBytes(file));
        }

        public byte[] GetFileData(int index)
        {
            if (index < 0 || Data.Count >= index)
                return null;
            return Data[index] ?? (Data[index] = File.ReadAllBytes(Paths[index]));
        }

        public byte[] this[int index]
        {
            get => GetFileData(index);
            set
            {
                if (value != null && Data[index] != null)
                    TrackModify[index] = value.SequenceEqual(Data[index]);
                Data[index] = value;
            }
        }

        public string FilePath { get; set; }
        public bool Modified => TrackModify.Count != 0;
        public int Count => Paths.Count;

        public Task<byte[][]> GetFiles() => new Task<byte[][]>(() => Paths.Select(File.ReadAllBytes).ToArray());
        public Task<byte[]> GetFile(int file, int subFile = 0) => new Task<byte[]>(() => this[file]);
        public Task SetFile(int file, byte[] value, int subFile = 0) => new Task(() => this[file] = value);
        public Task SaveAs(string path, ContainerHandler handler, CancellationToken token) => new Task(SaveAll);

        private void SaveAll()
        {
            for (int i = 0; i < Paths.Count; i++)
            {
                if (!TrackModify[i])
                    continue;
                var data = Data[i];
                if (data == null)
                    continue;
                File.WriteAllBytes(Paths[i], data);
            }
        }

        public void Dump(string path, ContainerHandler handler)
        {
            SaveAll(); // there's really nothing to dump, just save any modified
        }
    }
}
