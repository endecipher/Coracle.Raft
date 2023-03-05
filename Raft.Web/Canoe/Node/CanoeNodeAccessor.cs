#region License
// Copyright (c) 2023 Ayan Choudhury
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion

using Core.Raft.Canoe.Engine.Node;
using EventGuidance.Dependency;

namespace Raft.Web.Canoe.Node
{
    public interface INode
    {
        ICanoeNode CanoeNode { get; set; }

        void Initialize();
    }

    public class CanoeNodeAccessor : INode
    {
        private object obj =  new object();

        private ICanoeNode _node = null;

        public ICanoeNode CanoeNode { 
            get
            {
                lock (obj)
                {
                    return _node;
                }
            }
            set
            {
                lock (obj)
                {
                    _node = value;
                }
            }
        }

        public CanoeNodeAccessor()
        {
            CanoeNode = ComponentContainer.Instance.GetInstance<ICanoeNode>();
        }

        public void Initialize()
        {
            lock (obj)
            {
                CanoeNode.Start();
            }
        }
    }
}
