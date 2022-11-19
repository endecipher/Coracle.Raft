using EventGuidance.Logging;
using Raft.Web.Canoe.Logging;
using System.Collections.Concurrent;

namespace Raft.Web.Canoe.ClientHandling.Notes
{
    public class Notes : INotes
    {
        public IEventLogger Logger { get; }

        public ConcurrentDictionary<string, Note> NoteMap { get; }

        public Notes(IEventLogger logger)
        {
            Logger = logger;
            NoteMap = new ConcurrentDictionary<string, Note>();
        }

        public bool TryGet(string noteName, out Note note)
        {
            var hasNote = NoteMap.TryGetValue(noteName, out var value);
            Logger.LogDebug(nameof(Notes), noteName.ToString(), $"{nameof(TryGet)} returns {value}");
            note = value;
            return hasNote;
        }

        public Note Add(Note note)
        {
            NoteMap.TryAdd(note.UniqueHeader, note);
            Logger.LogDebug(nameof(Notes), note.UniqueHeader, $"New Note added with header {note.UniqueHeader} for {nameof(Add)} with text: {note.Text}");
            return note;
        }

        public bool HasNote(Note note)
        {
            return NoteMap.ContainsKey(note.UniqueHeader);
        }
    }
}
