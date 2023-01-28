using System.Collections.Concurrent;
using ActivityLogger.Logging;
using Coracle.Samples.Logging;

namespace Coracle.Samples.ClientHandling.Notes
{
    public class Notes : INotes
    {
        #region Constants

        public const string NoteEntity = nameof(Notes);
        public const string NoteName = nameof(NoteName);
        public const string Note = nameof(Note);
        public const string IsNotePresent = nameof(IsNotePresent);
        public const string GetNote = nameof(GetNote);
        public const string AddNote = nameof(AddNote);

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
