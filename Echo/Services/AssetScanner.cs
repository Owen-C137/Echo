using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Echo.Services
{
    public class ScannedAsset
    {
        public string RelativePath { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public bool Exists { get; set; }
        public long FileSize { get; set; }
        public string AssetName { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
    }

    public class ScanResult
    {
        public List<ScannedAsset> Assets { get; set; } = new List<ScannedAsset>();
        public int TotalAssets { get; set; }
        public int FoundAssets { get; set; }
        public int MissingAssets { get; set; }
        public long TotalSize { get; set; }
        public List<string> MissingFiles { get; set; } = new List<string>();
        public HashSet<string> ReferencedSoundAliases { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public List<ScannedAsset> SoundFiles { get; set; } = new List<ScannedAsset>();
        public int TotalSoundFiles { get; set; }
        public List<AttachmentDefinition> ResolvedAttachments { get; set; } = new List<AttachmentDefinition>(); // Full attachment definitions for GDT generation
    }

    public static class AssetScanner
    {
        public static ScanResult ScanAssets(GdtParseResult parseResult, string bo3RootPath, SoundAliasParseResult? soundAliasResult = null, List<string>? gdtFilePaths = null)
        {
            var scanResult = new ScanResult();

            if (string.IsNullOrWhiteSpace(bo3RootPath) || !Directory.Exists(bo3RootPath))
            {
                Logger.Error($"Invalid BO3 root path: {bo3RootPath}");
                return scanResult;
            }

            Logger.Info($"Scanning assets in: {bo3RootPath}");

            // Scan regular file assets
            foreach (var asset in parseResult.Assets)
            {
                foreach (var relativePath in asset.FilePaths)
                {
                    // Clean and normalize the relative path
                    var cleanRelativePath = relativePath.Trim().Replace("\\\\", "\\");
                    
                    // Remove leading ..\ or ../ (relative to GDT location, but we're using BO3 root)
                    // Example: "..\\model_export\\file.png" becomes "model_export\\file.png"
                    while (cleanRelativePath.StartsWith("..\\") || cleanRelativePath.StartsWith("../"))
                    {
                        cleanRelativePath = cleanRelativePath.Substring(3);
                    }
                    
                    // Remove leading .\ or ./
                    if (cleanRelativePath.StartsWith(".\\") || cleanRelativePath.StartsWith("./"))
                    {
                        cleanRelativePath = cleanRelativePath.Substring(2);
                    }
                    
                    // Skip engine placeholders and texture references without file extensions
                    if (cleanRelativePath.StartsWith("$", StringComparison.OrdinalIgnoreCase) || 
                        cleanRelativePath.StartsWith("ximage_", StringComparison.OrdinalIgnoreCase) ||
                        (!Path.HasExtension(cleanRelativePath) && !cleanRelativePath.Contains("\\")))
                    {
                        // These are engine references or auto-generated texture names, skip them
                        continue;
                    }
                    
                    // Determine correct export directory based on file type
                    string fullPath;
                    bool fileExists = false;
                    
                    // For FX files: map fx\ prefix to share\raw\fx\
                    if (cleanRelativePath.StartsWith("fx\\", StringComparison.OrdinalIgnoreCase) ||
                        cleanRelativePath.StartsWith("fx/", StringComparison.OrdinalIgnoreCase))
                    {
                        // Remove the fx\ prefix and add share\raw\fx\
                        var fxPath = cleanRelativePath.Substring(3); // Remove "fx\" or "fx/"
                        cleanRelativePath = Path.Combine("share", "raw", "fx", fxPath);
                        fullPath = Path.Combine(bo3RootPath, cleanRelativePath).Replace("\\\\", "\\");
                        fileExists = File.Exists(fullPath);
                    }
                    // For animations: try xanim_export first, fallback to model_export
                    else if (cleanRelativePath.Contains(".xanim", StringComparison.OrdinalIgnoreCase) &&
                        !cleanRelativePath.StartsWith("xanim_export", StringComparison.OrdinalIgnoreCase) &&
                        !cleanRelativePath.StartsWith("model_export", StringComparison.OrdinalIgnoreCase))
                    {
                        // Try xanim_export first
                        var animExportPath = Path.Combine("xanim_export", cleanRelativePath);
                        fullPath = Path.Combine(bo3RootPath, animExportPath).Replace("\\\\", "\\");
                        fileExists = File.Exists(fullPath);
                        
                        // If not found, try model_export as fallback
                        if (!fileExists)
                        {
                            var modelExportPath = Path.Combine("model_export", cleanRelativePath);
                            var fallbackPath = Path.Combine(bo3RootPath, modelExportPath).Replace("\\\\", "\\");
                            if (File.Exists(fallbackPath))
                            {
                                cleanRelativePath = modelExportPath;
                                fullPath = fallbackPath;
                                fileExists = true;
                            }
                            else
                            {
                                cleanRelativePath = animExportPath; // Keep original attempt
                            }
                        }
                        else
                        {
                            cleanRelativePath = animExportPath;
                        }
                    }
                    // For models: use model_export (if not already prefixed)
                    else if (cleanRelativePath.Contains(".XMODEL", StringComparison.OrdinalIgnoreCase) &&
                             !cleanRelativePath.StartsWith("model_export", StringComparison.OrdinalIgnoreCase))
                    {
                        cleanRelativePath = Path.Combine("model_export", cleanRelativePath);
                        fullPath = Path.Combine(bo3RootPath, cleanRelativePath).Replace("\\\\", "\\");
                        fileExists = File.Exists(fullPath);
                    }
                    else
                    {
                        // Standard path resolution
                        fullPath = Path.Combine(bo3RootPath, cleanRelativePath).Replace("\\\\", "\\");
                        fileExists = File.Exists(fullPath);
                    }

                    var scannedAsset = new ScannedAsset
                    {
                        RelativePath = cleanRelativePath, // Store the cleaned path
                        FullPath = fullPath,
                        AssetName = asset.Name,
                        AssetType = asset.Type,
                        Exists = fileExists
                    };

                    // Debug logging for first few files
                    if (scanResult.Assets.Count < 5)
                    {
                        Logger.Info($"DEBUG - Original path: {relativePath}");
                        Logger.Info($"DEBUG - Cleaned path: {cleanRelativePath}");
                        Logger.Info($"DEBUG - Full path: {fullPath}");
                        Logger.Info($"DEBUG - File exists: {scannedAsset.Exists}");
                    }

                    if (scannedAsset.Exists)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(fullPath);
                            scannedAsset.FileSize = fileInfo.Length;
                            scanResult.TotalSize += fileInfo.Length;
                            scanResult.FoundAssets++;
                        }
                        catch (Exception ex)
                        {
                            Logger.Warning($"Failed to get file info for: {fullPath} - {ex.Message}");
                        }
                    }
                    else
                    {
                        scanResult.MissingAssets++;
                        scanResult.MissingFiles.Add(relativePath);
                        Logger.Warning($"Missing asset file: {relativePath}");
                    }

                    scanResult.Assets.Add(scannedAsset);
                }

                // Collect sound aliases
                foreach (var aliasName in asset.SoundAliases)
                {
                    scanResult.ReferencedSoundAliases.Add(aliasName);
                }

                // Resolve shared weapon sounds
                if (asset.SharedWeaponSounds.Count > 0 && gdtFilePaths != null && gdtFilePaths.Count > 0)
                {
                    // Get the directory where GDT files are located
                    var gdtDirectory = Path.GetDirectoryName(gdtFilePaths[0]);
                    if (!string.IsNullOrEmpty(gdtDirectory))
                    {
                        foreach (var sharedSoundName in asset.SharedWeaponSounds)
                        {
                            Logger.Info($"Resolving shared weapon sounds: {sharedSoundName}");
                            var resolvedAliases = GdtParser.ResolveSharedWeaponSounds(sharedSoundName, gdtDirectory);
                            
                            foreach (var alias in resolvedAliases)
                            {
                                scanResult.ReferencedSoundAliases.Add(alias);
                            }
                        }
                    }
                }

                // Resolve attachment models
                if (asset.AttachmentModels.Count > 0 && gdtFilePaths != null && gdtFilePaths.Count > 0)
                {
                    // Get the directory where GDT files are located
                    var gdtDirectory = Path.GetDirectoryName(gdtFilePaths[0]);
                    if (!string.IsNullOrEmpty(gdtDirectory))
                    {
                        foreach (var attachmentName in asset.AttachmentModels)
                        {
                            Logger.Info($"Resolving attachment model: {attachmentName}");
                            
                            // Resolve the full attachment definition (includes raw GDT text)
                            var attachmentDef = GdtParser.ResolveAttachmentDefinition(attachmentName, gdtDirectory);
                            
                            if (attachmentDef != null)
                            {
                                // Store the attachment definition for GDT generation
                                scanResult.ResolvedAttachments.Add(attachmentDef);
                                
                                // Add resolved file paths as assets
                                foreach (var path in attachmentDef.FilePaths)
                                {
                                    var cleanPath = path.Trim().Replace("\\\\", "\\");
                                    
                                    // For models: use model_export
                                    string fullPath;
                                    if (!cleanPath.StartsWith("model_export", StringComparison.OrdinalIgnoreCase))
                                    {
                                        cleanPath = Path.Combine("model_export", cleanPath);
                                    }
                                    fullPath = Path.Combine(bo3RootPath, cleanPath).Replace("\\\\", "\\");

                                    var scannedAsset = new ScannedAsset
                                    {
                                        RelativePath = cleanPath,
                                        FullPath = fullPath,
                                        Exists = File.Exists(fullPath),
                                        FileSize = File.Exists(fullPath) ? new FileInfo(fullPath).Length : 0,
                                        AssetName = asset.Name,
                                        AssetType = $"{asset.Type} (Attachment: {attachmentName})"
                                    };

                                    scanResult.Assets.Add(scannedAsset);

                                    if (scannedAsset.Exists)
                                    {
                                        scanResult.TotalSize += scannedAsset.FileSize;
                                    }
                                    else
                                    {
                                        scanResult.MissingFiles.Add(cleanPath);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Resolve sound aliases to actual files if provided
            if (soundAliasResult != null && scanResult.ReferencedSoundAliases.Count > 0)
            {
                Logger.Info($"Resolving {scanResult.ReferencedSoundAliases.Count} sound aliases...");
                
                foreach (var aliasName in scanResult.ReferencedSoundAliases)
                {
                    var soundFiles = SoundAliasParser.ResolveAliasToFiles(aliasName, soundAliasResult.Aliases);
                    
                    foreach (var soundPath in soundFiles)
                    {
                        // Sound paths in CSVs are relative to sound_assets\ (NOT share/raw/sound)
                        var fullPath = Path.Combine(bo3RootPath, "sound_assets", soundPath.Replace("/", "\\"));
                        
                        var scannedAsset = new ScannedAsset
                        {
                            RelativePath = Path.Combine("sound_assets", soundPath.Replace("/", "\\")),
                            FullPath = fullPath,
                            AssetName = aliasName,
                            AssetType = "soundalias",
                            Exists = File.Exists(fullPath)
                        };

                        if (scannedAsset.Exists)
                        {
                            try
                            {
                                var fileInfo = new FileInfo(fullPath);
                                scannedAsset.FileSize = fileInfo.Length;
                                scanResult.TotalSize += fileInfo.Length;
                                scanResult.FoundAssets++;
                            }
                            catch (Exception ex)
                            {
                                Logger.Warning($"Failed to get file info for sound: {fullPath} - {ex.Message}");
                            }
                        }
                        else
                        {
                            scanResult.MissingAssets++;
                            scanResult.MissingFiles.Add(scannedAsset.RelativePath);
                            Logger.Warning($"Missing sound file: {scannedAsset.RelativePath}");
                        }

                        scanResult.SoundFiles.Add(scannedAsset);
                    }
                }
                
                scanResult.TotalSoundFiles = scanResult.SoundFiles.Count;
                Logger.Info($"Sound resolution complete: {scanResult.TotalSoundFiles} sound files found");
            }

            scanResult.TotalAssets = scanResult.Assets.Count + scanResult.SoundFiles.Count;

            Logger.Info($"Scan complete: {scanResult.FoundAssets} found, {scanResult.MissingAssets} missing, Total size: {FormatBytes(scanResult.TotalSize)}");

            return scanResult;
        }

        public static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
