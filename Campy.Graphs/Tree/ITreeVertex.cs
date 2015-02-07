using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphs
{
    public interface ITreeVertex : IVertex
    {
        List<IVertex> Children
        {
            get;
            set;
        }

        IVertex Parent
        {
            get;
        }
    }
}
