using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLib.IOC
{
    public interface IServiceCollection
    {
        public void AddSingleTon<TInterface, TImplementation>() where TImplementation : TInterface;
        public void AddTransient<TInterface, TImplementation>() where TImplementation : TInterface;
        public void AddSingleTon<TImplementation>() where TImplementation : class;
        public void AddTransient<TImplementation>() where TImplementation : class;

        public bool CheckType(Type type);
        public Type KeyType(string name);
        public ServiceType GetType(Type type);
        public IServiceContainer CreateContainer();
    }
    public class ServiceType
    {
        public Type? Type { get; set; }
        public bool IsSingleton { get; set; }
        public object? Prameter { get; set; }
        public object? CalbakcFunc { get; set; }
    }
}
