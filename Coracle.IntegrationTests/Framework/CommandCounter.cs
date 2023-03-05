namespace Coracle.Raft.Tests.Framework
{
    public interface ICommandCounter
    {
        int NextCommandCounter { get; }

        int GetLatestCommandCounter { get; }
    }

    public class CommandCounter : ICommandCounter
    {
        private int _commandCounter = 0;
        private int _latestCommandCounter = 0;

        public int NextCommandCounter
        {
            get
            {
                _commandCounter++;
                _latestCommandCounter = _commandCounter;
                return _commandCounter;
            }
        }

        public int GetLatestCommandCounter
        {
            get
            {
                return _latestCommandCounter;
            }
        }
    }
}
