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

namespace Campy.TreeWalker
{
    public class MethodBodyAstBuilder : CPlusPlusCLIAstBuilder
    {
        public MethodBodyAstBuilder(DecompilerContext context)
            : base(context)
        { }

        Dictionary<String, String> _rewrite = new Dictionary<string, string>();

        public void SetUpGenericSubstitition(Dictionary<String, String> rewrite)
        {
            _rewrite = rewrite;
        }


        public override void GenerateCode(ITextOutput output)
        {
            syntaxTree.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
            TextTokenWriter outputFormatter = new TextTokenWriter(output, context) { FoldBraces = context.Settings.FoldBraces };
            CSharpFormattingOptions formattingPolicy = context.Settings.CSharpFormattingOptions;
            MethodBodyOutputVisitor vis = new MethodBodyOutputVisitor(outputFormatter, formattingPolicy);
            vis.SetUpGenericSubstitition(this._rewrite);
            syntaxTree.AcceptVisitor(vis);
        }
    }

    /// <summary>
    /// Outputs the AST.
    /// </summary>
    public class MethodBodyOutputVisitor : CPlusPlusCLIOutputVisitor
    {
        public MethodBodyOutputVisitor(TextTokenWriter formatter, CSharpFormattingOptions formattingPolicy)
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
