using EventGuidance.Logging;
using Coracle.Web.Logging;
using System.Collections.Concurrent;
using ActivityLogger.Logging;
using EventGuidance.Dependency;
using Coracle.Web.Coracle.Logging;

namespace Coracle.Web.ClientHandling.Notes
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

        IActivityLogger ActivityLogger => ComponentContainer.Instance.GetInstance<IActivityLogger>();

        public ConcurrentDictionary<string, Note> NoteMap { get; }

        public Notes()
        {
            NoteMap = new ConcurrentDictionary<string, Note>();
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
    }
}
