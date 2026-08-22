using System;
using System.Collections.Generic;

namespace ProjectDelta.Infrastructure
{
    public sealed class ServiceRegistry
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public void Register<TService>(TService instance)
        {
            _services[typeof(TService)] = instance;
        }

        public TService Get<TService>()
        {
            return (TService)_services[typeof(TService)];
        }

        public bool TryGet<TService>(out TService service)
        {
            if (_services.TryGetValue(typeof(TService), out var raw))
            {
                service = (TService)raw;
                return true;
            }

            service = default;
            return false;
        }
    }
}
