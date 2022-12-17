namespace Coracle.IntegrationTests.Components.ClientHandling.Notes
{
    public interface INotes
    {
        Note Add(Note note);
        bool TryGet(string uniqueNoteName, out Note note);
        bool HasNote(Note note);
        IEnumerable<string> GetAllHeaders();
    }
}