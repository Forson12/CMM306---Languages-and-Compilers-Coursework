using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler.Nodes.CommandNodes
{
    public class UnlessCommandNode : ICommandNode
    {
        public ICommandNode Body { get; }
        public IExpressionNode Condition { get; }
        public Position Position { get; }

        public UnlessCommandNode(IExpressionNode condition, ICommandNode body, Position pos)
        {
            Condition = condition;
            Body = body;
            Position = pos;
        }
    }
}
