using System.Collections.Generic;

namespace Coracle.Raft.Engine.Node.Validations
{
    public interface IEngineValidator
    {
        /// <summary>
        /// Determines if the <paramref name="configuration"/> satisfies mandatory checks
        /// </summary>
        /// <param name="configuration">Configuration to check</param>
        /// <param name="errors">Resultant errors</param>
        /// <returns><c>true</c> if no errors. <c>false</c> otherwise</returns>
        bool IsValid(IEngineConfiguration configuration, out IEnumerable<string> errors);
    }
}
