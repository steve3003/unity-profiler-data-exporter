namespace ProfilerDataExporter
{
    public interface IFactory<T>
    {
        T Create();
    }
}

