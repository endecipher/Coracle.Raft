namespace Rafty.UnitTests
{
    using System.Threading.Tasks;
    using Infrastructure;
    using Microsoft.Extensions.Logging;
    using Moq;
    using System;
    using System.Collections.Generic;
    using Concensus.Messages;
    using Concensus.Node;
    using Concensus.Peers;
    using Rafty.Concensus;
    using Rafty.Concensus.States;
    using Rafty.FiniteStateMachine;
    using Rafty.Log;
    using Shouldly;
    using Xunit;
    /*
        1. Reply false if term < currentTerm (§5.1)

        2. Reply false if log doesn’t contain an entry at prevLogIndex
        whose term matches prevLogTerm (§5.3)

        3. If an existing entry conflicts with a new one (same index
        but different terms), delete the existing entry and all that
        follow it (§5.3)

        4. Append any new entries not already in the log

        5. If leaderCommit > commitIndex, set commitIndex =
        min(leaderCommit, index of last new entry)
    */

    public class AppendEntriesTests
    {
        private readonly IFiniteStateMachine _fsm;
        private readonly INode _node;
        private CurrentState _currentState;
        private readonly ILog _log;
        private readonly List<IPeer> _peers;
        private readonly IRandomDelay _random;
        private readonly InMemorySettings _settings;
        private readonly IRules _rules;
        private readonly Mock<ILoggerFactory> _loggerFactory;

        public AppendEntriesTests()
        {
            _loggerFactory = new Mock<ILoggerFactory>();
            var logger = new Mock<ILogger>();
            _loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(logger.Object);
            _rules = new Rules(_loggerFactory.Object, new NodeId(default(string)));
            _settings = new InMemorySettingsBuilder().Build();
            _random = new RandomDelay();
             _log = new InMemoryLog();
            _peers = new List<IPeer>();
            _fsm = new InMemoryStateMachine();
            _node = new NothingNode();
        }
        
        [Fact]
        public async Task ShouldReplyFalseIfRpcTermLessThanCurrentTerm()
        {
            _currentState = new CurrentState(Guid.NewGuid().ToString(), 1, default(string), 0, 0, default(string));
            var appendEntriesRpc = new AppendEntriesBuilder().WithTerm(0).Build();
            var follower = new Follower(_currentState, _fsm, _log, _random, _node, _settings, _rules, _peers, _loggerFactory.Object);
            var appendEntriesResponse = await follower.Handle(appendEntriesRpc);
            appendEntriesResponse.Success.ShouldBe(false);
            appendEntriesResponse.Term.ShouldBe(1);
        }

        [Fact]
        public async Task ShouldReplyFalseIfLogDoesntContainEntryAtPreviousLogIndexWhoseTermMatchesRpcPrevLogTerm()
        {
            _currentState = new CurrentState(Guid.NewGuid().ToString(), 2, default(string), 0, 0, default(string));
            await _log.Apply(new LogEntry(new FakeCommand(""), typeof(string), 2));
            var appendEntriesRpc = new AppendEntriesBuilder().WithTerm(2).WithPreviousLogIndex(1).WithPreviousLogTerm(1).Build();
            var follower = new Follower(_currentState, _fsm, _log, _random, _node, _settings, _rules, _peers, _loggerFactory.Object);
            var appendEntriesResponse = await follower.Handle(appendEntriesRpc);
            appendEntriesResponse.Success.ShouldBe(false);
            appendEntriesResponse.Term.ShouldBe(2);
        }

        [Fact]
        public async Task ShouldDeleteExistingEntryIfItConflictsWithNewOne()
        {
            _currentState = new CurrentState(Guid.NewGuid().ToString(), 1, default(string), 2, 0, default(string));
            await _log.Apply(new LogEntry(new FakeCommand("term 1 commit index 1"), typeof(string), 1));
            await _log.Apply(new LogEntry(new FakeCommand("term 1 commit index 2"), typeof(string), 1));
            await _log.Apply(new LogEntry(new FakeCommand("term 1 commit index 3"), typeof(string), 1));
            var appendEntriesRpc = new AppendEntriesBuilder()
                .WithEntry(new LogEntry(new FakeCommand("term 2 commit index 3"), typeof(string),2))
                .WithTerm(2)
                .WithPreviousLogIndex(2)
                .WithPreviousLogTerm(1)
                .WithLeaderCommitIndex(3)
                .Build();
            var follower = new Follower(_currentState, _fsm, _log, _random, _node, _settings, _rules, _peers, _loggerFactory.Object);
            var appendEntriesResponse = await follower.Handle(appendEntriesRpc);
            appendEntriesResponse.Success.ShouldBe(true);
            appendEntriesResponse.Term.ShouldBe(2);
        }

        [Fact]
        public async Task ShouldDeleteExistingEntryIfItConflictsWithNewOneAndAppendNewEntries()
        {
            _currentState = new CurrentState(Guid.NewGuid().ToString(), 1, default(string), 0, 0, default(string));
            await _log.Apply(new LogEntry(new FakeCommand("term 1 commit index 1"), typeof(string), 1));
            await _log.Apply(new LogEntry(new FakeCommand("term 1 commit index 2"), typeof(string), 1));
            await _log.Apply(new LogEntry(new FakeCommand("term 1 commit index 3"), typeof(string), 1));
            var appendEntriesRpc = new AppendEntriesBuilder()
                .WithEntry(new LogEntry(new FakeCommand("term 2 commit index 3"), typeof(string), 2))
                .WithTerm(2)
                .WithPreviousLogIndex(2)
                .WithPreviousLogTerm(1)
                .WithLeaderCommitIndex(3)
                .Build();
            var follower = new Follower(_currentState, _fsm, _log, _random, _node, _settings, _rules, _peers, _loggerFactory.Object);
            var appendEntriesResponse = await follower.Handle(appendEntriesRpc);
            appendEntriesResponse.Success.ShouldBe(true);
            appendEntriesResponse.Term.ShouldBe(2);
            _log.GetTermAtIndex(2).Result.ShouldBe(2);
        }

        [Fact]
        public async Task ShouldAppendAnyEntriesNotInTheLog()
        {
            _currentState = new CurrentState(Guid.NewGuid().ToString(), 1, default(string), 0, 0, default(string));
            await _log.Apply(new LogEntry(new FakeCommand("term 1 commit index 0"), typeof(string), 1));
            var appendEntriesRpc = new AppendEntriesBuilder()
                .WithEntry(new LogEntry(new FakeCommand("term 1 commit index 1"), typeof(string), 1))
                .WithTerm(1)
                .WithPreviousLogIndex(1)
                .WithPreviousLogTerm(1)
                .WithLeaderId(Guid.NewGuid().ToString())
                .Build();
            var follower = new Follower(_currentState, _fsm, _log, _random, _node, _settings, _rules, _peers, _loggerFactory.Object);
            var appendEntriesResponse = await follower.Handle(appendEntriesRpc);
            appendEntriesResponse.Success.ShouldBe(true);
            appendEntriesResponse.Term.ShouldBe(1);
            _log.GetTermAtIndex(1).Result.ShouldBe(1);
            follower.CurrentState.LeaderId.ShouldBe(appendEntriesRpc.LeaderId);
        }

        [Fact]
        public async Task FollowerShouldSetCommitIndexIfLeaderCommitGreaterThanCommitIndex()
        {
            _currentState = new CurrentState(Guid.NewGuid().ToString(), 1, default(string), 0, 0, default(string));
            var log = new LogEntry(new FakeCommand("term 1 commit index 0"), typeof(string), 1);
            await _log.Apply(log);
            var appendEntriesRpc = new AppendEntriesBuilder()
               .WithEntry(log)
               .WithTerm(1)
               .WithPreviousLogIndex(1)
               .WithPreviousLogTerm(1)
               .WithLeaderCommitIndex(1)
               .Build();
            var follower = new Follower(_currentState, _fsm, _log, _random, _node, _settings, _rules, _peers, _loggerFactory.Object);
            var response = await follower.Handle(appendEntriesRpc);
            response.Success.ShouldBeTrue();
            follower.CurrentState.CommitIndex.ShouldBe(1);
        }

        [Fact]
        public async Task CandidateShouldSetCommitIndexIfLeaderCommitGreaterThanCommitIndex()
        {
            _currentState = new CurrentState(Guid.NewGuid().ToString(), 0, default(string), 0, 0, default(string));
            var log = new LogEntry(new FakeCommand("term 1 commit index 0"), typeof(string), 1);
            await _log.Apply(log);
            var appendEntriesRpc = new AppendEntriesBuilder()
               .WithEntry(log)
               .WithTerm(1)
               .WithPreviousLogIndex(1)
               .WithPreviousLogTerm(1)
               .WithLeaderCommitIndex(1)
               .WithLeaderId(Guid.NewGuid().ToString())
               .Build();
            var candidate = new Candidate(_currentState, _fsm, _peers, _log, _random, _node, _settings, _rules, _loggerFactory.Object);
            var response = await candidate.Handle(appendEntriesRpc);
            response.Success.ShouldBeTrue();
            candidate.CurrentState.CommitIndex.ShouldBe(1);
            candidate.CurrentState.LeaderId.ShouldBe(appendEntriesRpc.LeaderId);
        }

        [Fact]
        public async Task LeaderShouldSetCommitIndexIfLeaderCommitGreaterThanCommitIndex()
        {
            _currentState = new CurrentState(Guid.NewGuid().ToString(), 0, default(string), 0, 0, default(string));
            var log = new LogEntry(new FakeCommand("term 1 commit index 0"), typeof(string), 1);
            await _log.Apply(log);
            var appendEntriesRpc = new AppendEntriesBuilder()
               .WithEntry(log)
               .WithTerm(1)
               .WithPreviousLogIndex(1)
               .WithPreviousLogTerm(1)
               .WithLeaderCommitIndex(1)
               .WithLeaderId(Guid.NewGuid().ToString())
               .Build();
            var leader = new Leader(_currentState, _fsm, (s) => _peers, _log, _node, _settings, _rules, _loggerFactory.Object);
            var response = await leader.Handle(appendEntriesRpc);
            response.Success.ShouldBeTrue();
            leader.CurrentState.CommitIndex.ShouldBe(1);
            leader.CurrentState.LeaderId.ShouldBe(appendEntriesRpc.LeaderId);
        }
    }
}