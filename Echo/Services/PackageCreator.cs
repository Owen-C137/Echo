using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Echo.Services
{
    public class PackageResult
    {
        public bool Success { get; set; }
        public string PackagePath { get; set; } = string.Empty;
        public long PackageSize { get; set; }
        public int FilesCopied { get; set; }
        public int FilesFailed { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public TimeSpan Duration { get; set; }
    }

    public interface IPackageProgress
    {
        void ReportProgress(int current, int total, string message);
        void ReportLog(string message);
    }

    public static class PackageCreator
    {
        public static PackageResult CreatePackage(
            List<string> gdtFilePaths,
            ScanResult scanResult,
            string packageName,
            string bo3RootPath,
            SoundAliasParseResult? soundAliasResult = null,
            int? soundMode = null,
            string? consolidatedCsvName = null,
            IPackageProgress? progress = null)
        {
            var startTime = DateTime.Now;
            var result = new PackageResult();
            var settings = SettingsManager.CurrentSettings;

            // Use provided sound mode or fall back to settings
            int actualSoundMode = soundMode ?? settings.SoundAliasHandling;
            string actualConsolidatedName = consolidatedCsvName ?? "echo_consolidated";

            try
            {
                // Validate settings
                if (string.IsNullOrWhiteSpace(bo3RootPath) || !Directory.Exists(bo3RootPath))
                {
                    result.Errors.Add("Invalid BO3 root path");
                    Logger.Error("Invalid BO3 root path for packaging");
                    return result;
                }

                // Get output directory
                var outputDir = settings.PackageOutputDirectory;
                if (string.IsNullOrWhiteSpace(outputDir))
                {
                    outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Packaged");
                }

                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // Generate package name with pattern
                var finalPackageName = GeneratePackageName(packageName, gdtFilePaths);
                var stagingDir = Path.Combine(outputDir, finalPackageName);
                var zipPath = Path.Combine(outputDir, finalPackageName + ".zip");

                Logger.Info($"Creating package: {finalPackageName}");

                // Check for missing assets if validation is enabled
                if (settings.ValidateAssets && scanResult.MissingAssets > 0)
                {
                    var missingList = string.Join("\n", scanResult.MissingFiles.Take(10));
                    if (scanResult.MissingFiles.Count > 10)
                    {
                        missingList += $"\n... and {scanResult.MissingFiles.Count - 10} more";
                    }
                    
                    result.Errors.Add($"Missing {scanResult.MissingAssets} asset files:\n{missingList}");
                    Logger.Warning($"Package has {scanResult.MissingAssets} missing assets");
                }

                // Create staging directory
                if (Directory.Exists(stagingDir))
                {
                    Directory.Delete(stagingDir, true);
                }
                Directory.CreateDirectory(stagingDir);

                progress?.ReportLog("Preparing to copy assets...");

                // Calculate total files to copy
                var totalFiles = scanResult.Assets.Count(a => a.Exists);
                var currentFile = 0;

                // Copy assets preserving structure
                Logger.Info("Copying assets to staging directory...");
                progress?.ReportLog($"Copying {totalFiles} asset files...");
                
                foreach (var asset in scanResult.Assets.Where(a => a.Exists))
                {
                    try
                    {
                        currentFile++;
                        var fileName = Path.GetFileName(asset.RelativePath);
                        progress?.ReportProgress(currentFile, totalFiles, $"Copying {fileName}");
                        progress?.ReportLog($"Copying: {asset.RelativePath}");
                        
                        var destPath = Path.Combine(stagingDir, asset.RelativePath);
                        var destDir = Path.GetDirectoryName(destPath);

                        if (!Directory.Exists(destDir))
                        {
                            Directory.CreateDirectory(destDir!);
                        }

                        File.Copy(asset.FullPath, destPath, true);
                        result.FilesCopied++;
                    }
                    catch (Exception ex)
                    {
                        result.FilesFailed++;
                        result.Errors.Add($"Failed to copy {asset.RelativePath}: {ex.Message}");
                        progress?.ReportLog($"ERROR: Failed to copy {asset.RelativePath}");
                        Logger.Error($"Failed to copy asset: {asset.RelativePath}", ex);
                    }
                }

                // Handle sound alias files based on user setting
                if (soundAliasResult != null && scanResult.ReferencedSoundAliases.Count > 0)
                {
                    if (actualSoundMode == 0) // Consolidate
                    {
                        Logger.Info($"Creating consolidated sound alias CSV with {scanResult.ReferencedSoundAliases.Count} aliases...");
                        progress?.ReportLog($"Creating consolidated CSV: {actualConsolidatedName}.csv");
                        
                        var aliasDir = Path.Combine(stagingDir, "share", "raw", "sound", "aliases");
                        Directory.CreateDirectory(aliasDir);
                        var consolidatedCsvPath = Path.Combine(aliasDir, $"{actualConsolidatedName}.csv");
                        
                        SoundAliasParser.CreateConsolidatedCsv(
                            scanResult.ReferencedSoundAliases,
                            soundAliasResult.Aliases,
                            consolidatedCsvPath);
                        
                        result.FilesCopied++;
                        
                        // Copy sound files
                        var soundFileCount = scanResult.SoundFiles.Count(s => s.Exists);
                        var currentSound = 0;
                        Logger.Info($"Copying {soundFileCount} sound files...");
                        progress?.ReportLog($"Copying {soundFileCount} sound files...");
                        
                        foreach (var soundFile in scanResult.SoundFiles.Where(s => s.Exists))
                        {
                            try
                            {
                                currentSound++;
                                var fileName = Path.GetFileName(soundFile.RelativePath);
                                progress?.ReportProgress(currentFile + currentSound, totalFiles + soundFileCount, $"Copying sound: {fileName}");
                                
                                var destPath = Path.Combine(stagingDir, soundFile.RelativePath);
                                var destDir = Path.GetDirectoryName(destPath);

                                if (!Directory.Exists(destDir))
                                {
                                    Directory.CreateDirectory(destDir!);
                                }

                                File.Copy(soundFile.FullPath, destPath, true);
                                result.FilesCopied++;
                            }
                            catch (Exception ex)
                            {
                                result.FilesFailed++;
                                result.Errors.Add($"Failed to copy sound {soundFile.RelativePath}: {ex.Message}");
                                Logger.Error($"Failed to copy sound file: {soundFile.RelativePath}", ex);
                                progress?.ReportLog($"ERROR: Failed to copy sound {soundFile.RelativePath}");
                            }
                        }
                    }
                    else if (actualSoundMode == 1) // Copy full CSV files
                    {
                        Logger.Info("Copying full sound alias CSV files...");
                        
                        var sourceAliasDir = Path.Combine(bo3RootPath, "share", "raw", "sound", "aliases");
                        var destAliasDir = Path.Combine(stagingDir, "share", "raw", "sound", "aliases");
                        Directory.CreateDirectory(destAliasDir);
                        
                        // Find which CSV files are referenced
                        var referencedCsvFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var aliasName in scanResult.ReferencedSoundAliases)
                        {
                            if (soundAliasResult.Aliases.TryGetValue(aliasName, out var alias))
                            {
                                referencedCsvFiles.Add(alias.SourceCsvFile);
                            }
                        }
                        
                        foreach (var csvFile in referencedCsvFiles)
                        {
                            try
                            {
                                var sourcePath = Path.Combine(sourceAliasDir, csvFile);
                                var destPath = Path.Combine(destAliasDir, csvFile);
                                
                                if (File.Exists(sourcePath))
                                {
                                    File.Copy(sourcePath, destPath, true);
                                    result.FilesCopied++;
                                }
                            }
                            catch (Exception ex)
                            {
                                result.Errors.Add($"Failed to copy CSV {csvFile}: {ex.Message}");
                                Logger.Error($"Failed to copy CSV: {csvFile}", ex);
                            }
                        }
                        
                        // Copy sound files
                        Logger.Info($"Copying {scanResult.SoundFiles.Count} sound files...");
                        foreach (var soundFile in scanResult.SoundFiles.Where(s => s.Exists))
                        {
                            try
                            {
                                var destPath = Path.Combine(stagingDir, soundFile.RelativePath);
                                var destDir = Path.GetDirectoryName(destPath);

                                if (!Directory.Exists(destDir))
                                {
                                    Directory.CreateDirectory(destDir!);
                                }

                                File.Copy(soundFile.FullPath, destPath, true);
                                result.FilesCopied++;
                            }
                            catch (Exception ex)
                            {
                                result.FilesFailed++;
                                result.Errors.Add($"Failed to copy sound {soundFile.RelativePath}: {ex.Message}");
                                Logger.Error($"Failed to copy sound file: {soundFile.RelativePath}", ex);
                            }
                        }
                    }
                    // soundHandling == 2 (Skip) - do nothing
                }

                // Copy source GDT files if enabled
                if (settings.IncludeSourceGdt)
                {
                    Logger.Info("Copying source GDT files...");
                    foreach (var gdtPath in gdtFilePaths)
                    {
                        try
                        {
                            // Get relative path from BO3 root
                            var relativePath = Path.GetRelativePath(bo3RootPath, gdtPath);
                            var destPath = Path.Combine(stagingDir, relativePath);
                            var destDir = Path.GetDirectoryName(destPath);

                            if (!Directory.Exists(destDir))
                            {
                                Directory.CreateDirectory(destDir!);
                            }

                            File.Copy(gdtPath, destPath, true);
                            result.FilesCopied++;
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"Failed to copy GDT file: {ex.Message}");
                            Logger.Error($"Failed to copy GDT: {gdtPath}", ex);
                        }
                    }
                }

                // Generate attachment GDT file if enabled and there are resolved attachments
                if (settings.IncludeAttachmentGdt && scanResult.ResolvedAttachments.Count > 0)
                {
                    Logger.Info($"Generating attachment GDT with {scanResult.ResolvedAttachments.Count} definitions...");
                    progress?.ReportLog($"Creating attachment GDT ({scanResult.ResolvedAttachments.Count} attachments)...");
                    
                    try
                    {
                        // Use the first GDT file to determine the output location
                        var firstGdtPath = gdtFilePaths.FirstOrDefault();
                        if (!string.IsNullOrEmpty(firstGdtPath))
                        {
                            GdtGenerator.CreateAttachmentGdtFile(stagingDir, firstGdtPath, scanResult.ResolvedAttachments, bo3RootPath);
                            result.FilesCopied++;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Failed to create attachment GDT: {ex.Message}");
                        Logger.Error($"Failed to create attachment GDT", ex);
                    }
                }

                // Create missing files report if there are missing files
                if (scanResult.MissingFiles.Count > 0)
                {
                    var reportPath = Path.Combine(stagingDir, "missing_files.txt");
                    File.WriteAllLines(reportPath, scanResult.MissingFiles);
                    Logger.Info($"Created missing files report: {scanResult.MissingFiles.Count} files");
                }

                // Create manifest
                CreateManifest(stagingDir, gdtFilePaths, scanResult, finalPackageName);

                // Compress to ZIP
                Logger.Info("Compressing package...");
                
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }

                var compressionLevel = settings.CompressionLevel switch
                {
                    0 => CompressionLevel.NoCompression,
                    1 => CompressionLevel.Fastest,
                    _ => CompressionLevel.Optimal
                };

                progress?.ReportLog($"Creating ZIP archive...");
                progress?.ReportProgress(100, 100, "Compressing package...");

                // Check archive structure setting
                if (settings.ArchiveStructure == 0) // Direct
                {
                    // Compress contents directly (extract to BO3 root)
                    ZipFile.CreateFromDirectory(stagingDir, zipPath, compressionLevel, false);
                }
                else // Wrapped
                {
                    // Wrap in root folder
                    ZipFile.CreateFromDirectory(stagingDir, zipPath, compressionLevel, true);
                }

                progress?.ReportLog($"Cleaning up temporary files...");
                
                // Clean up staging directory
                Directory.Delete(stagingDir, true);

                // Get final package info
                var packageInfo = new FileInfo(zipPath);
                result.PackageSize = packageInfo.Length;
                result.PackagePath = zipPath;
                result.Success = true;

                result.Duration = DateTime.Now - startTime;

                progress?.ReportLog($"Package created successfully: {AssetScanner.FormatBytes(result.PackageSize)}");                Logger.Info($"Package created successfully: {zipPath} ({AssetScanner.FormatBytes(result.PackageSize)}) in {result.Duration.TotalSeconds:F2}s");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add($"Package creation failed: {ex.Message}");
                Logger.Error("Package creation failed", ex);
            }

            return result;
        }

        private static string GeneratePackageName(string customName, List<string> gdtFilePaths)
        {
            if (!string.IsNullOrWhiteSpace(customName))
            {
                return SanitizeFileName(customName);
            }

            var pattern = SettingsManager.CurrentSettings.PackageNamePattern ?? "echo_packaged_{date}_{time}";
            var now = DateTime.Now;
            
            var name = pattern
                .Replace("{date}", now.ToString("yyyyMMdd"))
                .Replace("{time}", now.ToString("HHmmss"))
                .Replace("{gdtname}", gdtFilePaths.Count > 0 ? Path.GetFileNameWithoutExtension(gdtFilePaths[0]) : "unknown");

            return SanitizeFileName(name);
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }

        private static void CreateManifest(string stagingDir, List<string> gdtFilePaths, ScanResult scanResult, string packageName)
        {
            var manifestPath = Path.Combine(stagingDir, "manifest.json");
            
            var manifest = new
            {
                PackageName = packageName,
                CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                CreatedBy = "Echo GDT Package Manager",
                Version = "1.0",
                SourceGdtFiles = gdtFilePaths.Select(Path.GetFileName).ToList(),
                TotalAssets = scanResult.TotalAssets,
                FoundAssets = scanResult.FoundAssets,
                MissingAssets = scanResult.MissingAssets,
                TotalSizeBytes = scanResult.TotalSize,
                TotalSizeFormatted = AssetScanner.FormatBytes(scanResult.TotalSize)
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(manifest, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(manifestPath, json);
            
            Logger.Info("Manifest created");
        }
    }
}
