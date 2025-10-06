using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Echo.Services
{
    public class FileMatch
    {
        public string OriginalPath { get; set; } = string.Empty;
        public string FoundPath { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public int MatchScore { get; set; } // 100 = perfect, lower = fuzzy
        public long FileSize { get; set; }
        public string MatchReason { get; set; } = string.Empty;
    }

    public class MissingFileSearchResult
    {
        public string OriginalPath { get; set; } = string.Empty;
        public List<FileMatch> PossibleMatches { get; set; } = new List<FileMatch>();
        public bool HasExactMatch => PossibleMatches.Any(m => m.MatchScore == 100);
        public bool HasFuzzyMatches => PossibleMatches.Any(m => m.MatchScore < 100);
    }

    public interface IMissingFileProgress
    {
        void ReportProgress(int current, int total, string message);
        void ReportSearchProgress(string directory, int filesScanned);
    }

    public static class MissingFileFinder
    {
        /// <summary>
        /// Search for missing files across the entire BO3 installation
        /// </summary>
        public static async Task<List<MissingFileSearchResult>> FindMissingFilesAsync(
            List<string> missingRelativePaths,
            string bo3RootPath,
            IMissingFileProgress? progress = null,
            CancellationToken cancellationToken = default)
        {
            var results = new List<MissingFileSearchResult>();
            
            if (missingRelativePaths == null || !missingRelativePaths.Any())
                return results;

            if (string.IsNullOrWhiteSpace(bo3RootPath) || !Directory.Exists(bo3RootPath))
            {
                Logger.Error($"Invalid BO3 root path: {bo3RootPath}");
                return results;
            }

            Logger.Info($"Starting deep search for {missingRelativePaths.Count} missing files");

            // Build file index of entire BO3 directory (this will take time)
            var fileIndex = await Task.Run(() => BuildFileIndex(bo3RootPath, progress, cancellationToken), cancellationToken);
            
            if (cancellationToken.IsCancellationRequested)
                return results;

            Logger.Info($"File index built: {fileIndex.Count} files indexed");

            // Search for each missing file
            for (int i = 0; i < missingRelativePaths.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var missingPath = missingRelativePaths[i];
                progress?.ReportProgress(i + 1, missingRelativePaths.Count, $"Searching for: {Path.GetFileName(missingPath)}");

                var searchResult = await Task.Run(() => FindMatches(missingPath, fileIndex, bo3RootPath), cancellationToken);
                results.Add(searchResult);
            }

            Logger.Info($"Search complete: Found matches for {results.Count(r => r.HasExactMatch || r.HasFuzzyMatches)}/{missingRelativePaths.Count} files");

            return results;
        }

        /// <summary>
        /// Build an index of all files in BO3 directory
        /// </summary>
        private static Dictionary<string, List<string>> BuildFileIndex(
            string bo3RootPath,
            IMissingFileProgress? progress,
            CancellationToken cancellationToken)
        {
            var fileIndex = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            int filesScanned = 0;

            try
            {
                // Search key directories (avoid searching EVERYTHING to save time)
                var searchDirs = new[]
                {
                    Path.Combine(bo3RootPath, "model_export"),
                    Path.Combine(bo3RootPath, "xanim_export"),
                    Path.Combine(bo3RootPath, "sound_assets"),
                    Path.Combine(bo3RootPath, "share", "raw"),
                    Path.Combine(bo3RootPath, "texture_assets"),
                    Path.Combine(bo3RootPath, "video")
                };

                foreach (var searchDir in searchDirs)
                {
                    if (!Directory.Exists(searchDir))
                        continue;

                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var dirName = Path.GetFileName(searchDir);
                    progress?.ReportSearchProgress(dirName, filesScanned);

                    // Recursively get all files
                    var files = Directory.GetFiles(searchDir, "*.*", SearchOption.AllDirectories);

                    foreach (var file in files)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        filesScanned++;
                        if (filesScanned % 1000 == 0)
                        {
                            progress?.ReportSearchProgress(dirName, filesScanned);
                        }

                        var fileName = Path.GetFileName(file);
                        
                        if (!fileIndex.ContainsKey(fileName))
                        {
                            fileIndex[fileName] = new List<string>();
                        }

                        fileIndex[fileName].Add(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error building file index: {ex.Message}");
            }

            return fileIndex;
        }

        /// <summary>
        /// Find possible matches for a missing file
        /// </summary>
        private static MissingFileSearchResult FindMatches(
            string missingRelativePath,
            Dictionary<string, List<string>> fileIndex,
            string bo3RootPath)
        {
            var result = new MissingFileSearchResult
            {
                OriginalPath = missingRelativePath
            };

            var fileName = Path.GetFileName(missingRelativePath);
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(missingRelativePath);
            var extension = Path.GetExtension(missingRelativePath);

            // 1. Try exact filename match
            if (fileIndex.TryGetValue(fileName, out var exactMatches))
            {
                foreach (var fullPath in exactMatches)
                {
                    var relativePath = GetRelativePath(fullPath, bo3RootPath);
                    
                    result.PossibleMatches.Add(new FileMatch
                    {
                        OriginalPath = missingRelativePath,
                        FoundPath = relativePath,
                        FullPath = fullPath,
                        MatchScore = 100,
                        FileSize = new FileInfo(fullPath).Length,
                        MatchReason = "Exact filename match"
                    });
                }
            }

            // 2. Try fuzzy matching if no exact match or if exact match is in wrong directory
            if (!result.HasExactMatch || result.PossibleMatches.Count > 1)
            {
                // Look for files with same name but different extension
                var similarNames = fileIndex.Keys
                    .Where(k => Path.GetFileNameWithoutExtension(k).Equals(fileNameWithoutExt, StringComparison.OrdinalIgnoreCase))
                    .Where(k => !k.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var similarName in similarNames)
                {
                    if (fileIndex.TryGetValue(similarName, out var similarMatches))
                    {
                        foreach (var fullPath in similarMatches)
                        {
                            var relativePath = GetRelativePath(fullPath, bo3RootPath);
                            
                            result.PossibleMatches.Add(new FileMatch
                            {
                                OriginalPath = missingRelativePath,
                                FoundPath = relativePath,
                                FullPath = fullPath,
                                MatchScore = 80,
                                FileSize = new FileInfo(fullPath).Length,
                                MatchReason = "Same name, different extension"
                            });
                        }
                    }
                }

                // 3. Try partial name matching (contains the file name without extension)
                var partialMatches = fileIndex.Keys
                    .Where(k => k.Contains(fileNameWithoutExt, StringComparison.OrdinalIgnoreCase))
                    .Where(k => !k.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                    .Where(k => !Path.GetFileNameWithoutExtension(k).Equals(fileNameWithoutExt, StringComparison.OrdinalIgnoreCase))
                    .Take(5) // Limit partial matches to avoid too many results
                    .ToList();

                foreach (var partialName in partialMatches)
                {
                    if (fileIndex.TryGetValue(partialName, out var partialFiles))
                    {
                        foreach (var fullPath in partialFiles.Take(2)) // Max 2 per partial match
                        {
                            var relativePath = GetRelativePath(fullPath, bo3RootPath);
                            
                            result.PossibleMatches.Add(new FileMatch
                            {
                                OriginalPath = missingRelativePath,
                                FoundPath = relativePath,
                                FullPath = fullPath,
                                MatchScore = 60,
                                FileSize = new FileInfo(fullPath).Length,
                                MatchReason = "Partial filename match"
                            });
                        }
                    }
                }
            }

            // Sort by match score (best matches first)
            result.PossibleMatches = result.PossibleMatches
                .OrderByDescending(m => m.MatchScore)
                .ThenBy(m => m.FoundPath.Length) // Prefer shorter paths
                .ToList();

            return result;
        }

        /// <summary>
        /// Get relative path from BO3 root
        /// </summary>
        private static string GetRelativePath(string fullPath, string bo3RootPath)
        {
            if (fullPath.StartsWith(bo3RootPath, StringComparison.OrdinalIgnoreCase))
            {
                return fullPath.Substring(bo3RootPath.Length).TrimStart('\\', '/');
            }
            return fullPath;
        }

        /// <summary>
        /// Calculate Levenshtein distance for fuzzy string matching
        /// </summary>
        private static int LevenshteinDistance(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1))
                return s2?.Length ?? 0;

            if (string.IsNullOrEmpty(s2))
                return s1.Length;

            var d = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                d[i, 0] = i;

            for (int j = 0; j <= s2.Length; j++)
                d[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[s1.Length, s2.Length];
        }
    }
}
