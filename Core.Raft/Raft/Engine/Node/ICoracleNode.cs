namespace Coracle.Raft.Engine.Node
{
    public interface ICoracleNode
    {
        #region Engine Methods 

        bool IsStarted { get; }

        /// <summary>
        /// Starts the Coracle Node. 
        /// New <see cref="TaskGuidance.BackgroundProcessing.Core.IResponsibilities"/> processor queue is configured, as the starting <see cref="States.Follower"/> state is initialized.
        /// </summary>
        void Start();

        #endregion

        #region Initialize

        bool IsInitialized { get; }

        /// <summary>
        /// Validates the <see cref="IEngineConfiguration"/> using <see cref="Node.Validations.IEngineValidator"/>.
        /// Uses <see cref="Discovery.IDiscoveryHandler"/> to initialize internal cluster configuration.
        /// </summary>
        void InitializeConfiguration();

        #endregion
    }
}
