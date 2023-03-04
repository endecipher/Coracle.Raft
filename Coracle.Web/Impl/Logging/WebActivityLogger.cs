using Microsoft.AspNetCore.SignalR;
using Coracle.Web.Hubs;
using ActivityLogger.Logging;
using CorrelationId.Abstractions;
using Newtonsoft.Json;
using Coracle.Raft.Engine.States.LeaderEntities;
using Coracle.Raft.Engine.States;
using Coracle.Raft.Engine.Configuration.Cluster;
using Coracle.Samples.Data;

namespace Coracle.Web.Impl.Logging
{
    public class CoracleProperty
    {
        public enum Property 
        {
            Term,
            CommitIndex,
            LastApplied,
            NextIndices,
            MatchIndices,
            VotedFor,
            State,
            LogChain,
            Cluster
        }

        internal Property Prop { get; set; }
        public string Name => Prop.ToString();
        public string Value { get; set; }
    }

    public class WebActivityLogger : IActivityLogger
    {
        public WebActivityLogger(ICorrelationContextAccessor correlationContext, IHubContext<LogHub> logHubContext, IHubContext<RaftHub> raftHubContext)
        {
            CorrelationContextAccessor = correlationContext;
            LogHubContext = logHubContext;
            RaftHubContext = raftHubContext;
        }

        public IHubContext<LogHub> LogHubContext { get; }
        public IHubContext<RaftHub> RaftHubContext { get; }
        public ActivityLogLevel Level { get; set; } = ActivityLogLevel.Debug;
        public ICorrelationContextAccessor CorrelationContextAccessor { get; }

        public void Log(ActivityLogger.Logging.Activity e)
        {
            if (!CanProceed(e.Level)) return;

            var activity = new
            {
                CorrelationContextAccessor?.CorrelationContext?.CorrelationId,
                Activity = e
            };

            string message = JsonConvert.SerializeObject(activity, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            LogHubContext.Clients.All.SendAsync(LogHub.ReceiveLog, message);

            foreach (var prop in Frame(e))
            {
                RaftHubContext.Clients.All.SendAsync(RaftHub.ReceiveEntries, prop);
            }
        }

        public bool CanProceed(ActivityLogLevel level)
        {
            return (int)level >= (int)Level;
        }


        #region Hub Frames

        public IEnumerable<CoracleProperty> Frame(Activity e)
        {
            var list = new List<CoracleProperty>();

            switch (e.EntitySubject)
            {
                case SampleVolatileStateHandler.Entity:
                    {
                        list.Add(new CoracleProperty
                        {
                            Prop = CoracleProperty.Property.Term,
                            Value = e.Parameters.First(_ => _.Name.Equals(SampleVolatileStateHandler.CurrentTermValue)).Value
                        });

                        list.Add(new CoracleProperty
                        {
                            Prop = CoracleProperty.Property.VotedFor,
                            Value = e.Parameters.First(_ => _.Name.Equals(SampleVolatileStateHandler.VotedForValue)).Value
                        });
                    }
                    break;

                case SampleVolatileStateHandler.EntityLog:
                    {
                        list.Add(new CoracleProperty
                        {
                            Prop = CoracleProperty.Property.LogChain,
                            Value = e.Parameters.First(_ => _.Name.Equals(SampleVolatileStateHandler.logChain)).Value
                        });
                    }
                    break;

                case VolatileProperties.Entity:
                    {
                        list.Add(new CoracleProperty
                        {
                            Prop = CoracleProperty.Property.CommitIndex,
                            Value = e.Parameters.First(_ => _.Name.Equals(VolatileProperties.commitIndex)).Value
                        });

                        list.Add(new CoracleProperty
                        {
                            Prop = CoracleProperty.Property.LastApplied,
                            Value = e.Parameters.First(_ => _.Name.Equals(VolatileProperties.lastApplied)).Value
                        });
                    }
                    break;

                case LeaderVolatileProperties.Entity:
                    {
                        var eventColl = new HashSet<string>
                        {
                            LeaderVolatileProperties.DecrementedNextIndex,
                            LeaderVolatileProperties.DecrementedNextIndexToFirstIndexOfConflictingTerm,
                            LeaderVolatileProperties.DecrementedNextIndexToFirstIndexOfLeaderTermCorrespondingToConflictingIndexEntry,
                            LeaderVolatileProperties.DecrementedNextIndexToFirstIndexOfPriorValidTerm,
                            LeaderVolatileProperties.UpdatedIndices
                        };

                        if (eventColl.Contains(e.Event))
                        {
                            string nodeId = e.Parameters.First(_ => _.Name.Equals(LeaderVolatileProperties.nodeId)).Value;
                            string newNextIndex = e.Parameters.First(_ => _.Name.Equals(LeaderVolatileProperties.newNextIndex)).Value;

                            list.Add(new CoracleProperty
                            {
                                Prop = CoracleProperty.Property.NextIndices,
                                Value = $"{nodeId} = {newNextIndex}"
                            });
                        }

                        if (e.Event.Equals(LeaderVolatileProperties.UpdatedIndices))
                        {
                            string nodeId = e.Parameters.First(_ => _.Name.Equals(LeaderVolatileProperties.nodeId)).Value;
                            string newMatchIndex = e.Parameters.First(_ => _.Name.Equals(LeaderVolatileProperties.newMatchIndex)).Value;

                            list.Add(new CoracleProperty
                            {
                                Prop = CoracleProperty.Property.MatchIndices,
                                Value = $"{nodeId} = {newMatchIndex}"
                            });
                        }
                    }
                    break;

                case CurrentStateAccessor.Entity:
                    {
                        list.Add(new CoracleProperty
                        {
                            Prop = CoracleProperty.Property.State,
                            Value = e.Parameters.First(_ => _.Name.Equals(CurrentStateAccessor.newState)).Value
                        });
                    }
                    break;

                case AbstractState.Entity:
                    {
                        if (e.Event.Equals(AbstractState.Stopping) || e.Event.Equals(AbstractState.Resuming) || e.Event.Equals(AbstractState.Decommissioning))
                        {
                            list.Add(new CoracleProperty
                            {
                                Prop = CoracleProperty.Property.State,
                                Value = e.Parameters.First(_ => _.Name.Equals(AbstractState.newState)).Value
                            });
                        }
                    }
                    break;

                case ClusterConfiguration.Entity:
                    {
                        if (e.Event.Equals(ClusterConfiguration.NewUpdate))
                        {
                            list.Add(new CoracleProperty
                            {
                                Prop = CoracleProperty.Property.Cluster,
                                Value = e.Parameters.First(_ => _.Name.Equals(ClusterConfiguration.allNodeIds)).Value
                            });
                        }
                    }
                    break;
            }

            return list;
        }

        #endregion
    }
}
