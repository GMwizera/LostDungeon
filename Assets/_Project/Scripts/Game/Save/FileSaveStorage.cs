using System.IO;
using LostDungeon.Core.Save;

namespace LostDungeon.Game.Save
{
    /// <summary>Saves to a file. Writes a temp file first so a crash can't break it.</summary>
    public class FileSaveStorage : ISaveStorage
    {
        private readonly string path;

        public FileSaveStorage(string path) => this.path = path;

        public bool Exists() => File.Exists(path);
        public string Read() => File.ReadAllText(path);

        public void Write(string text)
        {
            string temp = path + ".tmp";
            File.WriteAllText(temp, text);
            if (File.Exists(path))
                File.Delete(path);
            File.Move(temp, path);
        }

        public void Delete()
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
