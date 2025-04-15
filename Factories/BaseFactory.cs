#if UNITY_EDITOR
namespace ProfilerDataExporter
{
    public class BaseFactory<T> : IFactory<T>
        where T : new()
    {
        // IFactory<T> implementation
        public T Create()
        {
            return new T();
        }
    }
}
#endif
