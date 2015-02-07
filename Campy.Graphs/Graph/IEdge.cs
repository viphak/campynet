using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphs
{
    public interface IEdge<NAME>
    {
        NAME From
        {
            get;
        }

        NAME To
        {
            get;
        }
    }
}
