using System;
using System.Collections.Generic;
using System.Linq;

namespace Coracle.Raft.Engine.Node.Validations
{
    internal class EngineValidator : IEngineValidator
    {
        public bool IsValid(IEngineConfiguration configuration, out IEnumerable<string> errors)
        {
            var messages = new List<string>();

            void Add(string _)
            {
                messages.Add(string.Concat($"{nameof(IEngineConfiguration)} error: ", _));
            }

            if (string.IsNullOrWhiteSpace(configuration.NodeId) || string.IsNullOrWhiteSpace(configuration.NodeUri.ToString()))
            {
                Add($"{nameof(configuration.NodeId)} or {nameof(configuration.NodeUri)} cannot be blank");
            }

            if (configuration.SnapshotThresholdSize < 5)
            {
                Add($"{nameof(configuration.SnapshotThresholdSize)} cannot be less than 5");
            }

            if (configuration.SnapshotBufferSizeFromLastEntry < 1)
            {
                Add($"{nameof(configuration.SnapshotThresholdSize)} cannot be zero or negative");
            }

            if (configuration.ProcessorQueueSize < 1)
            {
                Add($"{nameof(configuration.ProcessorQueueSize)} cannot be zero or negative");
            }

            foreach (var info in typeof(IEngineConfiguration)
                .GetProperties().Where(_ => _.Name.Contains("seconds", StringComparison.OrdinalIgnoreCase)))
            {
                var value = Convert.ToInt32(info.GetValue(configuration));

                if (value < 1)
                {
                    Add($"{info.Name} cannot be zero or negative");
                }
            }

            errors = messages;

            return !messages.Any();
        }
    }
}
