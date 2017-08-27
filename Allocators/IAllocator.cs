namespace ProfilerDataExporter
{
    public interface IAllocator<T>
    {
        T Allocate();
        void Free(T element);
    }
}

