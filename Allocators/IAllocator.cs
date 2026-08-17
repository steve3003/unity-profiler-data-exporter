using System.Collections.Generic;

namespace ProfilerDataExporter
{
    public interface IAllocator<T>
    {
        T Allocate();
        void Free(T element);
        void Free(IList<T> elements);
    }
}

