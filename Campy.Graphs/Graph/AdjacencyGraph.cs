using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphs.Graph
{
    /// <summary>
    /// AdjacencyGraph implements the edges in a graph using an adjacency matrix. The matrix is
    /// represented using a contiguous array of bits. Edges are not explicitly represented.
    /// All edges, and successors are represented using iterators of vertices.
    /// </summary>
    public class AdjacencyGraph<T> : IGraph
    {
        /// <summary>
        /// Unique specifies whether the Name field is unique across all vertices. If true, only one vertex can have the 
        /// data with the value.
        /// </summary>
        public bool Unique
        {
            get;
            set;
        }

        /// <summary>
        /// Namespace specifis
        /// </summary>
        protected Dictionary<T, IVertex> Namespace = new Dictionary<T, IVertex>();

    }
}
