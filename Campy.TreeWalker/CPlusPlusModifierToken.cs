using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Globalization;

namespace Campy.TreeWalker
{

    public class CPlusPlusModifierToken : CSharpTokenNode
	{
		public Modifiers modifier;
		
		public Modifiers Modifier {
			get { return modifier; }
			set { 
				ThrowIfFrozen();
				this.modifier = value; 
			}
		}
		
		protected int TokenLength {
			get {
				return GetModifierName (modifier).Length;
			}
		}
		
		public string GetText (CSharpFormattingOptions formattingOptions = null)
		{
			return GetModifierName (Modifier);
		}
		
		protected override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
            CPlusPlusModifierToken o = other as CPlusPlusModifierToken;
			return o != null && this.modifier == o.modifier;
		}
		
		// Not worth using a dictionary for such few elements.
		// This table is sorted in the order that modifiers should be output when generating code.
		static readonly Modifiers[] allModifiers = {
			Modifiers.Public, Modifiers.Protected, Modifiers.Private, Modifiers.Internal,
			Modifiers.New,
			Modifiers.Unsafe,
			Modifiers.Abstract, Modifiers.Virtual, Modifiers.Sealed, Modifiers.Static, Modifiers.Override,
			Modifiers.Readonly, Modifiers.Volatile,
			Modifiers.Extern, Modifiers.Partial, Modifiers.Const,
			Modifiers.Async,
			Modifiers.Any
		};
		
		public static IEnumerable<Modifiers> AllModifiers {
			get { return allModifiers; }
		}

        public CPlusPlusModifierToken(TextLocation location, TokenRole role) : base(location, role)
        {
        }
		
