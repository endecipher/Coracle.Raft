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

using Coracle.Raft.Engine.Command;

namespace Coracle.Raft.Examples.ClientHandling
{
    public class NoteCommand : ICommand
    {
        public NoteCommand()
        {

        }

        public string UniqueId { get; set; } = Guid.NewGuid().ToString();

        public bool IsReadOnly { get; set; }

        public string Type { get; set; }

        public Note Data { get; set; }

        public static NoteCommand CreateAdd(Note data)
        {
            var command = new NoteCommand();

            command.IsReadOnly = false;
            command.Type = NoteStorage.AddNote;
            command.Data = data;

            return command;
        }

        public static NoteCommand CreateGet(Note data)
        {
            var command = new NoteCommand();

            command.IsReadOnly = true;
            command.Type = NoteStorage.GetNote;
            command.Data = data;

            return command;
        }
    }
}
