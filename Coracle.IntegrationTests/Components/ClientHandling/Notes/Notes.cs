using System.Collections.Concurrent;
using ActivityLogger.Logging;
using Coracle.IntegrationTests.Components.Logging;

namespace Coracle.IntegrationTests.Components.ClientHandling.Notes
{
    public class Notes : INotes
    {
        #region Constants

        private const string NoteEntity = nameof(Notes);
        private const string NoteName = nameof(NoteName);
        private const string Note = nameof(Note);
        private const string IsNotePresent = nameof(IsNotePresent);
        private const string GetNote = nameof(GetNote);
        private const string AddNote = nameof(AddNote);

        #endregion

        IActivityLogger ActivityLogger { get; }

        public ConcurrentDictionary<string, Note> NoteMap { get; }

        public Notes(IActivityLogger activityLogger)
        {
            NoteMap = new ConcurrentDictionary<string, Note>();
            ActivityLogger = activityLogger;
        }

        public bool TryGet(string noteName, out Note note)
        {
            var hasNote = NoteMap.TryGetValue(noteName, out var value);

            ActivityLogger?.Log(new ImplActivity
            {
                EntitySubject = NoteEntity,
                Event = GetNote,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(NoteName, noteName))
            .With(ActivityParam.New(IsNotePresent, hasNote))
            .With(ActivityParam.New(Note, value))
            .WithCallerInfo());

            note = value;
            return hasNote;
        }

        public Note Add(Note note)
        {
            NoteMap.TryAdd(note.UniqueHeader, note);

            ActivityLogger?.Log(new ImplActivity
            {
                EntitySubject = NoteEntity,
                Event = AddNote,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(NoteName, note.UniqueHeader))
            .With(ActivityParam.New(Note, note))
            .WithCallerInfo());

            return note;
        }

        public bool HasNote(Note note)
        {
            return NoteMap.ContainsKey(note.UniqueHeader);
        }

        public IEnumerable<string> GetAllHeaders()
        {
            return NoteMap.Keys;
        }
    }
}