		public static string GetModifierName(Modifiers modifier)
		{
			switch (modifier) {
				case Modifiers.Private:
					return "private:";
				case Modifiers.Internal:
					return "internal";
				case Modifiers.Protected:
					return "protected:";
				case Modifiers.Public:
					return "public:";
				case Modifiers.Abstract:
					return "abstract";
				case Modifiers.Virtual:
					return "virtual";
				case Modifiers.Sealed:
                    return ""; // "final" in C++ CLI and occurs afterwards.
				case Modifiers.Static:
					return "static";
				case Modifiers.Override:
					return "override";
				case Modifiers.Readonly:
					return "readonly";
				case Modifiers.Const:
					return "const";
				case Modifiers.New:
					return "new";
				case Modifiers.Partial:
					return "partial";
				case Modifiers.Extern:
					return "extern";
				case Modifiers.Volatile:
					return "volatile";
				case Modifiers.Unsafe:
					return "unsafe";
				case Modifiers.Async:
					return "async";
				case Modifiers.Any:
					// even though it's used for pattern matching only, 'any' needs to be in this list to be usable in the AST
					return "any";
				default:
					throw new NotSupportedException("Invalid value for Modifiers");
			}
		}
	}
    public class CPlusPlusCLIModifierToken : CSharpTokenNode
    {
        Modifiers modifier;

        public Modifiers Modifier
        {
            get { return modifier; }
            set
            {
                ThrowIfFrozen();
                this.modifier = value;
            }
        }

        protected int TokenLength
        {
            get
            {
                return GetModifierName(modifier).Length;
            }
        }

        public string GetText(CSharpFormattingOptions formattingOptions = null)
        {
            return GetModifierName(Modifier);
        }

        protected override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
        {
            CPlusPlusModifierToken o = other as CPlusPlusModifierToken;
            return o != null && this.modifier == o.modifier;
        }

        // Not worth using a dictionary for such few elements.
        // This table is sorted in the order that modifiers should be output when generating code.
        static readonly Modifiers[] allModifiers = {
			Modifiers.Public, Modifiers.Protected, Modifiers.Private, Modifiers.Internal,
			Modifiers.New,
			Modifiers.Unsafe,
			Modifiers.Abstract, Modifiers.Virtual, Modifiers.Sealed, Modifiers.Static, Modifiers.Override,
			Modifiers.Readonly, Modifiers.Volatile,
			Modifiers.Extern, Modifiers.Partial, Modifiers.Const,
			Modifiers.Async,
			Modifiers.Any
		};

        public static IEnumerable<Modifiers> AllModifiers
        {
            get { return allModifiers; }
        }

        public CPlusPlusCLIModifierToken(TextLocation location, TokenRole role) : base(location, role)
        {
        }

        public static string GetModifierName(Modifiers modifier)
        {
            switch (modifier)
            {
                case Modifiers.Private:
                    return "private";
                case Modifiers.Internal:
                    return "internal";
                case Modifiers.Protected:
                    return "protected";
                case Modifiers.Public:
                    return "public";
                case Modifiers.Abstract:
                    return "abstract";
                case Modifiers.Virtual:
                    return "virtual";
                case Modifiers.Sealed:
                    return ""; // "final" in C++ CLI and occurs afterwards.
                case Modifiers.Static:
                    return "static";
                case Modifiers.Override:
                    return "override";
                case Modifiers.Readonly:
                    return "readonly";
                case Modifiers.Const:
                    return "const";
                case Modifiers.New:
                    return "new";
                case Modifiers.Partial:
                    return "partial";
                case Modifiers.Extern:
                    return "extern";
                case Modifiers.Volatile:
                    return "volatile";
                case Modifiers.Unsafe:
                    return "unsafe";
                case Modifiers.Async:
                    return "async";
                case Modifiers.Any:
                    // even though it's used for pattern matching only, 'any' needs to be in this list to be usable in the AST
                    return "any";
                default:
                    throw new NotSupportedException("Invalid value for Modifiers");
            }
        }
    }
    public class CPlusPlusCLIFieldModifierToken : CSharpTokenNode
    {
        Modifiers modifier;

        public Modifiers Modifier
        {
            get { return modifier; }
            set
            {
                ThrowIfFrozen();
                this.modifier = value;
            }
        }

        protected int TokenLength
        {
            get
            {
                return GetModifierName(modifier).Length;
            }
        }

        public string GetText(CSharpFormattingOptions formattingOptions = null)
        {
            return GetModifierName(Modifier);
        }

        protected override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
        {
            CPlusPlusModifierToken o = other as CPlusPlusModifierToken;
            return o != null && this.modifier == o.modifier;
        }

        // Not worth using a dictionary for such few elements.
        // This table is sorted in the order that modifiers should be output when generating code.
        static readonly Modifiers[] allModifiers = {
			Modifiers.Public, Modifiers.Protected, Modifiers.Private, Modifiers.Internal,
			Modifiers.New,
			Modifiers.Unsafe,
			Modifiers.Abstract, Modifiers.Virtual, Modifiers.Sealed, Modifiers.Static, Modifiers.Override,
			Modifiers.Readonly, Modifiers.Volatile,
			Modifiers.Extern, Modifiers.Partial, Modifiers.Const,
			Modifiers.Async,
			Modifiers.Any
		};

        public CPlusPlusCLIFieldModifierToken(TextLocation location, TokenRole role) : base(location, role)
        {
        }

        public static IEnumerable<Modifiers> AllModifiers
        {
            get { return allModifiers; }
        }

        public static string GetModifierName(Modifiers modifier)
        {
            switch (modifier)
            {
                case Modifiers.Private:
                    return "private:";
                case Modifiers.Internal:
                    return "internal";
                case Modifiers.Protected:
                    return "protected:";
                case Modifiers.Public:
                    return "public:";
                case Modifiers.Abstract:
                    return "abstract";
                case Modifiers.Virtual:
                    return "virtual";
                case Modifiers.Sealed:
                    return ""; // "final" in C++ CLI and occurs afterwards.
                case Modifiers.Static:
                    return "static";
                case Modifiers.Override:
                    return "override";
                case Modifiers.Readonly:
                    return "readonly";
                case Modifiers.Const:
                    return "const";
                case Modifiers.New:
                    return "new";
                case Modifiers.Partial:
                    return "partial";
                case Modifiers.Extern:
                    return "extern";
                case Modifiers.Volatile:
                    return "volatile";
                case Modifiers.Unsafe:
                    return "unsafe";
                case Modifiers.Async:
                    return "async";
                case Modifiers.Any:
                    // even though it's used for pattern matching only, 'any' needs to be in this list to be usable in the AST
                    return "any";
                default:
                    throw new NotSupportedException("Invalid value for Modifiers");
            }
        }
    }

}