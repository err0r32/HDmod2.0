using System;
using System.Collections.Generic;

namespace BarotraumaHD
{
    /// <summary>
    /// Контейнер служб для управления зависимостями в HD-моде
    /// </summary>
    public static class HDServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private static readonly Dictionary<Type, Func<object>> _serviceFactories = new Dictionary<Type, Func<object>>();

        #region Регистрация сервисов
        
        /// <summary>
        /// Регистрирует экземпляр сервиса
        /// </summary>
        /// <typeparam name="T">Тип сервиса</typeparam>
        /// <param name="service">Экземпляр сервиса</param>
        /// <exception cref="ArgumentNullException">Если service равен null</exception>
        /// <exception cref="InvalidOperationException">Если сервис уже зарегистрирован</exception>
        public static void Register<T>(T service) where T : class
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            var type = typeof(T);
            if (_services.ContainsKey(type))
                throw new InvalidOperationException($"Сервис {type.Name} уже зарегистрирован");

            _services[type] = service;
            HDMod.Log($"Сервис зарегистрирован: {type.Name}");
        }

        /// <summary>
        /// Регистрирует фабрику для создания сервиса
        /// </summary>
        /// <typeparam name="T">Тип сервиса</typeparam>
        /// <param name="factory">Фабрика сервиса</param>
        /// <exception cref="ArgumentNullException">Если factory равен null</exception>
        public static void Register<T>(Func<T> factory) where T : class
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var type = typeof(T);
            _serviceFactories[type] = factory;
            HDMod.Log($"Фабрика сервиса зарегистрирована: {type.Name}");
        }

        #endregion

        #region Получение сервисов

        /// <summary>
        /// Получает экземпляр сервиса
        /// </summary>
        /// <typeparam name="T">Тип сервиса</typeparam>
        /// <returns>Экземпляр сервиса</returns>
        /// <exception cref="InvalidOperationException">Если сервис не найден</exception>
        public static T Get<T>() where T : class
        {
            var type = typeof(T);

            // Пытаемся получить существующий экземпляр
            if (_services.TryGetValue(type, out var service))
                return (T)service;

            // Пытаемся создать через фабрику
            if (_serviceFactories.TryGetValue(type, out var factory))
            {
                var newService = factory();
                _services[type] = newService; // Кэшируем результат
                return (T)newService;
            }

            throw new InvalidOperationException($"Сервис {type.Name} не зарегистрирован");
        }

        /// <summary>
        /// Пытается получить экземпляр сервиса
        /// </summary>
        /// <typeparam name="T">Тип сервиса</typeparam>
        /// <param name="service">Экземпляр сервиса</param>
        /// <returns>True если сервис найден</returns>
        public static bool TryGet<T>(out T service) where T : class
        {
            try
            {
                service = Get<T>();
                return true;
            }
            catch
            {
                service = null;
                return false;
            }
        }

        #endregion

        #region Управление жизненным циклом

        /// <summary>
        /// Очищает все зарегистрированные сервисы (вызывает Dispose для IDisposable)
        /// </summary>
        public static void Clear()
        {
            foreach (var service in _services.Values)
            {
                if (service is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                        HDMod.Log($"Сервис освобожден: {service.GetType().Name}");
                    }
                    catch (Exception ex)
                    {
                        HDMod.Error($"Ошибка при освобождении сервиса {service.GetType().Name}: {ex.Message}");
                    }
                }
            }

            _services.Clear();
            _serviceFactories.Clear();
            HDMod.Log("Все сервисы очищены");
        }

        /// <summary>
        /// Удаляет конкретный сервис
        /// </summary>
        /// <typeparam name="T">Тип сервиса</typeparam>
        /// <returns>True если сервис был удален</returns>
        public static bool Remove<T>() where T : class
        {
            var type = typeof(T);
            bool removed = false;

            if (_services.TryGetValue(type, out var service))
            {
                if (service is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                        HDMod.Log($"Сервис освобожден: {type.Name}");
                    }
                    catch (Exception ex)
                    {
                        HDMod.Error($"Ошибка при освобождении сервиса {type.Name}: {ex.Message}");
                    }
                }

                removed = _services.Remove(type);
                _serviceFactories.Remove(type);
            }

            return removed;
        }

        #endregion
    }
}