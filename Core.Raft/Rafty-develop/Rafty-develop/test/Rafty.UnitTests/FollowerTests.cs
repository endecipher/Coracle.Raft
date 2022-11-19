using System.Diagnostics;
using System.Threading;
using Castle.Components.DictionaryAdapter;
using static Rafty.Infrastructure.Wait;

namespace Rafty.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Concensus;
    using Concensus.Messages;
    using Concensus.Node;
    using Concensus.Peers;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Rafty.Concensus.States;
    using Rafty.FiniteStateMachine;
    using Rafty.Infrastructure;
    using Rafty.Log;
    using Shouldly;
    using Xunit;

    public class FollowerTests 
    {
        private readonly IFiniteStateMachine _fsm;
        private List<IPeer> _peers;
        private readonly ILog _log;
        private readonly IRandomDelay _random;
        private INode _node;
        private CurrentState _currentState;
        private InMemorySettings _settings;
        private IRules _rules;
        private IPeersProvider _peersProvider;
        private Mock<ILoggerFactory> _loggerFactory;
        private Mock<ILogger> _logger;

        public FollowerTests()
        {
            _logger = new Mock<ILogger>();
            _loggerFactory = new Mock<ILoggerFactory>();
            _loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_logger.Object);
            _rules = new Rules(_loggerFactory.Object, new NodeId(default(string)));
            _settings = new InMemorySettingsBuilder().Build();
            _random = new RandomDelay();
            _log = new InMemoryLog();
            _peers = new List<IPeer>();
            _fsm = new InMemoryStateMachine();
            _peersProvider = new InMemoryPeersProvider(_peers);
            _currentState = new CurrentState(Guid.NewGuid().ToString(), 0, default(string), -1, -1, default(string));
            _node = new NothingNode();
        }

        [Fact]
        public void CommitIndexShouldBeInitialisedToMinusOne()
        {
            _node = new Node(_fsm, _log, _settings, _peersProvider, _loggerFactory.Object);
            _node.Start(new NodeId(Guid.NewGuid().ToString()));
            _node.State.CurrentState.CommitIndex.ShouldBe(0);
        }

        [Fact]
        public void CurrentTermShouldBeInitialisedToZero()
        {
            _node = new Node(_fsm, _log, _settings, _peersProvider, _loggerFactory.Object);
            _node.Start(new NodeId(Guid.NewGuid().ToString()));
            _node.State.CurrentState.CurrentTerm.ShouldBe(0);
        }

        [Fact]
        public void LastAppliedShouldBeInitialisedToZero()
        {
            _node = new Node(_fsm, _log, _settings, _peersProvider, _loggerFactory.Object);
            _node.Start(new NodeId(Guid.NewGuid().ToString()));
            _node.State.CurrentState.LastApplied.ShouldBe(0);
        }

        [Fact]
        public void ShouldBecomeCandidateWhenFollowerReceivesTimeoutAndHasNotHeardFromLeader()
        {
            _node = new TestingNode();
            var node = (TestingNode)_node;
            node.SetState(new Follower(_currentState, _fsm, _log, _random, node, new InMemorySettingsBuilder().WithMinTimeout(0).WithMaxTimeout(0).Build(),_rules, _peers, _loggerFactory.Object));
            var result = WaitFor(1000).Until(() => node.BecomeCandidateCount > 0);
            result.ShouldBeTrue();
        }

        [Fact]
        public void ShouldBecomeCandidateWhenFollowerReceivesTimeoutAndHasNotHeardFromLeaderSinceLastTimeout()
        {
            _node = new TestingNode();
            var node = (TestingNode)_node;
            node.SetState(new Follower(_currentState, _fsm, _log, _random, node, new InMemorySettingsBuilder().WithMinTimeout(0).WithMaxTimeout(0).Build(), _rules, _peers, _loggerFactory.Object));
            _node.Handle(new AppendEntriesBuilder().WithTerm(1).WithLeaderCommitIndex(-1).Build());
            var result = WaitFor(1000).Until(() => node.BecomeCandidateCount > 0);
            result.ShouldBeTrue();
        }

        [Fact]
        public void ShouldNotBecomeCandidateWhenFollowerReceivesTimeoutAndHasHeardFromLeader()
        {
            _node = new Node(_fsm, _log, _settings, _peersProvider, _loggerFactory.Object);
            _node.Start(new NodeId(Guid.NewGuid().ToString()));
            _node.State.ShouldBeOfType<Follower>();
            _node.Handle(new AppendEntriesBuilder().WithTerm(1).WithLeaderCommitIndex(-1).Build());
            _node.State.ShouldBeOfType<Follower>();
        }

        [Fact]
        public void ShouldNotBecomeCandidateWhenFollowerReceivesTimeoutAndHasHeardFromLeaderSinceLastTimeout()
        {
            _node = new Node(_fsm, _log, _settings, _peersProvider, _loggerFactory.Object);
            _node.Start(new NodeId(Guid.NewGuid().ToString()));
            _node.State.ShouldBeOfType<Follower>();
            _node.Handle(new AppendEntriesBuilder().WithTerm(1).WithLeaderCommitIndex(-1).Build());
            _node.State.ShouldBeOfType<Follower>();
            _node.Handle(new AppendEntriesBuilder().WithTerm(1).WithLeaderCommitIndex(-1).Build());
            _node.State.ShouldBeOfType<Follower>();
        }

        [Fact]
        public void ShouldStartAsFollower()
        {
            _node = new Node(_fsm, _log, _settings, _peersProvider, _loggerFactory.Object);
            _node.Start(new NodeId(Guid.NewGuid().ToString()));
            _node.State.ShouldBeOfType<Follower>();
        }

        [Fact]
        public void VotedForShouldBeInitialisedToNone()
        {
            _node = new Node(_fsm, _log, _settings, _peersProvider, _loggerFactory.Object);
            _node.Start(new NodeId(Guid.NewGuid().ToString()));  
            _node.State.CurrentState.VotedFor.ShouldBe(default(string));
        }

        [Fact]
        public async Task ShouldUpdateVotedFor()
        {
            _node = new NothingNode();
            _currentState = new CurrentState(Guid.NewGuid().ToString(), 0, default(string), 0, 0, default(string));
            var follower = new Follower(_currentState, _fsm, _log, _random, _node, _settings, _rules, _peers, _loggerFactory.Object);
            var requestVote = new RequestVoteBuilder().WithCandidateId(Guid.NewGuid().ToString()).WithLastLogIndex(1).Build();
            var requestVoteResponse = await follower.Handle(requestVote);
            requestVoteResponse.VoteGranted.ShouldBeTrue();
            follower.CurrentState.VotedFor.ShouldBe(requestVote.CandidateId);
        }

        [Fact]
        public async Task ShouldVoteForNewCandidateInAnotherTermsElection()
        {
             _node = new NothingNode();
            _currentState = new CurrentState(Guid.NewGuid().ToString(), 0, default(string), 0, 0, default(string));
            var follower = new Follower(_currentState, _fsm, _log, _random, _node, _settings,_rules, _peers, _loggerFactory.Object);
            var requestVote = new RequestVoteBuilder().WithTerm(0).WithCandidateId(Guid.NewGuid().ToString()).WithLastLogIndex(1).Build();
            var requestVoteResponse = await follower.Handle(requestVote);
            follower.CurrentState.VotedFor.ShouldBe(requestVote.CandidateId);
            requestVoteResponse.VoteGranted.ShouldBeTrue();
            requestVote = new RequestVoteBuilder().WithTerm(1).WithCandidateId(Guid.NewGuid().ToString()).WithLastLogIndex(1).Build();
            requestVoteResponse = await follower.Handle(requestVote);
            requestVoteResponse.VoteGranted.ShouldBeTrue();
            follower.CurrentState.VotedFor.ShouldBe(requestVote.CandidateId);
        }

        [Fact]
        public async Task FollowerShouldForwardCommandToLeader()
        {             
            _node = new NothingNode();
            var leaderId = Guid.NewGuid().ToString();
            var leader = new FakePeer(leaderId);
            _peers = new List<IPeer>
            {
                leader
            };
            _currentState = new CurrentState(_currentState.Id, _currentState.CurrentTerm, _currentState.VotedFor, _currentState.CommitIndex, _currentState.LastApplied, leaderId);
            var follower = new Follower(_currentState, _fsm, _log, _random, _node, _settings,_rules, _peers, _loggerFactory.Object);
            var response = await follower.Accept(new FakeCommand());
            response.ShouldBeOfType<OkResponse<FakeCommand>>();
            leader.ReceivedCommands.ShouldBe(1);
        }

        [Fact]
        public async Task FollowerShouldReturnRetryIfNoLeader()
        {             
            _node = new NothingNode();
            _currentState = new CurrentState(_currentState.Id, _currentState.CurrentTerm, _currentState.VotedFor, _currentState.CommitIndex, _currentState.LastApplied, _currentState.LeaderId);
            var follower = new Follower(_currentState, _fsm, _log, _random, _node, _settings,_rules, _peers, _loggerFactory.Object);
            var response = await follower.Accept(new FakeCommand());
            var error = (ErrorResponse<FakeCommand>)response;
            error.Error.ShouldBe("Please retry command later. Unable to find leader.");
        }

        [Fact]
        public async Task FollowerShouldAppendNewEntries()
        {
            _currentState = new CurrentState(Guid.NewGuid().ToString(), 0, default(string), 0, 0, default(string));
            var follower = new Follower(_currentState, _fsm, _log, _random, _node, _settings,_rules, _peers, _loggerFactory.Object);
            var logEntry = new LogEntry(new FakeCommand(), typeof(FakeCommand), 1);
            var appendEntries = new AppendEntriesBuilder().WithTerm(1).WithEntry(logEntry).Build();
            var response = await follower.Handle(appendEntries);
            response.Success.ShouldBeTrue();
            response.Term.ShouldBe(1);
            var inMemoryLog = (InMemoryLog)_log;
            inMemoryLog.ExposedForTesting.Count.ShouldBe(1);
        }

        [Fact]
        public async Task FollowerShouldNotAppendDuplicateEntry()
        {
            _node = new NothingNode();
            _currentState = new CurrentState(Guid.NewGuid().ToString(), 0, default(string), 0, 0, default(string));
            var follower = new Follower(_currentState, _fsm, _log, _random, _node, _settings,_rules, _peers, _loggerFactory.Object);
            var logEntry = new LogEntry(new FakeCommand(), typeof(FakeCommand), 1);
            var appendEntries = new AppendEntriesBuilder().WithPreviousLogIndex(1).WithPreviousLogTerm(1).WithTerm(1).WithEntry(logEntry).WithTerm(1).WithLeaderCommitIndex(1).Build();
            var response = await follower.Handle(appendEntries);
            response.Success.ShouldBeTrue();
            response.Term.ShouldBe(1);
            response = await follower.Handle(appendEntries);
            response.Success.ShouldBeTrue();
            response.Term.ShouldBe(1);
            var inMemoryLog = (InMemoryLog)_log;
            inMemoryLog.ExposedForTesting.Count.ShouldBe(1);
        }
    }
}