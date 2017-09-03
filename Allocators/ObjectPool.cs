using System;
using System.Collections.Generic;

namespace ProfilerDataExporter
{
    public class ObjectPool<T> : IAllocator<T>
    {
        private List<T> allElements;
        private Stack<T> freeElements;
        private IFactory<T> factory;

        public ObjectPool(IFactory<T> factory, int initialSize)
        {
            this.factory = factory;
            allElements = new List<T>(initialSize);
            freeElements = new Stack<T>(initialSize);

            for (int i = 0; i < initialSize; ++i)
            {
                var element = CreateElement();
                freeElements.Push(element);
            }
        }

        private T CreateElement()
        {
            var element = factory.Create();
            allElements.Add(element);
            return element;
        }

        // IAllocator<T> implementation
        public T Allocate()
        {
            if (freeElements.Count > 0)
            {
                return freeElements.Pop();
            }

            return CreateElement();
        }

        // IAllocator<T> implementation
        public virtual void Free(T element)
        {
            freeElements.Push(element);
        }

        // IAllocator<T> implementation
        public void Free(IList<T> elements)
        {
            for (int i = 0; i < elements.Count; ++i)
            {
                var list = elements[i];
                Free(list);
            }
        }
    }
}

