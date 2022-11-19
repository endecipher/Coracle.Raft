using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Raft.Canoe.Engine.Exceptions
{
    public class LeaderNotFoundException : Exception
    {
        public LeaderNotFoundException(string message) : base(message)
        {

        }

        public static LeaderNotFoundException New()
        {
            return new LeaderNotFoundException("The Leader is not Known to this Node as of yet. Please try later. ");
        }
    }
}
