using Coracle.Web.Hubs;
using CorrelationId.DependencyInjection;
using CorrelationId;
using Coracle.Raft.Engine.Remoting;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Raft.Engine.ClientHandling;
using Coracle.Raft.Engine.States;
using TaskGuidance.BackgroundProcessing.Core;
using ActivityLogger.Logging;
using CorrelationId.Abstractions;
using Coracle.Raft.Engine.Discovery;
using Coracle.Samples.ClientHandling.Notes;
using Coracle.Samples.ClientHandling;
using Coracle.IntegrationTests.Components.PersistentData;
using Coracle.Web.Controllers;
using Coracle.Web.Impl.Logging;
using Coracle.Web.Impl.Discovery;
using Coracle.Web.Impl.Node;
using Coracle.Web.Impl.Remoting;
using Coracle.Web.Impl.Configuration;
using TaskGuidance.BackgroundProcessing.Dependencies;

namespace Coracle.Web
{
    public sealed class EngineConfigurationOptions : IEngineConfiguration
    {
        public Uri DiscoveryServerUri { get; set; }
        public int ProcessorQueueSize { get; set; }
        public int ProcessorWaitTimeWhenQueueEmpty_InMilliseconds { get; set; }
        public string NodeId { get; set; }
        public int SendAppendEntriesRPC_MaxRetryInfinityCounter { get; set; }
        public int SendRequestVoteRPC_MaxRetryInfinityCounter { get; set; }
        public int SendAppendEntriesRPC_MaxSessionCapacity { get; set; }
        public bool IncludeOriginalClientCommandInResults { get; set; }
        public bool IncludeOriginalConfigurationInResults { get; set; }
        public bool IncludeJointConsensusConfigurationInResults { get; set; }
        public bool IncludeConfigurationChangeRequestInResults { get; set; }
        public int WaitPostEnroll_InMilliseconds { get; set; }
        public int MaxElectionTimeout_InMilliseconds { get; set; }
        public int MinElectionTimeout_InMilliseconds { get; set; }
        public int HeartbeatInterval_InMilliseconds { get; set; }
        public int NoLeaderElectedWaitInterval_InMilliseconds { get; set; }
        public int ClientCommandTimeout_InMilliseconds { get; set; }
        public int AppendEntriesTimeoutOnReceive_InMilliseconds { get; set; }
        public int CatchupIntervalOnConfigurationChange_InMilliseconds { get; set; }
        public int RequestVoteTimeoutOnReceive_InMilliseconds { get; set; }
        public int RequestVoteTimeoutOnSend_InMilliseconds { get; set; }
        public int AppendEntriesTimeoutOnSend_InMilliseconds { get; set; }
        public Uri ThisNodeUri { get; set; }
        public int EntryCommitWaitTimeout_InMilliseconds { get; set; }
        public int EntryCommitWaitInterval_InMilliseconds { get; set; }
        public int CatchUpOfNewNodesTimeout_InMilliseconds { get; set; }
        public int CatchUpOfNewNodesWaitInterval_InMilliseconds { get; set; }
        public int CheckDepositionWaitInterval_InMilliseconds { get; set; }

