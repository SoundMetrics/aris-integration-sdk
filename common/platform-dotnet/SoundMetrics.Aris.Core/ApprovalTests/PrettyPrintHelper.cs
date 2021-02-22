using System;
using System.Collections.Generic;
using System.Text;

namespace SoundMetrics.Aris.Core.ApprovalTests
{
    public class PrettyPrintHelper
    {
        public PrettyPrintHelper(int indent)
        {
            indents.Push(indent);
        }

        public PrettyPrintHelper PrintHeading(string heading)
        {
            var indent = indents.Peek();
            var indentString = new string(' ', indent);

            builder.Append(indentString);
            builder.Append(heading);
            builder.Append(": ");
            builder.AppendLine();

            return this;
        }

        public PrettyPrintHelper PrintValue(string label, object value)
        {
            var indent = indents.Peek();
            var indentString = new string(' ', indent);

            if (value is IPrettyPrintable printable)
            {
                printable.PrettyPrint(this);
            }
            else
            {
                builder.Append(indentString);
                builder.Append(label);
                builder.Append(": ");
                PrintValue();
            }

            return this;

            void PrintValue()
            {
                if (value is null)
                {
                    builder.AppendLine("null");
                }
                else
                {
                    builder.AppendLine(value.ToString());
                }
            }
        }

        public IDisposable PushIndent()
        {
            const int IndentIncrement = 4;
            indents.Push(indents.Peek() + IndentIncrement);
            return new IndentPop(this);
        }

        private void PopIndent() => indents.Pop();

        public override string ToString() => builder.ToString();

        private readonly StringBuilder builder = new StringBuilder();
        private readonly Stack<int> indents = new Stack<int>();

        private class IndentPop : IDisposable
        {
            public IndentPop(PrettyPrintHelper helper)
            {
                this.helper = helper;
            }

            public void Dispose()
            {
                helper.PopIndent();
            }

            private readonly PrettyPrintHelper helper;
        }
    }
}
