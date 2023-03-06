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

namespace Coracle.Raft.Engine.States
{
    namespace Current
    {
        public class CurrentAcessorActivityConstants
        {
            #region Constants
            public const string Entity = nameof(CurrentStateAccessor);
            public const string StateChange = nameof(StateChange);
            public const string newState = nameof(newState);
            #endregion
        }
    }

    internal class CurrentStateAccessor : ICurrentStateAccessor
    {
        public IActivityLogger ActivityLogger { get; }

        public CurrentStateAccessor(IActivityLogger activityLogger)
        {
            ActivityLogger = activityLogger;
        }


        #region Node State Holder

        private object _lock = new object();
        IStateDevelopment _state = null;

        IStateDevelopment State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;

                ActivityLogger?.Log(new CoracleActivity
                {
                    Description = $"State changed to {value.StateValue}",
                    EntitySubject = Current.CurrentAcessorActivityConstants.Entity,
                    Event = Current.CurrentAcessorActivityConstants.StateChange,
                    Level = ActivityLogLevel.Information,

                }
                .With(ActivityParam.New(Current.CurrentAcessorActivityConstants.newState, value.StateValue.ToString()))
                .WithCallerInfo());
            }
        }

        public IStateDevelopment Get()
        {
            lock (_lock)
            {
                return State;
            }
        }

        public void UpdateWith(IStateDevelopment changingState)
        {
            lock (_lock)
            {
                State = changingState;
            }
        }

        #endregion
    }
}
