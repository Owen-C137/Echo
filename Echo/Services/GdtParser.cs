using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Echo.Services
{
    public class GdtAsset
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public List<string> FilePaths { get; set; } = new List<string>();
        public List<string> SoundAliases { get; set; } = new List<string>(); // Sound alias references
        public List<string> SharedWeaponSounds { get; set; } = new List<string>(); // Shared weapon sound references
        public List<string> AttachmentModels { get; set; } = new List<string>(); // Attachment model references (attachViewModel1-99, attachWorldModel1-99)
    }

    public class AttachmentDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string RawDefinition { get; set; } = string.Empty; // Full GDT block text
        public List<string> FilePaths { get; set; } = new List<string>();
        public string SourceGdtPath { get; set; } = string.Empty; // Path to the GDT file this was found in
    }

    public class GdtParseResult
    {
        public List<GdtAsset> Assets { get; set; } = new List<GdtAsset>();
        public int TotalAssets { get; set; }
        public int TotalFiles { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public static class GdtParser
    {
        // Regex patterns for parsing GDT files
        private static readonly Regex AssetHeaderRegex = new Regex(
            @"^\s*""([^""]+)""\s*\(\s*""([^""]+)""\s*\)",
            RegexOptions.Compiled);

        private static readonly Regex FilePathRegex = new Regex(
            @"""(baseImage|filename|model|soundFile|file|texture|image)""\s*""([^""]+)""",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Property names that commonly contain file paths
        private static readonly HashSet<string> FilePropertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "baseImage", "filename", "model", "soundFile", "file", "texture", "image",
            "normalMap", "specularMap", "glossMap", "occlusionMap", "alphaMap",
            "colorMap", "detailMap", "revealMap", "camoMaskMap", "camoDetailMap"
        };

        // Property names that contain sound aliases
        private static readonly HashSet<string> SoundAliasPropertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "fireSound", "fireSoundPlayer", "fireSoundPlayerAkimbo", "lastShotSound", "lastShotSoundPlayer",
            "sound", "soundAlias", "loopSound", "stopSound", "startSound", "endSound",
            "reloadSound", "reloadSoundPlayer", "raiseSound", "putawaySound", "adsSound",
            "shellCasingSound", "meleeSound", "meleeSoundPlayer", "chargeSound", "chargeSoundPlayer",
            "rechamberSound", "rechamberSoundPlayer", "altFireSound", "altFireSoundPlayer",
            "explosionSound", "impactSound", "ambientSound", "musicAlias", "voiceAlias",
            "pickupSound", "pickupSoundPlayer", "dropSound", "nightVisionWearSound", "nightVisionWearSoundPlayer",
            "nightVisionRemoveSound", "projectileSound", "whizbySound", "shellShockSound",
            "deploySound", "finishDeploySound", "breakdownSound", "finishBreakdownSound",
            "detonateSound", "nightVisionWearSoundPlayer", "nightVisionRemoveSoundPlayer"
        };

        // Regex to detect attachment model properties (attachViewModel1-99, attachWorldModel1-99)
        private static readonly Regex AttachmentModelRegex = new Regex(
            @"^(attachViewModel|attachWorldModel)(\d+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static GdtParseResult ParseGdtFile(string gdtFilePath)
        {
            var result = new GdtParseResult();

            try
            {
                if (!File.Exists(gdtFilePath))
                {
                    result.Errors.Add($"GDT file not found: {gdtFilePath}");
                    Logger.Error($"GDT file not found: {gdtFilePath}");
                    return result;
                }

                Logger.Info($"Parsing GDT file: {gdtFilePath}");
                
                var lines = File.ReadAllLines(gdtFilePath);
                GdtAsset? currentAsset = null;
                int braceDepth = 0;
                var soundActionProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // Track which properties are Sound actions

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();

                    // Check for asset header (e.g., "asset_name" ( "type.gdf" ))
                    var headerMatch = AssetHeaderRegex.Match(line);
                    if (headerMatch.Success)
                    {
                        // Save previous asset if exists
                        if (currentAsset != null && (currentAsset.FilePaths.Count > 0 || currentAsset.SoundAliases.Count > 0 || currentAsset.AttachmentModels.Count > 0))
                        {
                            result.Assets.Add(currentAsset);
                        }

                        // Start new asset
                        var assetName = headerMatch.Groups[1].Value;
                        var assetType = headerMatch.Groups[2].Value;
                        
                        currentAsset = new GdtAsset
                        {
                            Name = assetName,
                            Type = assetType
                        };
                        
                        // Note: Animation assets (xanim.gdf) have their file paths in the "filename" property
                        // No need to add implicit files - they're explicitly defined
                        
                        braceDepth = 0;
                        soundActionProperties.Clear();
                        continue;
                    }

                    // Track braces for context
                    if (line.Contains("{")) braceDepth++;
                    if (line.Contains("}")) braceDepth--;

                    // If we're inside an asset definition, look for file paths
                    if (currentAsset != null && braceDepth > 0)
                    {
                        // Look for property-value pairs that might be file paths
                        var propertyMatch = Regex.Match(line, @"""([^""]+)""\s+""([^""]+)""");
                        if (propertyMatch.Success)
                        {
                            var propertyName = propertyMatch.Groups[1].Value;
                            var propertyValue = propertyMatch.Groups[2].Value;

                            // Check if this is a "Sound" action marker (e.g., "customnote1action" "Sound")
                            if (propertyName.EndsWith("action", StringComparison.OrdinalIgnoreCase) && 
                                propertyValue.Equals("Sound", StringComparison.OrdinalIgnoreCase))
                            {
                                // Extract the base property name (e.g., "customnote1" from "customnote1action")
                                var baseName = propertyName.Substring(0, propertyName.Length - 6); // Remove "action"
                                soundActionProperties.Add(baseName);
                                continue;
                            }

                            // Check if this is a sound parameter for a previously marked Sound action
                            // e.g., "customnote1actionparam1" where "customnote1" was marked as Sound
                            if (propertyName.Contains("actionparam", StringComparison.OrdinalIgnoreCase))
                            {
                                // Extract the base name before "actionparam"
                                var paramMatch = Regex.Match(propertyName, @"^(.+?)actionparam\d*$", RegexOptions.IgnoreCase);
                                if (paramMatch.Success)
                                {
                                    var baseName = paramMatch.Groups[1].Value;
                                    if (soundActionProperties.Contains(baseName))
                                    {
                                        var aliasName = propertyValue.Trim();
                                        if (!string.IsNullOrWhiteSpace(aliasName) && !currentAsset.SoundAliases.Contains(aliasName))
                                        {
                                            currentAsset.SoundAliases.Add(aliasName);
                                        }
                                        continue;
                                    }
                                }
                            }

                            // Check if this property is a known file path property
                            if (FilePropertyNames.Contains(propertyName))
                            {
                                // Clean the path (convert backslashes, remove leading/trailing spaces)
                                var cleanPath = propertyValue.Trim().Replace("\\\\", "\\");
                                
                                if (!string.IsNullOrWhiteSpace(cleanPath) && !currentAsset.FilePaths.Contains(cleanPath))
                                {
                                    currentAsset.FilePaths.Add(cleanPath);
                                    
                                    // Debug logging for first few files
                                    if (result.TotalFiles < 5)
                                    {
                                        Logger.Info($"DEBUG - Parsed file path: {cleanPath} (from property: {propertyName})");
                                    }
                                }
                            }
                            // Check if this property is a sound alias property
                            else if (SoundAliasPropertyNames.Contains(propertyName))
                            {
                                var aliasName = propertyValue.Trim();
                                
                                if (!string.IsNullOrWhiteSpace(aliasName) && !currentAsset.SoundAliases.Contains(aliasName))
                                {
                                    currentAsset.SoundAliases.Add(aliasName);
                                }
                            }
                            // Also check for direct sound parameter properties (notetrackXsoundparam, etc.)
                            else if (propertyName.Contains("soundparam", StringComparison.OrdinalIgnoreCase) ||
                                     propertyName.Contains("sound_", StringComparison.OrdinalIgnoreCase))
                            {
                                // These are likely sound aliases
                                var aliasName = propertyValue.Trim();
                                if (!string.IsNullOrWhiteSpace(aliasName) && !currentAsset.SoundAliases.Contains(aliasName))
                                {
                                    currentAsset.SoundAliases.Add(aliasName);
                                }
                            }
                            // Smart FX detection: property ends with "Effect" OR value starts with "fx\" or ends with ".efx"
                            else if (propertyName.EndsWith("Effect", StringComparison.OrdinalIgnoreCase) ||
                                     propertyValue.StartsWith("fx\\", StringComparison.OrdinalIgnoreCase) ||
                                     propertyValue.StartsWith("fx/", StringComparison.OrdinalIgnoreCase) ||
                                     propertyValue.EndsWith(".efx", StringComparison.OrdinalIgnoreCase))
                            {
                                // This is an FX file path
                                var cleanPath = propertyValue.Trim().Replace("\\\\", "\\");
                                
                                if (!string.IsNullOrWhiteSpace(cleanPath) && 
                                    cleanPath != "" && 
                                    !currentAsset.FilePaths.Contains(cleanPath))
                                {
                                    currentAsset.FilePaths.Add(cleanPath);
                                }
                            }
                            // Detect shared weapon sounds references
                            else if (propertyName.Equals("sharedWeaponSounds", StringComparison.OrdinalIgnoreCase))
                            {
                                var sharedSoundName = propertyValue.Trim();
                                if (!string.IsNullOrWhiteSpace(sharedSoundName) && 
                                    !currentAsset.SharedWeaponSounds.Contains(sharedSoundName))
                                {
                                    currentAsset.SharedWeaponSounds.Add(sharedSoundName);
                                }
                            }
                            // Detect attachment model references (attachViewModel1-99, attachWorldModel1-99)
                            else if (AttachmentModelRegex.IsMatch(propertyName))
                            {
                                var attachmentName = propertyValue.Trim();
                                if (!string.IsNullOrWhiteSpace(attachmentName) && 
                                    attachmentName != "" &&
                                    !currentAsset.AttachmentModels.Contains(attachmentName))
                                {
                                    currentAsset.AttachmentModels.Add(attachmentName);
                                    Logger.Info($"Found attachment model: {attachmentName} (from {propertyName})");
                                }
                            }
                        }
                    }
                }

                // Add the last asset
                if (currentAsset != null && (currentAsset.FilePaths.Count > 0 || currentAsset.SoundAliases.Count > 0 || currentAsset.AttachmentModels.Count > 0))
                {
                    result.Assets.Add(currentAsset);
                }

                result.TotalAssets = result.Assets.Count;
                result.TotalFiles = 0;
                foreach (var asset in result.Assets)
                {
                    result.TotalFiles += asset.FilePaths.Count;
                }

                Logger.Info($"GDT parsing complete: {result.TotalAssets} assets, {result.TotalFiles} file references");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error parsing GDT: {ex.Message}");
                Logger.Error("Failed to parse GDT file", ex);
            }

            return result;
        }

        public static GdtParseResult ParseMultipleGdtFiles(List<string> gdtFilePaths)
        {
            var combinedResult = new GdtParseResult();

            foreach (var gdtPath in gdtFilePaths)
            {
                var result = ParseGdtFile(gdtPath);
                
                combinedResult.Assets.AddRange(result.Assets);
                combinedResult.Errors.AddRange(result.Errors);
            }

            combinedResult.TotalAssets = combinedResult.Assets.Count;
            combinedResult.TotalFiles = 0;
            foreach (var asset in combinedResult.Assets)
            {
                combinedResult.TotalFiles += asset.FilePaths.Count;
            }

            Logger.Info($"Multiple GDT parsing complete: {gdtFilePaths.Count} files, {combinedResult.TotalAssets} assets, {combinedResult.TotalFiles} file references");

            return combinedResult;
        }

        /// <summary>
        /// Resolves attachment model references by searching for their definitions in common GDT files
        /// Returns full AttachmentDefinition with raw GDT text
        /// </summary>
        public static AttachmentDefinition? ResolveAttachmentDefinition(string attachmentName, string gdtDirectory)
        {
            try
            {
                // Search for common GDT files that might contain attachment definitions
                var commonGdtFiles = new List<string>();
                
                // Look for _wpn_t9_common.gdt and similar files
                var gdtFiles = Directory.GetFiles(gdtDirectory, "*.gdt", SearchOption.TopDirectoryOnly);
                foreach (var gdtFile in gdtFiles)
                {
                    var fileName = Path.GetFileName(gdtFile).ToLower();
                    // Common files typically have "common" in the name or start with underscore
                    if (fileName.Contains("common") || fileName.StartsWith("_"))
                    {
                        commonGdtFiles.Add(gdtFile);
                    }
                }

                // Search each common GDT file for the attachment model definition
                foreach (var gdtFile in commonGdtFiles)
                {
                    var lines = File.ReadAllLines(gdtFile);
                    bool inTargetAsset = false;
                    int braceDepth = 0;
                    var definitionLines = new List<string>();
                    var filePaths = new List<string>();
                    string assetType = "";

                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i];

                        // Look for the attachment model asset definition
                        // Example: "attach_t9_laser_01_smg_view" ( "xmodel.gdf" )
                        var headerMatch = Regex.Match(line, @"^\s*""([^""]+)""\s*\(\s*""([^""]+)""\s*\)");
                        if (headerMatch.Success)
                        {
                            var assetName = headerMatch.Groups[1].Value;
                            if (assetName.Equals(attachmentName, StringComparison.OrdinalIgnoreCase))
                            {
                                inTargetAsset = true;
                                assetType = headerMatch.Groups[2].Value;
                                definitionLines.Add(line); // Add header line
                                continue;
                            }
                        }

                        if (inTargetAsset)
                        {
                            definitionLines.Add(line); // Capture every line
                            
                            // Track braces
                            if (line.Contains("{")) braceDepth++;
                            if (line.Contains("}"))
                            {
                                braceDepth--;
                                if (braceDepth == 0)
                                {
                                    // End of this asset definition - we have the complete block
                                    var rawDefinition = string.Join(Environment.NewLine, definitionLines);
                                    
                                    Logger.Info($"Resolved attachment model '{attachmentName}' from {Path.GetFileName(gdtFile)} ({definitionLines.Count} lines)");
                                    
                                    return new AttachmentDefinition
                                    {
                                        Name = attachmentName,
                                        Type = assetType,
                                        RawDefinition = rawDefinition,
                                        FilePaths = filePaths,
                                        SourceGdtPath = gdtFile // Store which GDT this came from
                                    };
                                }
                            }

                            // Extract file paths from known properties
                            var propertyMatch = Regex.Match(line, @"""([^""]+)""\s+""([^""]+)""");
                            if (propertyMatch.Success && braceDepth > 0)
                            {
                                var propertyName = propertyMatch.Groups[1].Value;
                                var propertyValue = propertyMatch.Groups[2].Value.Trim();
                                
                                // Check if this is a file path property
                                if (FilePropertyNames.Contains(propertyName) && 
                                    !string.IsNullOrWhiteSpace(propertyValue) && 
                                    propertyValue != "" &&
                                    !filePaths.Contains(propertyValue))
                                {
                                    filePaths.Add(propertyValue);
                                }
                            }
                        }
                    }
                }

                Logger.Warning($"Could not resolve attachment model: {attachmentName}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error resolving attachment model '{attachmentName}': {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Resolves attachment model references by searching for their definitions in common GDT files
        /// Legacy method - returns only file paths
        /// </summary>
        public static List<string> ResolveAttachmentModel(string attachmentName, string gdtDirectory)
        {
            var filePaths = new List<string>();

            try
            {
                // Search for common GDT files that might contain attachment definitions
                var commonGdtFiles = new List<string>();
                
                // Look for _wpn_t9_common.gdt and similar files
                var gdtFiles = Directory.GetFiles(gdtDirectory, "*.gdt", SearchOption.TopDirectoryOnly);
                foreach (var gdtFile in gdtFiles)
                {
                    var fileName = Path.GetFileName(gdtFile).ToLower();
                    // Common files typically have "common" in the name or start with underscore
                    if (fileName.Contains("common") || fileName.StartsWith("_"))
                    {
                        commonGdtFiles.Add(gdtFile);
                    }
                }

                // Search each common GDT file for the attachment model definition
                foreach (var gdtFile in commonGdtFiles)
                {
                    var lines = File.ReadAllLines(gdtFile);
                    bool inTargetAsset = false;
                    int braceDepth = 0;

                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i].Trim();

                        // Look for the attachment model asset definition
                        // Example: "attach_t9_laser_01_smg_view" ( "xmodel.gdf" )
                        var headerMatch = Regex.Match(line, @"^\s*""([^""]+)""\s*\(\s*""([^""]+)""\s*\)");
                        if (headerMatch.Success)
                        {
                            var assetName = headerMatch.Groups[1].Value;
                            if (assetName.Equals(attachmentName, StringComparison.OrdinalIgnoreCase))
                            {
                                inTargetAsset = true;
                                continue;
                            }
                        }

                        if (inTargetAsset)
                        {
                            // Track braces
                            if (line.Contains("{")) braceDepth++;
                            if (line.Contains("}"))
                            {
                                braceDepth--;
                                if (braceDepth == 0)
                                {
                                    // End of this asset definition
                                    inTargetAsset = false;
                                    break; // Found what we need
                                }
                            }

                            // Extract file paths from known properties
                            var propertyMatch = Regex.Match(line, @"""([^""]+)""\s+""([^""]+)""");
                            if (propertyMatch.Success && braceDepth > 0)
                            {
                                var propertyName = propertyMatch.Groups[1].Value;
                                var propertyValue = propertyMatch.Groups[2].Value.Trim();
                                
                                // Check if this is a file path property
                                if (FilePropertyNames.Contains(propertyName) && 
                                    !string.IsNullOrWhiteSpace(propertyValue) && 
                                    propertyValue != "" &&
                                    !filePaths.Contains(propertyValue))
                                {
                                    filePaths.Add(propertyValue);
                                }
                            }
                        }
                    }

                    // If we found the definition, no need to search more files
                    if (filePaths.Count > 0)
                    {
                        Logger.Info($"Resolved attachment model '{attachmentName}' with {filePaths.Count} file(s) from {Path.GetFileName(gdtFile)}");
                        break;
                    }
                }

                if (filePaths.Count == 0)
                {
                    Logger.Warning($"Could not resolve attachment model: {attachmentName}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error resolving attachment model '{attachmentName}': {ex.Message}");
            }

            return filePaths;
        }

        /// <summary>
        /// Resolves shared weapon sounds by searching for their definitions in GDT files
        /// </summary>
        public static List<string> ResolveSharedWeaponSounds(string sharedSoundName, string gdtDirectory)
        {
            var soundAliases = new List<string>();

            try
            {
                // Search for common GDT files that might contain shared definitions
                var commonGdtFiles = new List<string>();
                
                // Look for _wpn_t9_common.gdt and similar files
                var gdtFiles = Directory.GetFiles(gdtDirectory, "*.gdt", SearchOption.TopDirectoryOnly);
                foreach (var gdtFile in gdtFiles)
                {
                    var fileName = Path.GetFileName(gdtFile).ToLower();
                    // Common files typically have "common" in the name or start with underscore
                    if (fileName.Contains("common") || fileName.StartsWith("_"))
                    {
                        commonGdtFiles.Add(gdtFile);
                    }
                }

                // Search each common GDT file for the shared weapon sound definition
                foreach (var gdtFile in commonGdtFiles)
                {
                    var lines = File.ReadAllLines(gdtFile);
                    bool inTargetAsset = false;
                    int braceDepth = 0;

                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i].Trim();

                        // Look for the shared weapon sound asset definition
                        // Example: "common_t9_rifle_sounds" ( "sharedweaponsounds.gdf" )
                        var headerMatch = Regex.Match(line, @"^\s*""([^""]+)""\s*\(\s*""([^""]+)""\s*\)");
                        if (headerMatch.Success)
                        {
                            var assetName = headerMatch.Groups[1].Value;
                            if (assetName.Equals(sharedSoundName, StringComparison.OrdinalIgnoreCase))
                            {
                                inTargetAsset = true;
                                continue;
                            }
                        }

                        if (inTargetAsset)
                        {
                            // Track braces
                            if (line.Contains("{")) braceDepth++;
                            if (line.Contains("}"))
                            {
                                braceDepth--;
                                if (braceDepth == 0)
                                {
                                    // End of this asset definition
                                    inTargetAsset = false;
                                    break; // Found what we need
                                }
                            }

                            // Extract sound alias values
                            var propertyMatch = Regex.Match(line, @"""([^""]+)""\s+""([^""]+)""");
                            if (propertyMatch.Success && braceDepth > 0)
                            {
                                var propertyValue = propertyMatch.Groups[2].Value.Trim();
                                
                                // Skip empty values
                                if (!string.IsNullOrWhiteSpace(propertyValue) && 
                                    propertyValue != "" &&
                                    !soundAliases.Contains(propertyValue))
                                {
                                    soundAliases.Add(propertyValue);
                                }
                            }
                        }
                    }

                    // If we found the definition, no need to search more files
                    if (soundAliases.Count > 0)
                    {
                        Logger.Info($"Resolved shared weapon sound '{sharedSoundName}' with {soundAliases.Count} sound aliases from {Path.GetFileName(gdtFile)}");
                        break;
                    }
                }

                if (soundAliases.Count == 0)
                {
                    Logger.Warning($"Could not find shared weapon sound definition: {sharedSoundName}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error resolving shared weapon sounds for '{sharedSoundName}'", ex);
            }

            return soundAliases;
        }
    }
}
