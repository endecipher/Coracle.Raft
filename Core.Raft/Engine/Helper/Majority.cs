using System;

namespace Coracle.Raft.Engine.Helper
{
    public static class Majority
    {
        public static bool HasAttained(int satisfiedNodes, int totalNodes)
        {
            return satisfiedNodes >= Math.Floor(totalNodes / 2d) + 1;
        }
    }
}
