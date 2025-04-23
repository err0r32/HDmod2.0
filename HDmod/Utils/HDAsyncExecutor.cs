using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Barotrauma;

namespace BarotraumaHD
{
    /// <summary>
    /// Система управления асинхронными задачами с приоритетами и ограничением параллелизма
    /// </summary>
    public sealed class HDAsyncExecutor : IDisposable
    {
        private class AsyncTask
        {
            public Func<Task> Action;
            public TaskPriority Priority;
            public TaskCompletionSource<bool> CompletionSource;
        }

        public enum TaskPriority
        {
            Low,
            Normal,
            High,
            Critical
        }

        private readonly ConcurrentQueue<AsyncTask>[] _priorityQueues;
        private readonly SemaphoreSlim _workSemaphore;
        private readonly CancellationTokenSource _cts;
        private bool _isRunning;
        private bool _disposed;

        /// <summary>
        /// Максимальное количество параллельных задач
        /// </summary>
        public int MaxConcurrentTasks { get; }

        /// <summary>
        /// Количество ожидающих задач
        /// </summary>
        public int PendingTasks => 
            _priorityQueues.Sum(q => q.Count);

        public HDAsyncExecutor(int maxConcurrentTasks = 4)
        {
            MaxConcurrentTasks = maxConcurrentTasks;
            _priorityQueues = new ConcurrentQueue<AsyncTask>[Enum.GetValues(typeof(TaskPriority)).Length];
            
            for (int i = 0; i < _priorityQueues.Length; i++)
            {
                _priorityQueues[i] = new ConcurrentQueue<AsyncTask>();
            }

            _workSemaphore = new SemaphoreSlim(maxConcurrentTasks, maxConcurrentTasks);
            _cts = new CancellationTokenSource();
            _isRunning = true;

            // Запуск обработчика задач
            Task.Run(ProcessTasks);
        }

        /// <summary>
        /// Добавляет задачу в очередь выполнения
        /// </summary>
        public Task EnqueueAsync(Func<Task> action, TaskPriority priority = TaskPriority.Normal)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(HDAsyncExecutor));

            var task = new AsyncTask
            {
                Action = action,
                Priority = priority,
                CompletionSource = new TaskCompletionSource<bool>()
            };

            _priorityQueues[(int)priority].Enqueue(task);
            return task.CompletionSource.Task;
        }

        /// <summary>
        /// Основной цикл обработки задач
        /// </summary>
        private async Task ProcessTasks()
        {
            while (_isRunning)
            {
                try
                {
                    // Поиск задачи с наивысшим приоритетом
                    AsyncTask task = null;
                    for (int i = _priorityQueues.Length - 1; i >= 0; i--)
                    {
                        if (_priorityQueues[i].TryDequeue(out task))
                        {
                            break;
                        }
                    }

                    if (task != null)
                    {
                        await _workSemaphore.WaitAsync(_cts.Token);
                        
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await task.Action();
                                task.CompletionSource.SetResult(true);
                            }
                            catch (Exception ex)
                            {
                                HDMod.Error($"Async task failed: {ex.Message}");
                                task.CompletionSource.SetException(ex);
                            }
                            finally
                            {
                                _workSemaphore.Release();
                            }
                        }, _cts.Token);
                    }
                    else
                    {
                        await Task.Delay(10); // Небольшая пауза при отсутствии задач
                    }
                }
                catch (OperationCanceledException)
                {
                    // Игнорируем отмену
                }
                catch (Exception ex)
                {
                    HDMod.Error($"Task processor error: {ex.Message}");
                    await Task.Delay(1000); // Пауза при ошибке
                }
            }
        }

        /// <summary>
        /// Ожидает завершения всех текущих задач
        /// </summary>
        public async Task DrainAsync()
        {
            while (PendingTasks > 0 || _workSemaphore.CurrentCount < MaxConcurrentTasks)
            {
                await Task.Delay(50);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _isRunning = false;
            _cts.Cancel();
            _workSemaphore.Dispose();
            _cts.Dispose();
            _disposed = true;
        }
    }
}

/*
// Инициализация
var executor = new HDAsyncExecutor(maxConcurrentTasks: 2);

// Добавление задач с разными приоритетами
var highPriorityTask = executor.EnqueueAsync(async () => 
{
    await Task.Delay(500);
    HDMod.Log("High priority task completed");
}, HDAsyncExecutor.TaskPriority.High);

var normalTask = executor.EnqueueAsync(async () => 
{
    await Task.Delay(1000);
    HDMod.Log("Normal task completed");
});

// Ожидание завершения
await highPriorityTask;

// Очистка
executor.Dispose();
*/