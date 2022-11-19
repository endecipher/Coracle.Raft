using EventGuidance.Structure;
using System.Collections.Generic;

namespace EventGuidance.DataStructures
{
    public class PriorityComparer : IComparer<ActionPriorityValues>
    {
        public int Compare(ActionPriorityValues x, ActionPriorityValues y)
        {
            return ((int)y) - ((int)x);
        }
    }
}
