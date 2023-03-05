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

using Coracle.Raft.Engine.Configuration.Alterations;
using Coracle.Raft.Engine.Snapshots;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.States.LeaderEntities
{
    internal interface ILeaderVolatileProperties : IMembershipUpdate
    {
        bool AreMajorityOfServersHavingEntriesUpUntilIndexReplicated(long index);
        Task DecrementNextIndex(string externalServerId, long followerConflictTerm, long followerFirstIndexOfConflictingTerm);
        Task DecrementNextIndex(string externalServerId);
        void Initialize();
        bool TryGetMatchIndex(string externalServerId, out long matchIndex);
        bool TryGetNextIndex(string externalServerId, out long nextIndex);
        void UpdateIndices(string externalServerId, long maxIndexReplicated);
        Task<IDictionary<string, ISnapshotHeader>> RequiresSnapshot();
    }
}
