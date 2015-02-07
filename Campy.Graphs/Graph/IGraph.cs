﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphs
{
    public interface IGraph<NAME>
    {
        IEnumerable<NAME> Vertices
        {
            get;
            //set;
        }

        IEnumerable<IEdge<NAME>> Edges
        {
            get;
            //set;
        }

        IEnumerable<NAME> Predecessors(NAME n);

        IEnumerable<NAME> ReversePredecessors(NAME n);

        IEnumerable<NAME> Successors(NAME n);

        IEnumerable<NAME> ReverseSuccessors(NAME n);

        bool IsLeaf(NAME node);
    }
}
