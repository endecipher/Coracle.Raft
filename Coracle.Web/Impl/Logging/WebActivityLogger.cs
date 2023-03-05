using Microsoft.AspNetCore.SignalR;
using Coracle.Web.Hubs;
using ActivityLogger.Logging;
using CorrelationId.Abstractions;
using Newtonsoft.Json;
using Coracle.Raft.Examples.Data;

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

                case Raft.Engine.States.Volatile.ActivityConstants.Entity:
                    {
                        list.Add(new CoracleProperty
                        {
                            Prop = CoracleProperty.Property.CommitIndex,
                            Value = e.Parameters.First(_ => _.Name.Equals(Raft.Engine.States.Volatile.ActivityConstants.commitIndex)).Value
                        });

                        list.Add(new CoracleProperty
                        {
                            Prop = CoracleProperty.Property.LastApplied,
                            Value = e.Parameters.First(_ => _.Name.Equals(Raft.Engine.States.Volatile.ActivityConstants.lastApplied)).Value
                        });
                    }
                    break;

                case Raft.Engine.States.LeaderEntities.LeaderVolatileActivityConstants.Entity:
                    {
                        var eventColl = new HashSet<string>
                        {
                            Raft.Engine.States.LeaderEntities.LeaderVolatileActivityConstants.DecrementedNextIndex,
                            Raft.Engine.States.LeaderEntities.LeaderVolatileActivityConstants.DecrementedNextIndexToFirstIndexOfConflictingTerm,
                            Raft.Engine.States.LeaderEntities.LeaderVolatileActivityConstants.DecrementedNextIndexToFirstIndexOfLeaderTermCorrespondingToConflictingIndexEntry,
                            Raft.Engine.States.LeaderEntities.LeaderVolatileActivityConstants.DecrementedNextIndexToFirstIndexOfPriorValidTerm,
                            Raft.Engine.States.LeaderEntities.LeaderVolatileActivityConstants.UpdatedIndices
                        };

                        if (eventColl.Contains(e.Event))
                        {
                            string nodeId = e.Parameters.First(_ => _.Name.Equals(Raft.Engine.States.LeaderEntities.LeaderVolatileActivityConstants.nodeId)).Value;
                            string newNextIndex = e.Parameters.First(_ => _.Name.Equals(Raft.Engine.States.LeaderEntities.LeaderVolatileActivityConstants.newNextIndex)).Value;

                            list.Add(new CoracleProperty
                            {
                                Prop = CoracleProperty.Property.NextIndices,
                                Value = $"{nodeId} = {newNextIndex}"
                            });
                        }

                        if (e.Event.Equals(Raft.Engine.States.LeaderEntities.LeaderVolatileActivityConstants.UpdatedIndices))
                        {
                            string nodeId = e.Parameters.First(_ => _.Name.Equals(Raft.Engine.States.LeaderEntities.LeaderVolatileActivityConstants.nodeId)).Value;
                            string newMatchIndex = e.Parameters.First(_ => _.Name.Equals(Raft.Engine.States.LeaderEntities.LeaderVolatileActivityConstants.newMatchIndex)).Value;

                            list.Add(new CoracleProperty
                            {
                                Prop = CoracleProperty.Property.MatchIndices,
                                Value = $"{nodeId} = {newMatchIndex}"
                            });
                        }
                    }
                    break;

                case Raft.Engine.States.Current.CurrentAcessorActivityConstants.Entity:
                    {
                        list.Add(new CoracleProperty
                        {
                            Prop = CoracleProperty.Property.State,
                            Value = e.Parameters.First(_ => _.Name.Equals(Raft.Engine.States.Current.CurrentAcessorActivityConstants.newState)).Value
                        });
                    }
                    break;

                case Raft.Engine.States.AbstractStateActivityConstants.Entity:
                    {
                        if (e.Event.Equals(Raft.Engine.States.AbstractStateActivityConstants.Stopping) || e.Event.Equals(Raft.Engine.States.AbstractStateActivityConstants.Resuming) || e.Event.Equals(Raft.Engine.States.AbstractStateActivityConstants.Decommissioning))
                        {
                            list.Add(new CoracleProperty
                            {
                                Prop = CoracleProperty.Property.State,
                                Value = e.Parameters.First(_ => _.Name.Equals(Raft.Engine.States.AbstractStateActivityConstants.newState)).Value
                            });
                        }
                    }
                    break;

                case Raft.Engine.Configuration.Cluster.ActivityConstants.Entity:
                    {
                        if (e.Event.Equals(Raft.Engine.Configuration.Cluster.ActivityConstants.NewUpdate))
                        {
                            list.Add(new CoracleProperty
                            {
                                Prop = CoracleProperty.Property.Cluster,
                                Value = e.Parameters.First(_ => _.Name.Equals(Raft.Engine.Configuration.Cluster.ActivityConstants.allNodeIds)).Value
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
