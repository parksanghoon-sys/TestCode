namespace WebApplication1.Store
{
    // 3. 저장소 인터페이스와 구현체를 정의합니다.
    public interface IStore<T>
    {
        T GetById(int id);
        IEnumerable<T> GetAll();
        void Add(T   user);
        void Update(T user);
        void Delete(int id);        
    }
}
