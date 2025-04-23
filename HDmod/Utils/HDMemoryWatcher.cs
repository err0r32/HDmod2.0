using System;
using System.Diagnostics;
using System.Threading;
using Barotrauma;

namespace BarotraumaHD
{
    /// <summary>
    /// Мониторинг использования памяти с автоматической оптимизацией
    /// </summary>
    public sealed class HDMemoryWatcher : IDisposable
    {
        private const int WARNING_THRESHOLD_MB = 3500; // 3.5GB
        private const int CRITICAL_THRESHOLD_MB = 4000; // 4GB
        private const int CHECK_INTERVAL_MS = 10000; // 10 секунд
        
        private readonly HDTextureCache _textureCache;
        private readonly Thread _monitorThread;
        private bool _isRunning;
        private bool _disposed;

        public event Action<int> OnMemoryWarning; 
        public event Action<int> OnMemoryCritical;

        public HDMemoryWatcher(HDTextureCache textureCache)
        {
            _textureCache = textureCache ?? throw new ArgumentNullException(nameof(textureCache));
            _monitorThread = new Thread(MonitorLoop)
            {
                Name = "HDMemoryWatcher",
                Priority = ThreadPriority.BelowNormal
            };
        }

        public void Start()
        {
            if (_isRunning) return;
            
            _isRunning = true;
            _monitorThread.Start();
            HDMod.Log("Memory watcher started");
        }

        public void Stop()
        {
            _isRunning = false;
            _monitorThread.Join(500);
        }

        private void MonitorLoop()
        {
            while (_isRunning && !_disposed)
            {
                try
                {
                    var memoryUsage = GetCurrentMemoryUsageMB();
                    
                    if (memoryUsage >= CRITICAL_THRESHOLD_MB)
                    {
                        HandleCriticalMemory(memoryUsage);
                    }
                    else if (memoryUsage >= WARNING_THRESHOLD_MB)
                    {
                        HandleWarningMemory(memoryUsage);
                    }

                    Thread.Sleep(CHECK_INTERVAL_MS);
                }
                catch (ThreadInterruptedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    HDMod.Error($"Memory monitor error: {ex.Message}");
                    Thread.Sleep(30000); // Подождать 30 сек после ошибки
                }
            }
        }

        private void HandleWarningMemory(int memoryUsage)
        {
            OnMemoryWarning?.Invoke(memoryUsage);
            
            // Умеренная оптимизация
            _textureCache.SetMaxSize(1500); // Уменьшаем кэш до 1.5GB
            HDMod.Log($"Memory warning ({memoryUsage}MB), reduced cache size");
        }

        private void HandleCriticalMemory(int memoryUsage)
        {
            OnMemoryCritical?.Invoke(memoryUsage);
            
            // Агрессивная оптимизация
            _textureCache.SetMaxSize(800); // Уменьшаем кэш до 800MB
            GC.Collect(2, GCCollectionMode.Forced);
            
            HDMod.Log($"Memory critical ({memoryUsage}MB), forced cleanup");
        }

        private int GetCurrentMemoryUsageMB()
        {
            using var proc = Process.GetCurrentProcess();
            return (int)(proc.PrivateMemorySize64 / (1024 * 1024));
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            Stop();
            
            OnMemoryWarning = null;
            OnMemoryCritical = null;
        }
    }
}

/*
HDMain.cs
var memoryWatcher = new HDMemoryWatcher(textureCache);
memoryWatcher.OnMemoryWarning += usage => 
    HDMod.Log($"Warning: High memory usage {usage}MB");
memoryWatcher.OnMemoryCritical += usage => 
    HDMod.Log($"CRITICAL: Memory usage {usage}MB");
memoryWatcher.Start();

HDTextureManager
public void AdjustCacheBasedOnMemory(int availableMemoryMB)
{
    int newSize = availableMemoryMB / 2; // Используем половину доступной памяти
    _cache.SetMaxSize(Math.Max(512, newSize)); // Минимум 512MB
}
*/