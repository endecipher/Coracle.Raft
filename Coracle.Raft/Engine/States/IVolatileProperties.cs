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

using ActivityLogger.Logging;
using Coracle.Raft.Engine.ActivityLogger;
using Coracle.Raft.Engine.Snapshots;

namespace Coracle.Raft.Engine.States
{
    internal interface IVolatileProperties
    {
        long CommitIndex { get; set; }
        long LastApplied { get; set; }
        void OnSuccessfulSnapshotInstallation(ISnapshotHeader snapshot);
    }

    namespace Volatile 
    {
        public class ActivityConstants
        {
            #region Constants
            public const string commitIndex = nameof(commitIndex);
            public const string lastApplied = nameof(lastApplied);
            public const string Entity = nameof(VolatileProperties);
            public const string VolatileChange = nameof(VolatileChange);
            #endregion
        }
    }

    internal class VolatileProperties : IVolatileProperties
    {
        public VolatileProperties(IActivityLogger activityLogger)
        {
            ActivityLogger = activityLogger;
        }

        private long _commitIndex = 0;

        public long CommitIndex
        {
            get
            {
                return _commitIndex;
            }

            set
            {
                _commitIndex = value;

                Snap();
            }
        }


        private long _lastApplied = 0;

        public long LastApplied
        {
            get
            {
                return _lastApplied;
            }

            set
            {
                _lastApplied = value;

                Snap();
            }
        }

        public IActivityLogger ActivityLogger { get; }

        private void Snap()
        {
            ActivityLogger?.Log(new CoracleActivity
            {
                EntitySubject = Volatile.ActivityConstants.Entity,
                Event = Volatile.ActivityConstants.VolatileChange,
                Level = ActivityLogLevel.Verbose
            }
            .With(ActivityParam.New(Volatile.ActivityConstants.lastApplied, _lastApplied))
            .With(ActivityParam.New(Volatile.ActivityConstants.commitIndex, _commitIndex))
            .WithCallerInfo());
        }

        public void OnSuccessfulSnapshotInstallation(ISnapshotHeader snapshot)
        {
            CommitIndex = snapshot.LastIncludedIndex;
            LastApplied = snapshot.LastIncludedIndex;
        }
    }
}
