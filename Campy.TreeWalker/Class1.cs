using System.Globalization;
using System.IO;
using ICSharpCode.NRefactory.CSharp;

namespace Campy.TreeWalker
{
    class Class1 : TextWriterTokenWriter
    {
        private TextWriter textWriter;

        public Class1(TextWriter tw) : base(tw)
        {
            textWriter = tw;
        }

        public override void WritePrimitiveValue(object value, string literalValue = null)
        {
            if (value is float)
            {
                float f = (float)value;
                if (float.IsInfinity(f) || float.IsNaN(f))
                {
                    // Strictly speaking, these aren't PrimitiveExpressions;
                    // but we still support writing these to make life easier for code generators.
                    textWriter.Write("float");
                    WriteToken(Roles.Dot, ".");
                    if (float.IsPositiveInfinity(f))
                    {
                        textWriter.Write("PositiveInfinity");
                    }
                    else if (float.IsNegativeInfinity(f))
                    {
                        textWriter.Write("NegativeInfinity");
                    }
                    else
                    {
                        textWriter.Write("NaN");
                    }
                    return;
                }
                if (f == 0 && 1 / f == float.NegativeInfinity)
                {
                    // negative zero is a special case
                    // (again, not a primitive expression, but it's better to handle
                    // the special case here than to do it in all code generators)
                    textWriter.Write("-");
                }
                var str = f.ToString("R", NumberFormatInfo.InvariantInfo); // KED + "f";
                textWriter.Write(str);
            }
        }
    }
}
