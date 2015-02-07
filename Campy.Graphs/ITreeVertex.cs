using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewGraphs
{
    public interface ITreeVertex<T> : IVertex<T>
    {
        List<IVertex<T>> Children
        {
            get;
            set;
        }

        IVertex<T> Parent
        {
            get;
        }
    }
}
