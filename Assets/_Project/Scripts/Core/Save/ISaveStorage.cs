namespace LostDungeon.Core.Save
{
    /// <summary>
    /// Where the save text lives. Desktop and mobile use a file; WebGL uses browser storage.
    /// The save logic only knows this interface, so the platform choice stays in one place.
    /// </summary>
    public interface ISaveStorage
    {
        bool Exists();
        string Read();
        void Write(string text);
        void Delete();
    }
}
