using ActivityLogger.Logging;
using Coracle.IntegrationTests.Components.Logging;
using Coracle.IntegrationTests.Components.Registries;
using Core.Raft.Canoe.Engine.Configuration.Cluster;
using Core.Raft.Canoe.Engine.Remoting;
using EventGuidance.Dependency;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Coracle.IntegrationTests.Framework
{
    //public class AssertionLogger
    //{
    //    private object _lock = new object();

    //    public Queue<Activity> Activities { get; set; } = new Queue<Activity>();

    //    public void Enqueue(Activity e)
    //    {
    //        lock (_lock)
    //        {
    //            Activities.Enqueue(e);
    //        }
    //    }
    //}
    //public interface INodeContext
    //{
    //    AssertionLogger AssertionLogger { get; set; }
    //    WebApplicationFactory<Web.Discovery.Startup> GetDiscoveryNode();
    //    WebApplicationFactory<Web.Startup> GetServerNode(string id);
    //}
    //public class NodeContext : INodeContext
    //{
    //    public NodeContext()
    //    {
    //    }

    //    private WebApplicationFactory<Coracle.Web.Discovery.Startup> DiscoveryNode { get; set; }

    //    public AssertionLogger AssertionLogger { get; set; } = new AssertionLogger();

    //    public WebApplicationFactory<Coracle.Web.Discovery.Startup> GetDiscoveryNode()
    //    {
    //        return DiscoveryNode;
    //    }

    //    private ConcurrentDictionary<string, WebApplicationFactory<Coracle.Web.Startup>> ServerNodes { get; set; } = new ConcurrentDictionary<string, WebApplicationFactory<Startup>>();

    //    public WebApplicationFactory<Coracle.Web.Startup> GetServerNode(string nodeId)
    //    {
    //        return !string.IsNullOrWhiteSpace(nodeId) ? ServerNodes[nodeId] : ServerNodes.First().Value;
    //    }

    //    private string Node(long value) => StringExtensions.Node(value);

    //    public void Setup(params string[] ids)
    //    {
    //        SetupDiscoveryNode();

    //        for (int i = 0; i < ids.Length; i++)
    //        {
    //            SetupServerNode(ids[i]);
    //        }
    //    }

    //    private void SetupDiscoveryNode()
    //    {
    //        DiscoveryNode = new WebApplicationFactory<Web.Discovery.Startup>()
    //            .WithWebHostBuilder(builder =>
    //            {
    //                //builder.ConfigureServices(services =>
    //                //{
    //                //    services.Configure<EngineConfigurationSettings>(options =>
    //                //    {
    //                //        options.DiscoveryServerUri = DiscoveryNode.Server.BaseAddress;
    //                //        options.NodeId = NodeIdString(id);
    //                //    });
    //                //});

    //                builder.UseTestServer(options =>
    //                {
    //                    //options.BaseAddress = new Uri("https://localhost:7016");
    //                });

    //                builder.ConfigureTestServices(services =>
    //                {
    //                    // remove the existing context configuration
    //                    //var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IRemoteManager));
    //                    //if (descriptor != null)
    //                    //    services.Remove(descriptor);

    //                    //services.AddSingleton<ITestRemoteManager, TestRemoteManager>(provider =>
    //                    //{
    //                    //    return Context.TestRemoteManager;
    //                    //});


    //                    services.Configure<DiscoveryLoggerOptions>(options =>
    //                    {
    //                        options.ConfigureHandler = true;
    //                        options.HandlerAction = AssertionLogger.Enqueue;
    //                    });

    //                });
    //            });
    //    }

    //    private void SetupServerNode(string id)
    //    {
    //        ServerNodes.TryAdd(id, new WebApplicationFactory<Coracle.Web.Startup>()
    //            .WithWebHostBuilder(builder =>
    //            {
    //                builder.ConfigureServices(services =>
    //                {
    //                    services.Configure<EngineConfigurationSettings>(options =>
    //                    {
    //                        options.DiscoveryServerUri = DiscoveryNode.Server.BaseAddress;
    //                        options.NodeId = id;
    //                    });
    //                });

    //                builder.UseTestServer(options =>
    //                {
    //                    //options.BaseAddress = new Uri("https://localhost:7166");
    //                });

    //                builder.ConfigureTestServices(services =>
    //                {
    //                    // remove the existing context configuration
    //                    //var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IRemoteManager));
    //                    //if (descriptor != null)
    //                    //    services.Remove(descriptor);

    //                    //services.Add(ServiceDescriptor.Singleton<IRemoteManager>(new TestRemoteManager(this)));

    //                    services.Configure<ContainerRegistryOptions>(options =>
    //                    {
    //                        options.ConfigureHandler = true;
    //                        options.HandlerAction = (container) =>
    //                        {
    //                            container.EjectAllInstancesOf<IRemoteManager>();
    //                            container.InjectInstanceFor<IRemoteManager>(new TestRemoteManager(this));
    //                        };
    //                    });

    //                    services.Configure<TestActivityLoggerOptions>(options =>
    //                    {
    //                        options.ConfigureHandler = true;
    //                        options.HandlerAction = AssertionLogger.Enqueue;
    //                    });
    //                });
    //            })
    //        );
    //    }
    //}

    //public class TestContext : ITestContext, IDisposable
    //{
    //    public NodeContext NodeContext { get; }

    //    public TestContext()
    //    {
    //        NodeContext = new NodeContext();
    //    }

    //    public void Dispose()
    //    {

    //    }
    //}

    //public interface ITestContext
    //{
    //    NodeContext NodeContext { get; }
    //}
}
