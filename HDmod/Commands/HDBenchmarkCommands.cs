using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Barotrauma;
using Microsoft.Xna.Framework;

namespace BarotraumaHD
{
    /// <summary>
    /// Команды для тестирования производительности и диагностики
    /// </summary>
    public static class HDBenchmarkCommands
    {
        private static bool _benchmarkRunning;
        private static Stopwatch _benchmarkTimer;
        private static int _benchmarkIterations;
        private static int _currentIteration;
        
        /// <summary>
        /// Регистрация всех benchmark-команд
        /// </summary>
        public static void RegisterCommands()
        {
            CommandUtils.RegisterCommand(
                "hd_benchmark_textures", 
                "Run texture loading benchmark", 
                args => RunTextureBenchmark(args));
                
            CommandUtils.RegisterCommand(
                "hd_benchmark_memory", 
                "Run memory usage benchmark", 
                args => RunMemoryBenchmark(args));
                
            CommandUtils.RegisterCommand(
                "hd_benchmark_cancel", 
                "Cancel running benchmark", 
                args => CancelBenchmark());
        }
        
        /// <summary>
        /// Benchmark загрузки текстур
        /// </summary>
        private static void RunTextureBenchmark(string[] args)
        {
            if (_benchmarkRunning)
            {
                HDMod.Message("Benchmark already running!", Color.Orange);
                return;
            }

            _benchmarkIterations = args.Length > 0 && int.TryParse(args[0], out var iterations) 
                ? Math.Clamp(iterations, 1, 100) 
                : 10;
                
            _currentIteration = 0;
            _benchmarkRunning = true;
            _benchmarkTimer = new Stopwatch();
            
            HDMod.Message($"Starting texture benchmark ({_benchmarkIterations} iterations)...", Color.LightGreen);
            
            CoroutineManager.StartCoroutine(TextureBenchmarkCoroutine());
        }
        
        private static System.Collections.IEnumerator TextureBenchmarkCoroutine()
        {
            var textureManager = HDServiceLocator.Get<HDTextureManager>();
            var testTextures = GetTestTexturePaths();
            var results = new long[_benchmarkIterations];
            
            _benchmarkTimer.Start();
            
            while (_currentIteration < _benchmarkIterations)
            {
                var iterationTimer = Stopwatch.StartNew();
                
                // Загрузка всех тестовых текстур
                foreach (var texturePath in testTextures)
                {
                    textureManager.GetTextureAsync(texturePath).Wait();
                    yield return CoroutineStatus.Running;
                }
                
                iterationTimer.Stop();
                results[_currentIteration] = iterationTimer.ElapsedMilliseconds;
                
                _currentIteration++;
                HDMod.Message($"Iteration {_currentIteration}: {iterationTimer.ElapsedMilliseconds}ms", Color.White);
                yield return CoroutineStatus.Running;
            }
            
            _benchmarkTimer.Stop();
            _benchmarkRunning = false;
            
            AnalyzeResults(results);
        }
        
        private static string[] GetTestTexturePaths()
        {
            // Возвращает список тестовых текстур для бенчмарка
            return new[]
            {
                "Content/HD/Items/weapons/coilgun.png",
                "Content/HD/Items/tools/welder.png",
                "Content/HD/Characters/human/male.png",
                "Content/HD/Structures/walls/metal.png"
            };
        }
        
        /// <summary>
        /// Benchmark использования памяти
        /// </summary>
        private static void RunMemoryBenchmark(string[] args)
        {
            var textureCache = HDServiceLocator.Get<HDTextureCache>();
            var textureManager = HDServiceLocator.Get<HDTextureManager>();
            
            var sb = new StringBuilder();
            sb.AppendLine("=== Memory Benchmark ===");
            sb.AppendLine($"Textures cached: {textureCache.Count}");
            sb.AppendLine($"Total VRAM: {CalculateTextureMemory(textureCache):F2} MB");
            sb.AppendLine($"Fallback textures: {textureManager.VanillaFallbackCount}");
            
            HDMod.Message(sb.ToString(), Color.LightBlue);
        }
        
        private static float CalculateTextureMemory(HDTextureCache cache)
        {
            // Расчет используемой видеопамяти
            return cache.Sum(t => t.Width * t.Height * 4 / 1024f / 1024f);
        }
        
        private static void CancelBenchmark()
        {
            if (!_benchmarkRunning) return;
            
            _benchmarkRunning = false;
            _benchmarkTimer?.Stop();
            HDMod.Message("Benchmark cancelled", Color.Orange);
        }
        
        private static void AnalyzeResults(long[] results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Benchmark Results ===");
            sb.AppendLine($"Total time: {_benchmarkTimer.Elapsed.TotalSeconds:F2}s");
            sb.AppendLine($"Average: {results.Average():F2}ms");
            sb.AppendLine($"Min: {results.Min()}ms");
            sb.AppendLine($"Max: {results.Max()}ms");
            
            HDMod.Message(sb.ToString(), Color.LightGreen);
        }
    }
}