using Microsoft.EntityFrameworkCore;

namespace cliProgressExample
{
    public partial class Repository<T> where T : class
    {
        public class TaketRepository : Repository<Ticket>
        {
            public TaketRepository(DbContext dbContext)
                :base(dbContext)
            {
                
            }
        }
    }
}
