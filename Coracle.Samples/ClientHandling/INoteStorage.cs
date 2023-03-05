namespace Coracle.Raft.Examples.ClientHandling
{
    public interface INoteStorage
    {
        Note Add(Note note);
        bool TryGet(string uniqueNoteName, out Note note);
        bool HasNote(Note note);
        IEnumerable<string> GetAllHeaders();
        void Reset();
        byte[] ExportData();
        void Build(byte[] exportedData);
    }
}