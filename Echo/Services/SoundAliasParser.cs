using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Echo.Services
{
    public class SoundAlias
    {
        public string AliasName { get; set; } = string.Empty;
        public List<string> FilePaths { get; set; } = new List<string>();
        public List<string> SecondaryAliases { get; set; } = new List<string>(); // Aliases that this alias chains to
        public string SourceCsvFile { get; set; } = string.Empty;
    }

    public class SoundAliasParseResult
    {
        public Dictionary<string, SoundAlias> Aliases { get; set; } = new Dictionary<string, SoundAlias>(StringComparer.OrdinalIgnoreCase);
        public int TotalAliases { get; set; }
        public int TotalSoundFiles { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public static class SoundAliasParser
    {
        /// <summary>
        /// Parse all CSV files in the BO3 sound/aliases directory
        /// </summary>
        public static SoundAliasParseResult ParseAllAliasCsvFiles(string bo3RootPath)
        {
            var result = new SoundAliasParseResult();

            try
            {
                var aliasDir = Path.Combine(bo3RootPath, "share", "raw", "sound", "aliases");
                
                if (!Directory.Exists(aliasDir))
                {
                    result.Errors.Add($"Sound aliases directory not found: {aliasDir}");
                    Logger.Error($"Sound aliases directory not found: {aliasDir}");
                    return result;
                }

                Logger.Info($"Parsing sound alias CSV files from: {aliasDir}");

                var csvFiles = Directory.GetFiles(aliasDir, "*.csv", SearchOption.TopDirectoryOnly);
                
                foreach (var csvFile in csvFiles)
                {
                    try
                    {
                        ParseCsvFile(csvFile, result);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Error parsing CSV {Path.GetFileName(csvFile)}: {ex.Message}");
                        Logger.Error($"Failed to parse CSV file: {csvFile}", ex);
                    }
                }

                result.TotalAliases = result.Aliases.Count;
                result.TotalSoundFiles = result.Aliases.Values.Sum(a => a.FilePaths.Count);

                Logger.Info($"Sound alias parsing complete: {result.TotalAliases} aliases, {result.TotalSoundFiles} sound files");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error parsing sound aliases: {ex.Message}");
                Logger.Error("Failed to parse sound alias CSV files", ex);
            }

            return result;
        }

        /// <summary>
        /// Parse a single CSV alias file
        /// </summary>
        private static void ParseCsvFile(string csvFilePath, SoundAliasParseResult result)
        {
            var lines = File.ReadAllLines(csvFilePath);
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                    continue; // Skip comments and empty lines

                // CSV format: alias_name,field1,field2,file_path1,field3,field4,field5,secondary_alias1,secondary_alias2,...
                // The file paths are typically in column 3 (index 3), and secondary aliases can appear in various positions
                var fields = line.Split(',');
                
                if (fields.Length < 2)
                    continue;

                var aliasName = fields[0].Trim();
                if (string.IsNullOrWhiteSpace(aliasName))
                    continue;

                // Create or get existing alias entry
                if (!result.Aliases.TryGetValue(aliasName, out var alias))
                {
                    alias = new SoundAlias
                    {
                        AliasName = aliasName,
                        SourceCsvFile = Path.GetFileName(csvFilePath)
                    };
                    result.Aliases[aliasName] = alias;
                }

                // Parse fields looking for file paths (contain .wav, .mp3, etc.) and secondary aliases
                for (int i = 1; i < fields.Length; i++)
                {
                    var field = fields[i].Trim();
                    if (string.IsNullOrWhiteSpace(field))
                        continue;

                    // Check if it's a file path (contains .wav, .mp3, .flac, etc.)
                    if (field.Contains(".wav", StringComparison.OrdinalIgnoreCase) ||
                        field.Contains(".mp3", StringComparison.OrdinalIgnoreCase) ||
                        field.Contains(".flac", StringComparison.OrdinalIgnoreCase))
                    {
                        // Normalize path separators
                        var normalizedPath = field.Replace("\\", "/");
                        
                        if (!alias.FilePaths.Contains(normalizedPath))
                        {
                            alias.FilePaths.Add(normalizedPath);
                        }
                    }
                    // Check if it references another alias (starts with wpn_, evt_, amb_, mus_, etc.)
                    else if (IsLikelyAliasReference(field))
                    {
                        if (!alias.SecondaryAliases.Contains(field))
                        {
                            alias.SecondaryAliases.Add(field);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Resolve a sound alias to all its actual file paths (following the chain)
        /// </summary>
        public static List<string> ResolveAliasToFiles(string aliasName, Dictionary<string, SoundAlias> allAliases, HashSet<string>? visitedAliases = null)
        {
            var filePaths = new List<string>();
            
            // Prevent infinite loops
            if (visitedAliases == null)
                visitedAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            if (visitedAliases.Contains(aliasName))
                return filePaths; // Already visited
            
            visitedAliases.Add(aliasName);

            // Find the alias
            if (!allAliases.TryGetValue(aliasName, out var alias))
                return filePaths; // Alias not found

            // Add direct file paths
            filePaths.AddRange(alias.FilePaths);

            // Recursively resolve secondary aliases
            foreach (var secondaryAlias in alias.SecondaryAliases)
            {
                var secondaryFiles = ResolveAliasToFiles(secondaryAlias, allAliases, visitedAliases);
                filePaths.AddRange(secondaryFiles);
            }

            return filePaths.Distinct().ToList();
        }

        /// <summary>
        /// Check if a string looks like an alias reference
        /// </summary>
        private static bool IsLikelyAliasReference(string value)
        {
            // Common alias prefixes in BO3
            var prefixes = new[] { "wpn_", "evt_", "amb_", "mus_", "vox_", "fly_", "foley_", "ui_", "zmb_", "veh_", "chr_" };
            
            foreach (var prefix in prefixes)
            {
                if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Create a consolidated CSV file with only the referenced aliases
        /// </summary>
        public static void CreateConsolidatedCsv(HashSet<string> referencedAliases, Dictionary<string, SoundAlias> allAliases, string outputPath)
        {
            try
            {
                Logger.Info($"Creating consolidated sound alias CSV: {outputPath}");

                // Recursively resolve all aliases needed
                var allNeededAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var aliasName in referencedAliases)
                {
                    CollectAllNeededAliases(aliasName, allAliases, allNeededAliases);
                }

                // Build CSV content
                var csvLines = new List<string>();
                
                // Add required CSV header (first line MUST be this exact header for BO3)
                csvLines.Add("Name,Behavior,Storage,FileSpec,FileSpecSustain,FileSpecRelease,Template,Loadspec,Secondary,SustainAlias,ReleaseAlias,Bus,VolumeGroup,DuckGroup,Duck,ReverbSend,CenterSend,VolMin,VolMax,DistMin,DistMaxDry,DistMaxWet,DryMinCurve,DryMaxCurve,WetMinCurve,WetMaxCurve,LimitCount,LimitType,EntityLimitCount,EntityLimitType,PitchMin,PitchMax,PriorityMin,PriorityMax,PriorityThresholdMin,PriorityThresholdMax,AmplitudePriority,PanType,Pan,Futz,Looping,RandomizeType,Probability,StartDelay,EnvelopMin,EnvelopMax,EnvelopPercent,OcclusionLevel,IsBig,DistanceLpf,FluxType,FluxTime,Subtitle,Doppler,ContextType,ContextValue,ContextType1,ContextValue1,ContextType2,ContextValue2,ContextType3,ContextValue3,Timescale,IsMusic,IsCinematic,FadeIn,FadeOut,Pauseable,StopOnEntDeath,Compression,StopOnPlay,DopplerScale,FutzPatch,VoiceLimit,IgnoreMaxDist,NeverPlayTwice,ContinuousPan,FileSource,FileSourceSustain,FileSourceRelease,FileTarget,FileTargetSustain,FileTargetRelease,Platform,Language,OutputDevices,PlatformMask,WiiUMono,StopAlias,DistanceLpfMin,DistanceLpfMax,FacialAnimationName,RestartContextLoops,SilentInCPZ,ContextFailsafe,GPAD,GPADOnly,MuteVoice,MuteMusic,RowSourceFileName,RowSourceShortName,RowSourceLineNumber");
                
                // Add comment header
                csvLines.Add("# Consolidated sound aliases - Generated by Echo GDT Packer");
                csvLines.Add("# Original aliases from multiple CSV files");
                csvLines.Add("");

                foreach (var aliasName in allNeededAliases.OrderBy(a => a))
                {
                    if (allAliases.TryGetValue(aliasName, out var alias))
                    {
                        // Reconstruct CSV line: alias_name,,,file_paths...,,,secondary_aliases...
                        var fields = new List<string> { alias.AliasName, "", "" };
                        
                        // Add file paths
                        fields.AddRange(alias.FilePaths);
                        
                        // Add some spacing
                        while (fields.Count < 8)
                            fields.Add("");
                        
                        // Add secondary aliases
                        fields.AddRange(alias.SecondaryAliases);
                        
                        csvLines.Add(string.Join(",", fields));
                    }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                File.WriteAllLines(outputPath, csvLines);

                Logger.Info($"Consolidated CSV created with {allNeededAliases.Count} aliases");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to create consolidated CSV", ex);
                throw;
            }
        }

        /// <summary>
        /// Recursively collect all aliases needed (including chained references)
        /// </summary>
        private static void CollectAllNeededAliases(string aliasName, Dictionary<string, SoundAlias> allAliases, HashSet<string> collected)
        {
            if (collected.Contains(aliasName))
                return;

            collected.Add(aliasName);

            if (allAliases.TryGetValue(aliasName, out var alias))
            {
                foreach (var secondaryAlias in alias.SecondaryAliases)
                {
                    CollectAllNeededAliases(secondaryAlias, allAliases, collected);
                }
            }
        }
    }
}
