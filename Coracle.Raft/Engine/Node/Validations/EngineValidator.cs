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
