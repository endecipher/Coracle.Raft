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

using Coracle.Raft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskGuidance.BackgroundProcessing.Dependencies;

namespace Coracle.Raft.Tests.Framework
{
    internal static class ServiceContainer
    {
        public static ServiceProvider Provider { get; set; }
        public static IServiceCollection ServiceDescriptors { get; set; }

        public static void Configure()
        {
            var serviceCollection = new ServiceCollection()
                .AddLogging(l => l.AddConsole())
                .AddOptions()
                .Configure<LoggerFilterOptions>(c => c.MinLevel = LogLevel.Trace);

            var dotNetContainer = new DotNetDependencyContainer(serviceCollection);

            /// TaskGuidance.BackgroundProcessing Registration
            new GuidanceDependencyRegistration().Register(dotNetContainer);

            /// Coracle.Raft Registration
            new Registration().Register(dotNetContainer);

            /// Test Extensions Registration
            new TestRegistration().Register(dotNetContainer);

            ServiceDescriptors = serviceCollection;
        }

        public static void Build()
        {
            Provider = ServiceDescriptors.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true
            });
        }

        public static void Remove<T>()
        {
            var descriptor = ServiceDescriptors.SingleOrDefault(d => d.ServiceType == typeof(T));

            if (descriptor != null)
                ServiceDescriptors.Remove(descriptor);
        }
    }
}
