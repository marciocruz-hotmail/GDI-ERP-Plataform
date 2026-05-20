using System;
using System.Collections.Generic;
using System.Web.Mvc;
using GdiPlataform.Lib.Lookups;

namespace GDI_ERP_Plataform.App_Start
{
    /// <summary>Registo MVC 5 de ILookupQueryService (Fase 2 LibDataSets).</summary>
    public static class LookupDependencyConfig
    {
        public static void Register()
        {
            var container = new LookupServiceContainer();
            container.Register(typeof(ILookupQueryService), () => new LookupQueryService());
            DependencyResolver.SetResolver(new LookupMvcDependencyResolver(container));
        }
    }

    internal sealed class LookupServiceContainer
    {
        private readonly Dictionary<Type, Func<object>> _factories = new Dictionary<Type, Func<object>>();

        public void Register(Type serviceType, Func<object> factory)
        {
            _factories[serviceType] = factory;
        }

        public object GetService(Type serviceType)
        {
            Func<object> factory;
            return _factories.TryGetValue(serviceType, out factory) ? factory() : null;
        }
    }

    internal sealed class LookupMvcDependencyResolver : IDependencyResolver
    {
        private readonly LookupServiceContainer _container;

        public LookupMvcDependencyResolver(LookupServiceContainer container)
        {
            _container = container;
        }

        public object GetService(Type serviceType) => _container.GetService(serviceType);

        public IEnumerable<object> GetServices(Type serviceType)
        {
            var service = GetService(serviceType);
            return service != null ? new[] { service } : Array.Empty<object>();
        }
    }
}
