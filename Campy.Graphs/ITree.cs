﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Campy.Graphs
{
    public interface ITree<T> : IGraph<T>
    {
        ITreeVertex<T> _Root
        {
            get;
            set;
        }
    }
}
