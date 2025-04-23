using System;
using System.Collections.Generic;
using System.Linq;

namespace BarotraumaHD
{
    /// <summary>
    /// Система управления событиями HD-мода с поддержкой приоритетов и отписки
    /// </summary>
    public static class HDEventDispatcher
    {
        #region Вложенные типы
        /// <summary>
        /// Делегат событий мода с поддержкой отмены
        /// </summary>
        /// <param name="args">Аргументы события</param>
        /// <returns>True если событие обработано и дальнейшая обработка не нужна</returns>
        public delegate bool HDEventDelegate<in T>(T args) where T : HDEventArgs;

        private class EventHandlerWrapper<T> where T : HDEventArgs
        {
            public HDEventDelegate<T> Handler { get; }
            public int Priority { get; }
            public string SubscriberName { get; }

            public EventHandlerWrapper(HDEventDelegate<T> handler, int priority, string subscriberName)
            {
                Handler = handler;
                Priority = priority;
                SubscriberName = subscriberName;
            }
        }
        #endregion

        #region Приватные поля
        private static readonly Dictionary<Type, object> _eventHandlers = new Dictionary<Type, object>();
        private static readonly Dictionary<string, List<Delegate>> _subscriberBindings = new Dictionary<string, List<Delegate>>();
        #endregion

        #region Публичные методы
        /// <summary>
        /// Подписаться на событие с указанием приоритета
        /// </summary>
        /// <typeparam name="T">Тип аргументов события</typeparam>
        /// <param name="handler">Обработчик</param>
        /// <param name="priority">Приоритет (чем выше, тем раньше вызов)</param>
        /// <param name="subscriberName">Имя подписчика (для отладки)</param>
        public static void Subscribe<T>(HDEventDelegate<T> handler, int priority = 0, string subscriberName = null) where T : HDEventArgs
        {
            var eventType = typeof(T);
            if (!_eventHandlers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<EventHandlerWrapper<T>>();
                _eventHandlers.Add(eventType, handlers);
            }

            var typedHandlers = (List<EventHandlerWrapper<T>>)handlers;
            typedHandlers.Add(new EventHandlerWrapper<T>(handler, priority, subscriberName));
            
            // Сортируем по приоритету (обратный порядок)
            typedHandlers.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            // Регистрируем для отписки по имени
            if (!string.IsNullOrEmpty(subscriberName))
            {
                if (!_subscriberBindings.TryGetValue(subscriberName, out var delegates))
                {
                    delegates = new List<Delegate>();
                    _subscriberBindings.Add(subscriberName, delegates);
                }
                delegates.Add(handler);
            }
        }

        /// <summary>
        /// Отписать все обработчики для указанного подписчика
        /// </summary>
        public static void Unsubscribe(string subscriberName)
        {
            if (_subscriberBindings.TryGetValue(subscriberName, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    UnsubscribeInternal(handler);
                }
                _subscriberBindings.Remove(subscriberName);
            }
        }

        /// <summary>
        /// Отписать конкретный обработчик
        /// </summary>
        public static void Unsubscribe<T>(HDEventDelegate<T> handler) where T : HDEventArgs
        {
            UnsubscribeInternal(handler);
        }

        /// <summary>
        /// Вызвать событие с обработкой всех подписчиков
        /// </summary>
        /// <returns>True если событие было обработано (хотя бы одним подписчиком)</returns>
        public static bool Raise<T>(T args) where T : HDEventArgs
        {
            if (_eventHandlers.TryGetValue(typeof(T), out var handlers))
            {
                foreach (var wrapper in (List<EventHandlerWrapper<T>>)handlers)
                {
                    try
                    {
                        if (wrapper.Handler(args))
                        {
                            args.IsHandled = true;
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        HDMod.Error($"Ошибка в обработчике события {typeof(T).Name} от {wrapper.SubscriberName}: {ex.Message}");
                    }
                }
            }
            return args.IsHandled;
        }
        #endregion

        #region Приватные методы
        private static void UnsubscribeInternal(Delegate handler)
        {
            foreach (var eventType in _eventHandlers.Keys.ToList())
            {
                var handlersList = _eventHandlers[eventType];
                var handlerType = handler.GetType();
                var genericType = handlerType.GetGenericArguments()[0];

                if (genericType == eventType.GetGenericArguments()[0])
                {
                    var removeMethod = typeof(List<>)
                        .MakeGenericType(typeof(EventHandlerWrapper<>).MakeGenericType(genericType))
                        .GetMethod("RemoveAll");

                    var predicate = new Func<object, bool>(wrapper =>
                    {
                        var handlerProperty = wrapper.GetType().GetProperty("Handler");
                        return handlerProperty.GetValue(wrapper) == handler;
                    });

                    removeMethod.Invoke(handlersList, new object[] { predicate });
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// Базовый класс для аргументов событий
    /// </summary>
    public abstract class HDEventArgs : EventArgs
    {
        /// <summary>
        /// Было ли событие обработано
        /// </summary>
        public bool IsHandled { get; set; }

        /// <summary>
        /// Можно ли отменить событие
        /// </summary>
        public bool IsCancellable { get; protected set; }
    }
}