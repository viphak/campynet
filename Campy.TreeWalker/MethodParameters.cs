using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.NRefactory.CSharp;

namespace Campy.TreeWalker
{
    public class MethodParametersAstBuilder : BaseAstBuilder
    {
        public MethodParametersAstBuilder(DecompilerContext context)
            : base(context)
        { }

        public override void GenerateCode(ITextOutput output)
        {
            syntaxTree.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
            TextTokenWriter outputFormatter = new TextTokenWriter(output, context) { FoldBraces = context.Settings.FoldBraces };
            CSharpFormattingOptions formattingPolicy = context.Settings.CSharpFormattingOptions;
            syntaxTree.AcceptVisitor(new MethodParametersOutputVisitor(outputFormatter, formattingPolicy));
        }
    }

    /// <summary>
    /// Outputs the AST.
    /// </summary>
    public class MethodParametersOutputVisitor : CSharpOutputVisitor
    {
        public MethodParametersOutputVisitor(TextTokenWriter formatter, CSharpFormattingOptions formattingPolicy)
            : base(formatter, formattingPolicy)
        {
        }

        public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
        {
            StartNode(methodDeclaration);
            // WriteAttributes(methodDeclaration.Attributes);
            // WriteModifiers(methodDeclaration.ModifierTokens);
            // methodDeclaration.ReturnType.AcceptVisitor(this);
            // Space();
            // WritePrivateImplementationType(methodDeclaration.PrivateImplementationType);
            // methodDeclaration.NameToken.AcceptVisitor(this);
            // WriteTypeParameters(methodDeclaration.TypeParameters);
            // Space(policy.SpaceBeforeMethodDeclarationParentheses);
            WriteCommaSeparatedListInParenthesis(methodDeclaration.Parameters, policy.SpaceWithinMethodDeclarationParentheses);
            // foreach (Constraint constraint in methodDeclaration.Constraints)
            // {
            //     constraint.AcceptVisitor(this);
            // }
            // WriteMethodBody(methodDeclaration.Body);
            EndNode(methodDeclaration);
        }
    }
}
