﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Campy.Graphs
{
    public class BinaryTreeAdjList<NAME> : TreeAdjList<NAME>
    {
        /// <summary>
        /// Constructor, and creates a predefined tree (see below).
        /// </summary>
        public BinaryTreeAdjList()
            : base()
        {
            NODE = typeof(BinaryTreeAdjListVertex<NAME>);
            EDGE = typeof(BinaryTreeAdjListEdge<NAME>);
        }

        //public void Sanity()
        //{
        //    // Check sanity of a complete tree.
        //    // 1. Check there is one and only one root.
        //    int count_roots = 0;
        //    foreach (Vertex v in this.Vertices)
        //    {
        //        Vertex tv = v as Vertex;
        //        if (tv.Parent == null)
        //            count_roots++;
        //    }
        //    if (count_roots != 1 && this.Vertices.Count() != 0)
        //        throw new Exception("Tree malformed -- there are " + count_roots + " roots.");

        //    // 2. Check each node that is has one parent, except for root, and that it's equal to predecessor list.
        //    foreach (Vertex v in this.Vertices)
        //    {
        //        if (v.Parent == null && this.Root != v)
        //            throw new Exception("Tree malformed -- node without a parent that isn't the root.");
        //        if (v.Predecessors.Count() > 1)
        //            throw new Exception("Tree malformed -- predecessor count greater than one.");
        //        if (v.Predecessors.Count() == 0 && this.Root != v)
        //            throw new Exception("Tree malformed -- node without a parent that isn't the root.");
        //        if (v.Predecessors.Count() != 0 && v.Predecessors.First() != v.Parent)
        //            throw new Exception("Tree malformed -- node predecessor and parent are inconsistent.");
        //    }

        //    // 3. Go through edge list and verify.
        //    Dictionary<GraphAdjList<NAME, NODE, EDGE>.Vertex, bool> seen = new Dictionary<GraphAdjList<NAME, NODE, EDGE>.Vertex, bool>();
        //    foreach (Edge e in this.Edges)
        //    {
        //        if (!seen.ContainsKey(e.To))
        //            seen.Add(e.To, true);
        //        else
        //        {
        //            throw new Exception("Tree malformed -- Visited more than once.");
        //        }
        //    }
        //}

        //public GraphAdjList<NAME> LeftContour(Vertex<NAME> v)
        //{
        //    // Create a graph with the left contour of v.
        //    GraphAdjList<NAME> lc = new GraphAdjList<NAME>();

        //    // Create left contour.
        //    int llevel = 1;
        //    Vertex<NAME> left = v.GetLeftMost(0, llevel);
        //    Vertex<NAME> cloneleft = lc.CloneVertex(v);
        //    Vertex<NAME> llast = v;
        //    Vertex<NAME> clonellast = cloneleft;
        //    while (left != null)
        //    {
        //        cloneleft = lc.CloneVertex((Vertex<NAME>)left);
        //        lc.AddEdge(clonellast, cloneleft);

        //        llevel++;
        //        llast = left;
        //        clonellast = cloneleft;
        //        left = v.GetLeftMost(0, llevel);
        //    }

        //    return lc;
        //}

        //public GraphAdjList<NAME> RightContour(Vertex<NAME> v)
        //{
        //    // Create a graph with the right contour of v.
        //    GraphAdjList<NAME> rc = new GraphAdjList<NAME>();

        //    rc.CloneVertex(v);

        //    // Create right contour.
        //    int rlevel = 1;
        //    Vertex<NAME> right = v.GetRightMost(0, rlevel);
        //    Vertex<NAME> cloneright = rc.CloneVertex(v);
        //    Vertex<NAME> rlast = v;
        //    Vertex<NAME> clonerlast = cloneright;
        //    while (right != null)
        //    {
        //        cloneright = rc.CloneVertex((Vertex<NAME>)right);
        //        rc.AddEdge(clonerlast, cloneright);

        //        rlevel++;
        //        rlast = right;
        //        clonerlast = cloneright;
        //        right = v.GetRightMost(0, rlevel);
        //    }
        //    return rc;
        //}

        //int height(NAME v, int d)
        //{
        //    return 0;
        //    //if (v == null)
        //    //    return d;
        //    //int m = d;
        //    //foreach (Vertex u in v.Successors)
        //    //{
        //    //    int x = this.height(u, d + 1);
        //    //    if (x > m)
        //    //        m = x;
        //    //}
        //    //return m;
        //}

        //public int Height()
        //{
        //    return this.height(this.Root, 1);
        //}

        //public Vertex<NAME> CloneVertex(Vertex<NAME> other)
        //{
        //    Vertex<NAME> v = new Vertex<NAME>();
        //    v.Copy(other);
        //    if (v.Parent == null)
        //        this.Root = v;
        //    return v;
        //}

    }

    public class BinaryTreeAdjListVertex<NAME> : TreeAdjListVertex<NAME>
    {
        public BinaryTreeAdjListVertex<NAME> Left
        {
            get;
            set;
        }

        public BinaryTreeAdjListVertex<NAME> Right
        {
            get;
            set;
        }

        public BinaryTreeAdjListVertex()
        {
        }

        public BinaryTreeAdjListVertex(NAME t)
            : base(t)
        {
        }

    }

    public class BinaryTreeAdjListEdge<NAME> : TreeAdjListEdge<NAME>
    {
        public BinaryTreeAdjListEdge()
            : base()
        {
        }

        public BinaryTreeAdjListEdge(GraphAdjListVertex<NAME> f, GraphAdjListVertex<NAME> t)
            : base(f, t)
        {
        }
    }

}
