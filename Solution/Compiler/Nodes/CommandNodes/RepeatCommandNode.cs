using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler.Nodes.CommandNodes
{
    public class RepeatCommandNode : ICommandNode
    {
        public ICommandNode Body { get; }
        public IExpressionNode Condition { get; }
        public Position Position { get; }

        public RepeatCommandNode(ICommandNode body, IExpressionNode condition, Position pos)
        {
            Body = body;
            Condition = condition;
            Position = pos;
        }
    }
}
