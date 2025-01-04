namespace PlayniteSounds.GeneratedFactories
{
    public interface IFactory<T>
    {
        T Create();
        void Release(T component);
    }
}
