using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Graphs
{
    public class GraphAdjList<NAME, NODE, EDGE> : IGraph<NAME>
        where NODE : GraphAdjList<NAME, NODE, EDGE>.Vertex, new()
        where EDGE : GraphAdjList<NAME, NODE, EDGE>.Edge, new()
    {
        public FiniteTotalOrderSet<NAME> NameSpace = new FiniteTotalOrderSet<NAME>();
        public NODE[] VertexSpace = new NODE[10];
        public EDGE[] EdgeSpace = new EDGE[10];
        public CompressedAdjacencyList<NAME> adj = new CompressedAdjacencyList<NAME>();

        class VertexEnumerator : IEnumerable<NAME>
        {
            NODE[] VertexSpace;

            public VertexEnumerator(NODE[] vs)
            {
                VertexSpace = vs;
            }

            public IEnumerator<NAME> GetEnumerator()
            {
                for (int i = 0; i < VertexSpace.Length; ++i)
                {
                    if (VertexSpace[i] != null)
                        yield return VertexSpace[i].Name;
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public IEnumerable<NAME> Vertices
        {
            get
            {
                return new VertexEnumerator(VertexSpace);
            }
        }

        public class EdgeEnumerator : IEnumerable<EDGE>
        {
            EDGE[] EdgeSpace;

            public EdgeEnumerator(EDGE[] es)
            {
                EdgeSpace = es;
            }

            public IEnumerator<EDGE> GetEnumerator()
            {
                for (int i = 0; i < EdgeSpace.Length; ++i)
                {
                    if (EdgeSpace[i] != null)
                        yield return EdgeSpace[i];
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public IEnumerable<IEdge<NAME>> Edges
        {
            get
            {
                return new EdgeEnumerator(EdgeSpace);
            }
        }
        
        virtual public NODE AddVertex(NAME v)
        {
            NODE vv = null;

            // NB: This code is very efficient if the name space
            // is integer, and has been preconstructed. Otherwise,
            // it will truly suck in speed.

            // Add name.
            NameSpace.Add(v);

            // Find bijection v into int domain.
            int iv = NameSpace.BijectFromBasetype(v);

            // Find node from int domain.
            if (iv >= VertexSpace.Length)
            {
                Array.Resize(ref VertexSpace, VertexSpace.Length * 2);
            }
            if (VertexSpace[iv] == null)
            {
                vv = new NODE();
                vv.Name = v;
                vv._Graph = this;
                VertexSpace[iv] = vv;
            }
            else
                vv = VertexSpace[iv];
            return vv;
        }

        virtual public void DeleteVertex(NODE vertex)
        {
        }

        virtual public EDGE AddEdge(NAME f, NAME t)
        {
            NODE vf = AddVertex(f);
            NODE vt = AddVertex(t);
            // Create adjacency table entry for (f, t).
            int j = adj.Add(f, t);
            // Create EDGE with from/to.
            if (j >= EdgeSpace.Length)
            {
                Array.Resize(ref EdgeSpace, EdgeSpace.Length * 2);
            }
            if (EdgeSpace[j] == null)
            {
                EDGE edge = new EDGE();
                edge.to = vt;
                edge.from = vf;
                EdgeSpace[j] = edge;
                return edge;
            }
            else
                return EdgeSpace[j];
        }

        virtual public void DeleteEdge(NAME f, NAME t)
        {
        }

        public void SetNameSpace(IEnumerable<NAME> ns)
        {
            NameSpace.OrderedRelationship(ns);
            adj.Construct(NameSpace);
        }

        public void Optimize()
        {
            adj.Shrink();
        }

        public GraphAdjList()
        {
        }

        class PredecessorEnumerator : IEnumerable<NAME>
        {
            GraphAdjList<NAME, NODE, EDGE> graph;
            NAME name;

            public PredecessorEnumerator(GraphAdjList<NAME, NODE, EDGE> g, NAME n)
            {
                graph = g;
                name = n;
            }

            public IEnumerator<NAME> GetEnumerator()
            {
                int[] index = graph.adj.IndexPredecessors;
                int[] data = graph.adj.DataPredecessors;
                int n = graph.NameSpace.BijectFromBasetype(name);
                NODE node = graph.VertexSpace[n];
                for (int i = index[n]; i < index[n + 1]; ++i)
                {
                    int d = data[i];
                    NAME c = graph.VertexSpace[d].Name;
                    yield return c;
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public IEnumerable<NAME> Predecessors(NAME n)
        {
            return new PredecessorEnumerator(this, n);
        }

        class ReversePredecessorEnumerator : IEnumerable<NAME>
        {
            GraphAdjList<NAME, NODE, EDGE> graph;
            NAME name;

            public ReversePredecessorEnumerator(GraphAdjList<NAME, NODE, EDGE> g, NAME n)
            {
                graph = g;
                name = n;
            }

            public IEnumerator<NAME> GetEnumerator()
            {
                int[] index = graph.adj.IndexPredecessors;
                int[] data = graph.adj.DataPredecessors;
                int n = graph.NameSpace.BijectFromBasetype(name);
                NODE node = graph.VertexSpace[n];
                for (int i = index[n + 1] - 1; i >= index[n]; --i)
                {
                    int d = data[i];
                    NAME c = graph.VertexSpace[d].Name;
                    yield return c;
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public IEnumerable<NAME> ReversePredecessors(NAME n)
        {
            return new ReversePredecessorEnumerator(this, n);
        }

        class SuccessorEnumerator : IEnumerable<NAME>
        {
            GraphAdjList<NAME, NODE, EDGE> graph;
            NAME name;

            public SuccessorEnumerator(GraphAdjList<NAME, NODE, EDGE> g, NAME n)
            {
                graph = g;
                name = n;
            }

            public IEnumerator<NAME> GetEnumerator()
            {
                int[] index = graph.adj.IndexSuccessors;
                int[] data = graph.adj.DataSuccessors;
                int n = graph.NameSpace.BijectFromBasetype(name);
                NODE node = graph.VertexSpace[n];
                for (int i = index[n]; i < index[n + 1]; ++i)
                {
                    int d = data[i];
                    NAME c = graph.VertexSpace[d].Name;
                    yield return c;
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public IEnumerable<NAME> Successors(NAME n)
        {
            return new SuccessorEnumerator(this, n);
        }

        public class ReverseSuccessorEnumerator : IEnumerable<NAME>
        {
            GraphAdjList<NAME, NODE, EDGE> graph;
            NAME name;

            public ReverseSuccessorEnumerator(GraphAdjList<NAME, NODE, EDGE> g, NAME n)
            {
                graph = g;
                name = n;
            }

            public IEnumerator<NAME> GetEnumerator()
            {
                int[] index = graph.adj.IndexSuccessors;
                int[] data = graph.adj.DataSuccessors;
                int n = graph.NameSpace.BijectFromBasetype(name);
                NODE node = graph.VertexSpace[n];
                for (int i = index[n + 1] - 1; i >= index[n]; --i)
                {
                    int d = data[i];
                    NAME c = graph.VertexSpace[d].Name;
                    yield return c;
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public IEnumerable<NAME> ReverseSuccessors(NAME n)
        {
            return new ReverseSuccessorEnumerator(this, n);
        }

        public bool IsLeaf(NAME n)
        {
            if (Successors(n).Count() == 0)
                return true;
            else
                return false;
        }

        public class Vertex : IVertex<NAME>
        {
            public NAME Name
            {
                get;
                set;
            }

            public int Index
            {
                get
                {
                    int i = _Graph.adj.FindName(Name);
                    return i;
                }
            }

            public GraphAdjList<NAME, NODE, EDGE> _Graph
            {
                get;
                set;
            }

            public Vertex()
            {
            }

            public Vertex(NAME t)
            {
                this.Name = t;
            }

            override public string ToString()
            {
                return Name.ToString();
            }


        }

        public class Edge : IEdge<NAME>
        {
            public Vertex from;

            public Vertex to;

            public Edge()
            {
            }

            public Edge(Vertex f, Vertex t)
            {
                from = (Vertex)f;
                to = (Vertex)t;
            }

            public NAME From
            {
                get { return from.Name; }
            }

            public NAME To
            {
                get { return to.Name; }
            }

            override public string ToString()
            {
                return "(" + from.Name + ", " + to.Name + ")";
            }
        }
    }

    public class GraphAdjListVertex<NAME>
        : GraphAdjList<NAME, GraphAdjListVertex<NAME>, GraphAdjListEdge<NAME>>.Vertex
    {
    }

    public class GraphAdjListEdge<NAME>
        : GraphAdjList<NAME, GraphAdjListVertex<NAME>, GraphAdjListEdge<NAME>>.Edge
    {
    }

    public class GraphAdjList<NAME>
        : GraphAdjList<NAME,
            GraphAdjListVertex<NAME>,
            GraphAdjListEdge<NAME>>
    {
    }
}
