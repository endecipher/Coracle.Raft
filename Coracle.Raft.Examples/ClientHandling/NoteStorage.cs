#region License
// Copyright (c) 2023 Ayan Choudhury
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion

using System.Collections.Concurrent;
using System.Text;
using ActivityLogger.Logging;
using Coracle.Raft.Examples.Logging;
using Newtonsoft.Json;

namespace Coracle.Raft.Examples.ClientHandling
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
