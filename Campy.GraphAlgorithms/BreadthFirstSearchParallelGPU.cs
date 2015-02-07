using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewGraphs;
using Campy;
using Campy.Types;

namespace GraphAlgorithms
{
    public class BreadthFirstSearchParallelGPU<NAME>
    {
        GraphAdjList<NAME> graph;
        IEnumerable<NAME> Source;

        public BreadthFirstSearchParallelGPU(GraphAdjList<NAME> g, IEnumerable<NAME> s)
        {
            graph = g;
            Source = s;
        }

        bool Terminate = false;

        public void VisitNodes(Func<int, bool> help)
        {
            Func<int, bool> local_help = help;
            if (Source.Count() != 0)
            {
                // compress adjacency tables.
                graph.Optimize();

                // get bijection of NAME range since ints is what we will work with.
                int[] index = graph.adj.IndexSuccessors;
                int[] data = graph.adj.DataSuccessors;

                // Allocate visited.
                int size = index.Length;
                int[] visited = new int[size];
                for (int i = 0; i < size; ++i)
                    visited[i] = 255;

                Accelerator_View av = Accelerator.get_default_view();
                Array_View<int> visited_gpu = new Array_View<int>(size, ref visited);
                Array_View<int> xxxxxxxx = Array_View<int>.Default_Value;

                // Initialize visited.
                Extent e = new Extent(size);
                AMP.Parallel_For_Each(e, (Index idx) =>
                {
                    int x = idx[0];
                    visited_gpu[x] = 0;
                });
                av.wait();

                int[] current_frontier = new int[index.Length];
                int[] top_current_frontier = new int[1];
                int[] next_frontier = new int[index.Length];
                int[] top_next_frontier = new int[1];

                foreach (NAME v in Source)
                {
                    int vv = graph.NameSpace.BijectFromBasetype(v);
                    current_frontier[top_current_frontier[0]++] = vv;
                }

                Array_View<int> current_frontier_gpu = new Array_View<int>(index.Length, ref current_frontier);
                Array_View<int> top_current_frontier_gpu = new Array_View<int>(1, ref top_current_frontier);
                Array_View<int> next_frontier_gpu = new Array_View<int>(index.Length, ref next_frontier);
                Array_View<int> top_next_frontier_gpu = new Array_View<int>(1, ref top_next_frontier);
                Array_View<int> index_gpu = new Array_View<int>(index.Length, ref index);
                Array_View<int> data_gpu = new Array_View<int>(data.Length, ref data);

                bool use_current_frontier = true;
                int level = 0;
                while (top_current_frontier_gpu[0] != 0 || top_next_frontier_gpu[0] != 0)
                {
                    level++;
                    //System.Console.WriteLine("Level " + level);
                    if (use_current_frontier)
                    {
                        Extent p = new Extent(top_current_frontier_gpu[0]);
                        AMP.Parallel_For_Each(av, p, (Index idx) =>
                        {
                            int n = idx[0];
                            // Each thread looks at current frontier, but not over top.
                            int u = current_frontier_gpu[n];

                            // visit u...
                            if (local_help(u))
                                return;

                            for (int i = index_gpu[u]; i < index_gpu[u + 1]; ++i)
                            {
                                int v = data_gpu[i];
                                if (visited_gpu[v] == 0)
                                {
                                    visited_gpu[v] = 255;
                                    int t = AMP.Atomic_Fetch_Add(ref top_next_frontier_gpu, 0, 1);
                                    next_frontier_gpu[t] = v;
                                }
                            }
                        });
                        top_current_frontier_gpu[0] = 0;
                    } 
                    else
                    {
                        Extent p = new Extent(top_next_frontier_gpu[0]);
                        AMP.Parallel_For_Each(av, p, (Index idx) =>
                        {
                            int n = idx[0];
                            
                            // Each thread looks at current frontier, but not over top.
                            int u = next_frontier_gpu[n];

                            // visit u...
                            if (local_help(u))
                                return;

                            for (int i = index_gpu[u]; i < index_gpu[u + 1]; ++i)
                            {
                                int v = data_gpu[i];
                                if (visited_gpu[v] == 0)
                                {
                                    visited_gpu[v] = 255;
                                    int t = AMP.Atomic_Fetch_Add(ref top_current_frontier_gpu, 0, 1);
                                    current_frontier_gpu[t] = v;
                                }
                            }
                        });
                        top_next_frontier_gpu[0] = 0;
                    }
                    use_current_frontier = !use_current_frontier;
                }
            }
        }
    }
}
