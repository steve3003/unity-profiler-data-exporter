#if UNITY_EDITOR
namespace ProfilerDataExporter
{
    public interface IFactory<T>
    {
        T Create();
    }
}
#endif
