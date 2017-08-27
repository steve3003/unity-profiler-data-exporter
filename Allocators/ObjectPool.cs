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

        T IAllocator<T>.Allocate()
        {
            if (freeElements.Count > 0)
            {
                return freeElements.Pop();
            }

            return CreateElement();
        }

        void IAllocator<T>.Free(T element)
        {
            freeElements.Push(element);
        }
    }
}

