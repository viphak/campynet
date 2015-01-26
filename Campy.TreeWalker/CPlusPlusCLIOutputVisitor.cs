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
using Campy.Utils;

namespace Campy.TreeWalker
{
    public class CPlusPlusCLIOutputVisitor : CSharpOutputVisitor
    {
        // For better, or worse, the ILSpy decompiler uses a terrible representation for an
        // attribute tree, in which the context of an AstNode is really unknown becuase there
        // are many ASTs without any parent. Absolutely terrible. Consequently, there are all
        // sorts of mechanisms in that decompiler to effectively do attribute propagation. Terrible.
        // So, for better, or worse, I continue the tradition by introducing even more attributes
        // used for context. Terrible. So, it is no longer a decompiler, but some funky hack.
        public enum SideEffects { Unset = 1,
            ContextFieldDeclaration = 2,
            ContextGlobal = 4,
            ContextOutManagedThunks = 8
        };
        public SideEffects the_context = 0;

        public CPlusPlusCLIOutputVisitor(IOutputFormatter formatter, CSharpFormattingOptions formattingPolicy)
            : base(formatter, formattingPolicy)
        {
        }

        public override void VisitCSharpTokenNode(CSharpTokenNode cSharpTokenNode)
        {
            CSharpModifierToken mod = cSharpTokenNode as CSharpModifierToken;
            if (mod != null)
            {
                StartNode(mod);
                WriteKeyword(CPlusPlusCLIModifierToken.GetModifierName(mod.Modifier));
                EndNode(mod);
            }
            else
            {
                throw new NotSupportedException("Should never visit individual tokens");
            }
        }

        public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
        {
            StartNode(typeDeclaration);
            //WriteAttributes(typeDeclaration.Attributes); no attributes
            WriteModifiers(typeDeclaration.ModifierTokens);
            BraceStyle braceStyle;
            switch (typeDeclaration.ClassType)
            {
                case ClassType.Enum:
                    WriteKeyword(Roles.EnumKeyword);
                    braceStyle = policy.EnumBraceStyle;
                    break;

                case ClassType.Interface:
                    WriteKeyword(Roles.InterfaceKeyword);
                    braceStyle = policy.InterfaceBraceStyle;
                    break;

                case ClassType.Struct:
                    WriteKeyword(Roles.StructKeyword);
                    braceStyle = policy.StructBraceStyle;
                    break;

                default:
                    //WriteKeyword(Roles.ClassKeyword);
                    WriteKeyword("ref class");
                    braceStyle = policy.ClassBraceStyle;
                    break;
            }
            typeDeclaration.NameToken.AcceptVisitor(this);
            WriteTypeParameters(typeDeclaration.TypeParameters);
            if (typeDeclaration.BaseTypes.Any())
            {
                Space();
                WriteToken(Roles.Colon);
                Space();
                WriteCommaSeparatedList(typeDeclaration.BaseTypes);
            }
            foreach (Constraint constraint in typeDeclaration.Constraints)
            {
                constraint.AcceptVisitor(this);
            }
            OpenBrace(braceStyle);
            if (typeDeclaration.ClassType == ClassType.Enum)
            {
                bool first = true;
                foreach (var member in typeDeclaration.Members)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        Comma(member, noSpaceAfterComma: true);
                        NewLine();
                    }
                    member.AcceptVisitor(this);
                }
                OptionalComma();
                NewLine();
            }
            else
            {
                this.the_context |= SideEffects.ContextFieldDeclaration;
                foreach (var member in typeDeclaration.Members)
                {
                    if (((this.the_context & SideEffects.ContextOutManagedThunks) != 0)
                        && (member is ConstructorDeclaration))
                        ; // skip output constructor because (1) it's the wrong name, (2) it's not important.
                    else
                        member.AcceptVisitor(this);
                }
                this.the_context &= ~SideEffects.ContextFieldDeclaration;
            }
            CloseBrace(braceStyle);
            OptionalSemicolon();
            NewLine();
            EndNode(typeDeclaration);
        }

    }

    public class CPlusPlusCLIAstBuilder : BaseAstBuilder
    {
        public CPlusPlusCLIOutputVisitor output_visitor;
        TextOutputFormatter outputFormatter;
        CSharpFormattingOptions formattingPolicy;

        public CPlusPlusCLIAstBuilder(DecompilerContext context)
            : base(context)
        {
        }

        public override void GenerateCode(ITextOutput output)
        {
            syntaxTree.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
            var outputFormatter = new TextOutputFormatter(output) { FoldBraces = context.Settings.FoldBraces };
            var formattingPolicy = context.Settings.CSharpFormattingOptions;
            syntaxTree.AcceptVisitor(new CPlusPlusCLIOutputVisitor(outputFormatter, formattingPolicy));
        }
    }
}
