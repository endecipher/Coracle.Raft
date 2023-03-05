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
