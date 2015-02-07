﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace NewGraphs
{
    public class GraphLinkedList<NAME, NODE, EDGE> : IGraph<NAME>
        where NODE : GraphLinkedList<NAME, NODE, EDGE>.Vertex, new()
        where EDGE : GraphLinkedList<NAME, NODE, EDGE>.Edge, new()
    {
        bool allow_duplicates = false;

        public FiniteTotalOrderSet<NAME> NameSpace = new FiniteTotalOrderSet<NAME>();
        public NODE[] VertexSpace = new NODE[10];
        public bool[] VertexSpaceDefined = new bool[10];
        public EDGE[] EdgeSpace = new EDGE[10];

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

        public class EdgeEnumerator : IEnumerable<IEdge<NAME>>
        {
            EDGE[] EdgeSpace;

            public EdgeEnumerator(EDGE[] es)
            {
                EdgeSpace = es;
            }

            public IEnumerator<IEdge<NAME>> GetEnumerator()
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

        virtual public GraphLinkedList<NAME, NODE, EDGE>.Vertex AddVertex(NAME v)
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
                Array.Resize(ref VertexSpaceDefined, VertexSpace.Length * 2);
            }
            if (!VertexSpaceDefined[iv])
            {
                vv = new NODE();
                vv.Name = v;
                vv._Graph = this;
                VertexSpace[iv] = vv;
                VertexSpaceDefined[iv] = true;
            }
            else
                vv = VertexSpace[iv];
            return vv;
        }

        virtual public void DeleteVertex(Vertex vertex)
        {
        }

        virtual public GraphLinkedList<NAME, NODE, EDGE>.Edge AddEdge(NAME f, NAME t)
        {
            Vertex vf = AddVertex(f);
            Vertex vt = AddVertex(t);
            // Graphs should not have duplicates!
            if (!allow_duplicates)
            {
                foreach (Edge search in vf._Successors)
                {
                    if (search.to == vt)
                        return search;
                }
            }
            Edge edge = new EDGE();
            edge.from = vf;
            edge.to = vt;
            edge.from._Successors.Add(edge);
            edge.to._Predecessors.Add(edge);
            return edge;
        }

        virtual public GraphLinkedList<NAME, NODE, EDGE>.Edge AddEdge(GraphLinkedList<NAME, NODE, EDGE>.Vertex f, GraphLinkedList<NAME, NODE, EDGE>.Vertex t)
        {
            EDGE edge = new EDGE();
            edge.from = f;
            edge.to = t;
            //Edge edge = new Edge((Vertex)f, (Vertex)t);
            edge.from._Successors.Add(edge);
            edge.to._Predecessors.Add(edge);
            return edge;
        }

        virtual public void DeleteEdge(Edge edge)
        {
        }

        virtual public void DeleteEdge(Vertex f, Vertex t)
        {
        }

        public void SetNameSpace(IEnumerable<NAME> ns)
        {
            NameSpace.OrderedRelationship(ns);
        }

        virtual public void Optimize()
        {
        }

        public GraphLinkedList()
        {
        }

        class PredecessorEnumerator : IEnumerable<NAME>
        {
            GraphLinkedList<NAME, NODE, EDGE> graph;
            NAME name;

            public PredecessorEnumerator(GraphLinkedList<NAME, NODE, EDGE> g, NAME n)
            {
                graph = g;
                name = n;
            }

            public IEnumerator<NAME> GetEnumerator()
            {
                int n = graph.NameSpace.BijectFromBasetype(name);
                NODE node = graph.VertexSpace[n];
                foreach (EDGE e in node._Predecessors)
                {
                    yield return e.From;
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
            GraphLinkedList<NAME, NODE, EDGE> graph;
            NAME name;

            public ReversePredecessorEnumerator(GraphLinkedList<NAME, NODE, EDGE> g, NAME n)
            {
                graph = g;
                name = n;
            }

            public IEnumerator<NAME> GetEnumerator()
            {
                int n = graph.NameSpace.BijectFromBasetype(name);
                NODE node = graph.VertexSpace[n];
                for (int i = node._Predecessors.Count - 1; i >= 0; --i)
                {
                    GraphLinkedList<NAME, NODE, EDGE>.Edge e = node._Predecessors[i];
                    yield return e.From;
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
            GraphLinkedList<NAME, NODE, EDGE> graph;
            NAME name;

            public SuccessorEnumerator(GraphLinkedList<NAME, NODE, EDGE> g, NAME n)
            {
                graph = g;
                name = n;
            }

            public IEnumerator<NAME> GetEnumerator()
            {
                int n = graph.NameSpace.BijectFromBasetype(name);
                NODE node = graph.VertexSpace[n];
                foreach (EDGE e in node._Successors)
                {
                    yield return e.To;
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
            GraphLinkedList<NAME, NODE, EDGE> graph;
            NAME name;

            public ReverseSuccessorEnumerator(GraphLinkedList<NAME, NODE, EDGE> g, NAME n)
            {
                graph = g;
                name = n;
            }

            public IEnumerator<NAME> GetEnumerator()
            {
                int n = graph.NameSpace.BijectFromBasetype(name);
                NODE node = graph.VertexSpace[n];
                for (int i = node._Successors.Count - 1; i >= 0; --i) 
                {
                    GraphLinkedList<NAME, NODE, EDGE>.Edge e = node._Successors[i];
                    yield return e.To;
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

        public bool IsLeaf(NAME name)
        {
            int n = this.NameSpace.BijectFromBasetype(name);
            NODE node = this.VertexSpace[n];
            if (node._Successors.Count == 0)
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

            public GraphLinkedList<NAME, NODE, EDGE> _Graph
            {
                get;
                set;
            }

            public Vertex()
            {
                _Predecessors = new List<Edge>();
                _Successors = new List<Edge>();
            }

            public Vertex(NAME t)
            {
                this._Successors = new List<Edge>();
                this._Predecessors = new List<Edge>();
                this.Name = t;
            }

            override public string ToString()
            {
                return Name.ToString();
            }

            public List<Edge> _Predecessors
            {
                get;
                set;
            }

            public List<Edge> _Successors
            {
                get;
                set;
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

    public class GraphLinkedListVertex<NAME>
        : GraphLinkedList<NAME, GraphLinkedListVertex<NAME>, GraphLinkedListEdge<NAME>>.Vertex
    {
    }

    public class GraphLinkedListEdge<NAME>
        : GraphLinkedList<NAME, GraphLinkedListVertex<NAME>, GraphLinkedListEdge<NAME>>.Edge
    {
    }

    public class GraphLinkedList<NAME>
        : GraphLinkedList<NAME,
            GraphLinkedListVertex<NAME>,
            GraphLinkedListEdge<NAME>>
    {
    }
}
