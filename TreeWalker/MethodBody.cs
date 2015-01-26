using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory.Utils;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.TypeSystem;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace TreeWalker
{
    public class MethodBodyAstBuilder : BaseAstBuilder
    {
        public MethodBodyAstBuilder(DecompilerContext context)
            : base(context)
        { }

        public override void GenerateCode(ITextOutput output)
        {
            syntaxTree.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
            var outputFormatter = new TextOutputFormatter(output) { FoldBraces = context.Settings.FoldBraces };
            var formattingPolicy = context.Settings.CSharpFormattingOptions;
            syntaxTree.AcceptVisitor(new MethodBodyOutputVisitor(outputFormatter, formattingPolicy));
        }
    }

    /// <summary>
    /// Outputs the AST.
    /// </summary>
    public class MethodBodyOutputVisitor : CSharpOutputVisitor
    {
        public MethodBodyOutputVisitor(IOutputFormatter formatter, CSharpFormattingOptions formattingPolicy)
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
            // WriteCommaSeparatedListInParenthesis(methodDeclaration.Parameters, policy.SpaceWithinMethodDeclarationParentheses);
            // foreach (Constraint constraint in methodDeclaration.Constraints)
            // {
            //     constraint.AcceptVisitor(this);
            // }
            WriteMethodBody(methodDeclaration.Body);
            EndNode(methodDeclaration);
        }
    }
}
