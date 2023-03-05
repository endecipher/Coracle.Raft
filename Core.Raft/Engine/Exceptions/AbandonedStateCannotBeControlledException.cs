using System;

namespace Coracle.Raft.Engine.Exceptions
{
    /// <summary>
    /// Thrown when a <see cref="States.AbstractState"/> is abandoned, and is tried to be started/paused/controlled etc.
    /// </summary>
    public class AbandonedStateCannotBeControlledException : InvalidOperationException
    {
        public AbandonedStateCannotBeControlledException(string message) : base($"An abandoned state cannot undergo normal operations. {message}")
        {
                
        }

        public static AbandonedStateCannotBeControlledException New()
        {
            return new AbandonedStateCannotBeControlledException(string.Empty);
        }
    }
}
