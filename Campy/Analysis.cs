using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.PE;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using System.IO;
using System.Diagnostics;
using SR = System.Reflection;
using Campy.Utils;

namespace Campy
{
    class Analysis
    {
        public static List<Delegate> FindAllTargets(System.Delegate del)
        {
            // To do this properly, a complete control flow graph with constant
            // propagation would have to be contructed. With that information,
            // we could determine with reasonable certainty what targets would
            // require translation to C++ AMP. We already use ILSpy for the
            // representation of the program, or we could go back to System.Reflection.
            //
            // For now, perform a transitive closure of the fields of the delegate.
            // This does a pretty reason job. C++ AMP adds a constraint in that
            // it cannot access data outside of auto variables (variables declared
            // in the lexically enclosing block). As a result, the call to the
            // delegate must be inlined to the top level delegate.
            StackQueue<Delegate> stack = new StackQueue<Delegate>();
            stack.Push(del);
            List<Delegate> delegates = new List<Delegate>();
            while (stack.Count > 0)
            {
                Delegate d = stack.Pop();
                if (delegates.Contains(d))
                    continue;
                delegates.Add(d);
                System.MulticastDelegate md = d as System.MulticastDelegate;
                if (md != null)
                {
                    foreach (System.Delegate d2 in md.GetInvocationList())
                    {
                        stack.Push(d2);
                    }
                }
                object target = md.Target;
                if (target == null)
                {
                    // If target is null, then the delegate is a function that
                    // uses either static data, or does not require any additional
                    // data.
                }
                else
                {
                    // Examine all fields, looking for delegates.
                    Type target_type = target.GetType();

                    // Convert all fields which happen to also be multicast delegates.
                    SR.FieldInfo[] target_type_fieldinfo = target_type.GetFields();
                    foreach (var field in target_type_fieldinfo)
                    {
                        var value = field.GetValue(target);
                        Type ft = value.GetType();
                        System.Delegate child = value as System.Delegate;
                        if (child != null)
                        {
                            // Chase down the field.
                            stack.Push(child);
                        }
                    }
                }
            }
            return delegates;
        }
    }
}
