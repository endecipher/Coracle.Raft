using System.Collections.Concurrent;
using System.Text;
using ActivityLogger.Logging;
using Coracle.Samples.Logging;
using Newtonsoft.Json;

namespace Coracle.Samples.ClientHandling
{
    public class NoteStorage : INoteStorage
    {
        #region Constants

        public const string NoteEntity = nameof(NoteStorage);
        public const string NoteName = nameof(NoteName);
        public const string Note = nameof(Note);
        public const string IsNotePresent = nameof(IsNotePresent);
        public const string GetNote = nameof(GetNote);
        public const string AddNote = nameof(AddNote);
        public const string ResetAndClear = nameof(ResetAndClear);
        public const string Rebuilt = nameof(Rebuilt);

        #endregion

        IActivityLogger ActivityLogger { get; }

        public ConcurrentDictionary<string, Note> NoteMap { get; private set; }

        public NoteStorage(IActivityLogger activityLogger)
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

        public void Reset()
        {
            NoteMap.Clear();

            ActivityLogger?.Log(new ImplActivity
            {
                EntitySubject = NoteEntity,
                Event = ResetAndClear,
                Level = ActivityLogLevel.Debug,

            }
            .WithCallerInfo());
        }

        public byte[] ExportData()
        {
            var export = JsonConvert.SerializeObject(NoteMap);
            byte[] bytes = Encoding.UTF8.GetBytes(export);
            return bytes;
        }

        public void Build(byte[] exportedData)
        {
            string data = Encoding.UTF8.GetString(exportedData);
            Reset();
            NoteMap = JsonConvert.DeserializeObject<ConcurrentDictionary<string, Note>>(data);

            ActivityLogger?.Log(new ImplActivity
            {
                EntitySubject = NoteEntity,
                Event = Rebuilt,
                Level = ActivityLogLevel.Debug,

            }
            .WithCallerInfo());
        }
    }
}
