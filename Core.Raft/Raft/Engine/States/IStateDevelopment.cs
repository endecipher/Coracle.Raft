using System;
using System.Threading.Tasks;

namespace Coracle.Raft.Engine.States
{
    internal interface IStateDevelopment : ISystemState, IDisposable
    {
        bool IsDisposed { get; }

        StateValues StateValue { get; }

        IVolatileProperties VolatileState { get; }

        IStateChanger StateChanger { get; set; }

        /// <summary>
        /// This method is called from the <see cref="StateChanger"/>. 
        /// It is invoked at the start on the state which is scheduled to be changed.
        /// Any cleanup operations can be invoked here.
        /// 
        /// Cleanup should be fast, since this Method is invoked after the new state is initialized and ready to operate, 
        /// hence Configuring New Responsibilities should be faster than timeout inititated on New State Initialization.
        /// </summary>
        /// <returns>And awaitable Task</returns>
        Task OnStateChangeBeginDisposal();

        /// <summary>
        /// This method is called from the <see cref="StateChanger"/>. 
        /// It is invoked on the new state just after dependencies have been filled.
        /// Any lightweight fast startup operations can be invoked here.
        /// 
        /// <see cref="IVolatileProperties"/> are passed on from Old State 
        /// </summary>
        /// <returns>And awaitable Task</returns>
        Task InitializeOnStateChange(IVolatileProperties volatileProperties);

        /// <summary>
        /// This method is called from the <see cref="StateChanger"/>. 
        /// It is invoked once the new state has replaced the old state in <see cref="ICurrentStateAccessor"/>
        /// </summary>
        /// <returns>And awaitable Task</returns>
        Task OnStateEstablishment();
    }
}
