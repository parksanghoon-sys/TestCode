using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLib.IOC
{
    public interface IServiceContainer
    {
        public TInterface GetService<TInterface>() where TInterface : class;
        public object GetService(Type serviceType);
        public Type KeyGet(string key); 
    }
}
