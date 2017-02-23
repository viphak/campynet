using System;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.NRefactory.CSharp;

namespace Campy.TreeWalker
{
    public class MethodSignatureAstBuilder : CPlusPlusCLIAstBuilder
    {
        public MethodSignatureAstBuilder(DecompilerContext context)
            : base(context)
        { }

        public override void GenerateCode(ITextOutput output)
        {
            syntaxTree.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
            TextTokenWriter outputFormatter = new TextTokenWriter(output, context) { FoldBraces = context.Settings.FoldBraces };
            CSharpFormattingOptions formattingPolicy = context.Settings.CSharpFormattingOptions;
            syntaxTree.AcceptVisitor(new MethodSignatureOutputVisitor(outputFormatter, formattingPolicy));
        }
    }

    public class MethodSignatureOutputVisitor : CPlusPlusCLIOutputVisitor
    {
        public MethodSignatureOutputVisitor(TextTokenWriter formatter, CSharpFormattingOptions formattingPolicy)
            : base(formatter, formattingPolicy)
        {
        }

        public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
        {
            StartNode(methodDeclaration);
            // WriteAttributes(methodDeclaration.Attributes);
            WriteModifiers(methodDeclaration.ModifierTokens);
            methodDeclaration.ReturnType.AcceptVisitor(this);
            Space();
            WritePrivateImplementationType(methodDeclaration.PrivateImplementationType);
            methodDeclaration.NameToken.AcceptVisitor(this);
            WriteTypeParameters(methodDeclaration.TypeParameters);
            Space(policy.SpaceBeforeMethodDeclarationParentheses);
            WriteCommaSeparatedListInParenthesis(methodDeclaration.Parameters, policy.SpaceWithinMethodDeclarationParentheses);
            // foreach (Constraint constraint in methodDeclaration.Constraints)
            // {
            //     constraint.AcceptVisitor(this);
            // }
            // WriteMethodBody(methodDeclaration.Body);
            EndNode(methodDeclaration);
        }

        public override void VisitCSharpTokenNode(CSharpTokenNode cSharpTokenNode)
        {
            CSharpModifierToken mod = cSharpTokenNode as CSharpModifierToken;
            if (mod != null)
            {
                // In context of FieldDefinition, the modifier is outputted using "<type> :" (type followed by colon).
                StartNode(mod);
                WriteKeyword(CPlusPlusCLIFieldModifierToken.GetModifierName(mod.Modifier));
                EndNode(mod);
            }
            else
            {
                throw new NotSupportedException("Should never visit individual tokens");
            }
        }
    }
}
