using System;
using System.Collections.Generic;

namespace ProfilerDataExporter
{
    public class ListPool<T> : ObjectPool<List<T>>
    {
        public ListPool(IFactory<List<T>> factory, int initialSize) : base(factory, initialSize)
        {
        }

        public override void Free(List<T> element)
        {
            base.Free(element);

            element.Clear();
        }
    }
}

