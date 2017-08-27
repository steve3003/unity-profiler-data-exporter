using System;
using System.Collections.Generic;

namespace ProfilerDataExporter
{
    public class ListFactory<T> : IFactory<List<T>>, IFactory<IEnumerable<T>>
    {
        private int size;

        public ListFactory(int size)
        {
            this.size = size;
        }

        List<T> IFactory<List<T>>.Create()
        {
            return Create();
        }

        IEnumerable<T> IFactory<IEnumerable<T>>.Create()
        {
            return Create();
        }

        public List<T> Create()
        {
            return new List<T>(size);
        }
    }
}

