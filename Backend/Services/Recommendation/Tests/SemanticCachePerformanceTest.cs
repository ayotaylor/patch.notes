using Backend.Services.Recommendation.Interfaces;
using System.Diagnostics;

namespace Backend.Services.Recommendation.Tests
{
    /// <summary>
    /// Performance test utility to compare precomputed cache vs real-time extraction
    /// This is a demonstration/utility class - not a formal unit test
    /// </summary>
    public static class SemanticCachePerformanceTest
    {
        public static async Task<PerformanceTestResult> RunPerformanceComparisonAsync(
            ISemanticKeywordCache semanticCache,
            List<string> testGenres,
            List<string> testPlatforms,
            List<string> testGameModes,
            List<string> testPerspectives)
        {
            var result = new PerformanceTestResult();
            
            // Test with cache
            var stopwatch = Stopwatch.StartNew();
            await TestWithCache(semanticCache, testGenres, testPlatforms, testGameModes, testPerspectives);
            stopwatch.Stop();
            result.CacheTime = stopwatch.ElapsedMilliseconds;
            
            // Test without cache (fallback to real-time extraction)
            stopwatch.Restart();
            await TestWithoutCache(testGenres, testPlatforms, testGameModes, testPerspectives);
            stopwatch.Stop();
            result.RealTimeTime = stopwatch.ElapsedMilliseconds;
            
            result.PerformanceGain = result.RealTimeTime > 0 ? 
                ((double)(result.RealTimeTime - result.CacheTime) / result.RealTimeTime) * 100 : 0;
            
            return result;
        }

        private static async Task TestWithCache(
            ISemanticKeywordCache semanticCache,
            List<string> testGenres,
            List<string> testPlatforms,
            List<string> testGameModes,
            List<string> testPerspectives)
        {
            if (!semanticCache.IsInitialized)
            {
                await semanticCache.InitializeCacheAsync();
            }

            // Simulate extracting keywords for many games
            for (int i = 0; i < 100; i++)
            {
                foreach (var genre in testGenres)
                {
                    var genreKeywords = semanticCache.GetGenreKeywords(genre);
                }
                
                foreach (var platform in testPlatforms)
                {
                    var platformKeywords = semanticCache.GetPlatformKeywords(platform);
                }
                
                foreach (var gameMode in testGameModes)
                {
                    var gameModeKeywords = semanticCache.GetGameModeKeywords(gameMode);
                }
                
                foreach (var perspective in testPerspectives)
                {
                    var perspectiveKeywords = semanticCache.GetPerspectiveKeywords(perspective);
                }
            }
        }

        private static async Task TestWithoutCache(
            List<string> testGenres,
            List<string> testPlatforms,
            List<string> testGameModes,
            List<string> testPerspectives)
        {
            // Simulate real-time semantic keyword extraction
            // This would normally involve fuzzy matching, file I/O, etc.
            for (int i = 0; i < 100; i++)
            {
                foreach (var genre in testGenres)
                {
                    // Simulate expensive fuzzy matching and keyword extraction
                    SimulateSemanticExtraction(genre);
                }
                
                foreach (var platform in testPlatforms)
                {
                    SimulateSemanticExtraction(platform);
                }
                
                foreach (var gameMode in testGameModes)
                {
                    SimulateSemanticExtraction(gameMode);
                }
                
                foreach (var perspective in testPerspectives)
                {
                    SimulateSemanticExtraction(perspective);
                }
            }
            
            await Task.CompletedTask; // Satisfy async signature
        }

        private static void SimulateSemanticExtraction(string input)
        {
            // Simulate the expensive operations that would happen during real-time extraction:
            // 1. Fuzzy matching against configuration
            // 2. String comparison algorithms
            // 3. Dictionary lookups
            // 4. Cross-category analysis
            
            var candidates = new[] { "Action", "Adventure", "RPG", "Strategy", "Simulation", "Puzzle", "Horror" };
            
            foreach (var candidate in candidates)
            {
                SemanticUtilityService.CalculateSimilarityScore(input, candidate);
            }
            
            // Simulate additional processing time
            Thread.SpinWait(1000); // Micro-delay to simulate processing overhead
        }
    }

    public class PerformanceTestResult
    {
        public long CacheTime { get; set; }
        public long RealTimeTime { get; set; }
        public double PerformanceGain { get; set; }
        
        public override string ToString()
        {
            return $"Cache Time: {CacheTime}ms, Real-time Time: {RealTimeTime}ms, Performance Gain: {PerformanceGain:F1}%";
        }
    }
}