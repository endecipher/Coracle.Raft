using System;

namespace Coracle.Raft.Engine.Exceptions
{
    public class InvalidStateConversionException : Exception
    {
        public string InvalidStateName { get; }
        public InvalidStateConversionException(string invalidStateName) : base($"Invalid new state value {invalidStateName}")
        {
            InvalidStateName = invalidStateName;
        }

        public static InvalidStateConversionException New(string invalidStateName)
        {
            return new InvalidStateConversionException(invalidStateName);
        }
    }
}
