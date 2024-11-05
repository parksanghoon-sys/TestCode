using Microservice.Application.Repositories;
using Microservice.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.Persistence
{
    public static class PersistenceServiceRegistrations
    {
        public static IServiceCollection AddAPersistenceServiceService(this IServiceCollection services)
        {
            services.AddScoped(typeof(IGenericRepository<>) , typeof(GenericRepository<>));

            


            return services;
        }
    }

}
