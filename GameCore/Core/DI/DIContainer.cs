using System;
using System.Collections.Generic;

namespace Core.DI
{
    /// <summary>
    /// Минимальный DI-контейнер: регистрация синглтон-инстансов и фабрик по (tag, type).
    /// Поддерживает родительский контейнер (root -> scene).
    /// </summary>
    public class DIContainer : IDisposable
    {
        private readonly DIContainer _parent;
        private readonly Dictionary<(string, Type), object> _instances = new();
        private readonly Dictionary<(string, Type), Func<DIContainer, object>> _factories = new();

        public DIContainer(DIContainer parent = null) => _parent = parent;

        public void RegisterInstance<T>(T instance) => RegisterInstance(null, instance);

        public void RegisterInstance<T>(string tag, T instance)
        {
            var key = (tag, typeof(T));
            if (_instances.ContainsKey(key))
                throw new Exception($"DI: instance ({tag}, {typeof(T).Name}) уже зарегистрирован");
            _instances[key] = instance;
        }

        public void RegisterFactory<T>(Func<DIContainer, T> factory) => RegisterFactory(null, factory);

        public void RegisterFactory<T>(string tag, Func<DIContainer, T> factory)
        {
            var key = (tag, typeof(T));
            if (_factories.ContainsKey(key))
                throw new Exception($"DI: factory ({tag}, {typeof(T).Name}) уже зарегистрирована");
            _factories[key] = c => factory(c);
        }

        public T Resolve<T>(string tag = null)
        {
            var key = (tag, typeof(T));

            if (_instances.TryGetValue(key, out var inst))
                return (T)inst;

            if (_factories.TryGetValue(key, out var factory))
            {
                var created = (T)factory(this);
                _instances[key] = created; // кешируем как синглтон
                return created;
            }

            if (_parent != null)
                return _parent.Resolve<T>(tag);

            throw new Exception($"DI: зависимость не найдена ({tag}, {typeof(T).Name})");
        }

        public bool TryResolve<T>(out T value, string tag = null)
        {
            var key = (tag, typeof(T));
            if (_instances.TryGetValue(key, out var inst)) { value = (T)inst; return true; }
            if (_factories.ContainsKey(key)) { value = Resolve<T>(tag); return true; }
            if (_parent != null) return _parent.TryResolve(out value, tag);
            value = default;
            return false;
        }

        public void Dispose()
        {
            foreach (var inst in _instances.Values)
                (inst as IDisposable)?.Dispose();
            _instances.Clear();
            _factories.Clear();
        }
    }
}