        public void ApplyFrom(IEngineConfiguration newConfig)
        {
            //TODO: Check all covered
            DiscoveryServerUri = newConfig.DiscoveryServerUri;
            ProcessorQueueSize = newConfig.ProcessorQueueSize;
            ProcessorWaitTimeWhenQueueEmpty_InMilliseconds = newConfig.ProcessorWaitTimeWhenQueueEmpty_InMilliseconds;
            NodeId = newConfig.NodeId;
            SendAppendEntriesRPC_MaxRetryInfinityCounter = newConfig.SendAppendEntriesRPC_MaxRetryInfinityCounter;
            SendRequestVoteRPC_MaxRetryInfinityCounter = newConfig.SendRequestVoteRPC_MaxRetryInfinityCounter;
            SendAppendEntriesRPC_MaxSessionCapacity = newConfig.SendAppendEntriesRPC_MaxSessionCapacity;
            IncludeOriginalClientCommandInResults = newConfig.IncludeOriginalClientCommandInResults;
            IncludeOriginalConfigurationInResults = newConfig.IncludeOriginalConfigurationInResults;
            IncludeJointConsensusConfigurationInResults = newConfig.IncludeJointConsensusConfigurationInResults;
            IncludeConfigurationChangeRequestInResults = newConfig.IncludeConfigurationChangeRequestInResults;
            WaitPostEnroll_InMilliseconds = newConfig.WaitPostEnroll_InMilliseconds;
            MaxElectionTimeout_InMilliseconds = newConfig.MaxElectionTimeout_InMilliseconds;
            MinElectionTimeout_InMilliseconds = newConfig.MinElectionTimeout_InMilliseconds;
            HeartbeatInterval_InMilliseconds = newConfig.HeartbeatInterval_InMilliseconds;
            NoLeaderElectedWaitInterval_InMilliseconds = newConfig.NoLeaderElectedWaitInterval_InMilliseconds;
            ClientCommandTimeout_InMilliseconds = newConfig.ClientCommandTimeout_InMilliseconds;
            AppendEntriesTimeoutOnReceive_InMilliseconds = newConfig.AppendEntriesTimeoutOnReceive_InMilliseconds;
            CatchupIntervalOnConfigurationChange_InMilliseconds = newConfig.CatchupIntervalOnConfigurationChange_InMilliseconds;
            RequestVoteTimeoutOnReceive_InMilliseconds = newConfig.RequestVoteTimeoutOnReceive_InMilliseconds;
            RequestVoteTimeoutOnSend_InMilliseconds = newConfig.RequestVoteTimeoutOnSend_InMilliseconds;
            AppendEntriesTimeoutOnSend_InMilliseconds = newConfig.AppendEntriesTimeoutOnSend_InMilliseconds;
            ThisNodeUri = newConfig.ThisNodeUri;
            EntryCommitWaitTimeout_InMilliseconds = newConfig.EntryCommitWaitTimeout_InMilliseconds;
            EntryCommitWaitInterval_InMilliseconds = newConfig.EntryCommitWaitInterval_InMilliseconds;
            CatchUpOfNewNodesTimeout_InMilliseconds = newConfig.CatchUpOfNewNodesTimeout_InMilliseconds;
            CatchUpOfNewNodesWaitInterval_InMilliseconds = newConfig.CatchUpOfNewNodesWaitInterval_InMilliseconds;
            CheckDepositionWaitInterval_InMilliseconds = newConfig.CheckDepositionWaitInterval_InMilliseconds;

        }
    }

    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddHttpContextAccessor();
            services.AddControllers();
            services.AddOptions();

            IConfigurationSection config = Configuration.GetSection("Coracle:EngineConfiguration");
            services.Configure<EngineConfigurationOptions>(config);

            services.Configure<ActivityLoggerOptions>(options =>
            {
                options.ConfigureHandler = false;
            });

            new GuidanceDependencyRegistration().Register(new DotNetDependencyContainer(services));
            new Raft.Dependencies.CoracleDependencyRegistration().Register(new DotNetDependencyContainer(services));

            services.AddSingleton<INotes, Notes>();
            services.AddSingleton<IRemoteManager, HttpRemoteManager>();
            services.AddSingleton<IClientRequestHandler, TestClientRequestHandler>();
            services.AddSingleton<IPersistentProperties, TestStateProperties>();
            services.AddSingleton<ITaskProcessorConfiguration, TaskProcessorConfiguration>();
            services.AddSingleton<IActivityLogger, WebActivityLogger>();
            services.AddTransient<ICorrelationContextAccessor, CorrelationContextAccessor>();
            services.AddTransient<ICoracleClient, CoracleClient>();
            services.AddSingleton<IDiscoverer, HttpDiscoverer>();
            services.AddSingleton<ICoracleNodeAccessor, CoracleNodeAccessor>();
            services.AddSingleton<IAppInfo, AppInfo>();
            services.AddSingleton<IActivityLogger, WebActivityLogger>();
            services.AddOptions<SettingOptions>();

            services.AddSignalR();
            
            services.AddDefaultCorrelationId(options =>
            {
                options.IgnoreRequestHeader = true;
                options.EnforceHeader = false;
            });

            services.AddMvc().AddRazorRuntimeCompilation();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseHttpsRedirection();
            
            app.UseStaticFiles();

            app.UseCorrelationId();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<LogHub>("/logHub");
                endpoints.MapHub<RaftHub>("/raftHub");

                endpoints.MapDefaultControllerRoute();
            });
        }


        public class DotNetDependencyContainer : IDependencyContainer
        {
            public DotNetDependencyContainer(IServiceCollection serviceDescriptors)
            {
                ServiceDescriptors = serviceDescriptors;
            }

            public IServiceCollection ServiceDescriptors { get; }

            void IDependencyContainer.RegisterSingleton<T1, T2>()
            {
                ServiceDescriptors.AddSingleton<T1, T2>();
            }

            void IDependencyContainer.RegisterTransient<T1, T2>()
            {
                ServiceDescriptors.AddTransient<T1, T2>();
            }
        }
    }
}
