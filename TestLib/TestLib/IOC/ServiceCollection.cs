using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLib.IOC
{
    public class ServiceCollection : IServiceCollection
    {
        private readonly Dictionary<Type, ServiceType> _serviceTypes = new Dictionary<Type, ServiceType>();
        private readonly Dictionary<string, Type> _keyTypes = new Dictionary<string, Type>();

        public void AddSingleTon<TInterface, TImplementation>() where TImplementation : TInterface
        {
            _keyTypes[typeof(TInterface).Name] = typeof(TInterface);
            _serviceTypes[typeof(TInterface)] = Crea
        }

        public void AddSingleTon<TImplementation>() where TImplementation : class
        {
            throw new NotImplementedException();
        }

        public void AddTransient<TInterface, TImplementation>() where TImplementation : TInterface
        {
            throw new NotImplementedException();
        }

        public void AddTransient<TImplementation>() where TImplementation : class
        {
            throw new NotImplementedException();
        }

        public bool CheckType(Type type)
        {
            throw new NotImplementedException();
        }

        public IServiceContainer CreateContainer()
        {
            return new ServiceContainer(this);
        }

        public ServiceType GetType(Type type)
        {
            throw new NotImplementedException();
        }

        public Type KeyType(string name)
        {
            throw new NotImplementedException();
        }
    }
}
