using System;
using System.Collections.Generic;

namespace MingDynasty.OpenToolkit.Core
{
    public interface IServiceRegistry
    {
        void Register<TService>(TService service);
        bool TryResolve<TService>(out TService service);
        TService Resolve<TService>();
        bool Unregister<TService>();
    }

    /// <summary>
    /// Explicit, instance-scoped service registry used by the bootstrap composition root.
    /// </summary>
    public sealed class ServiceRegistry : IServiceRegistry
    {
        private readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

        public void Register<TService>(TService service)
        {
            if (ReferenceEquals(service, null))
            {
                throw new ArgumentNullException(nameof(service));
            }

            Type serviceType = typeof(TService);
            if (services.ContainsKey(serviceType))
            {
                throw new InvalidOperationException("A service is already registered for " + serviceType.FullName + ".");
            }

            services.Add(serviceType, service);
        }

        public bool TryResolve<TService>(out TService service)
        {
            object value;
            if (services.TryGetValue(typeof(TService), out value))
            {
                service = (TService)value;
                return true;
            }

            service = default(TService);
            return false;
        }

        public TService Resolve<TService>()
        {
            TService service;
            if (!TryResolve(out service))
            {
                throw new InvalidOperationException("No service is registered for " + typeof(TService).FullName + ".");
            }

            return service;
        }

        public bool Unregister<TService>()
        {
            return services.Remove(typeof(TService));
        }
    }
}
