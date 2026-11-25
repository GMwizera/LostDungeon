namespace LostDungeon.Core.Save
{
    /// <summary>Where the save is kept. A file on desktop and mobile, browser storage on WebGL.</summary>
    public interface ISaveStorage
    {
        bool Exists();
        string Read();
        void Write(string text);
        void Delete();
    }
}
