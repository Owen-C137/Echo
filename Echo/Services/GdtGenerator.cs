using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Echo.Services
{
    public static class GdtGenerator
    {
        /// <summary>
        /// Generates a GDT file containing only the specified attachment definitions
        /// </summary>
        public static string GenerateAttachmentGdt(List<AttachmentDefinition> attachments)
        {
            var sb = new StringBuilder();
            
            // Opening brace
            sb.AppendLine("{");
            
            // Add each attachment definition
            foreach (var attachment in attachments)
            {
                // Add the raw definition (already includes formatting, tabs, braces, etc.)
                sb.AppendLine(attachment.RawDefinition);
            }
            
            // Closing brace
            sb.AppendLine("}");
            
            return sb.ToString();
        }

        /// <summary>
        /// Creates attachment GDT files in the package temporary directory
        /// Groups attachments by source GDT and creates one file per source
        /// </summary>
        public static void CreateAttachmentGdtFile(string tempDir, string originalGdtPath, List<AttachmentDefinition> attachments, string bo3RootPath)
        {
            if (attachments == null || attachments.Count == 0)
            {
                Logger.Info("No attachments to generate GDT for");
                return;
            }

            try
            {
                // Group attachments by source GDT file
                var groupedBySource = attachments.GroupBy(a => a.SourceGdtPath);

                foreach (var group in groupedBySource)
                {
                    var sourceGdtPath = group.Key;
                    var attachmentsList = group.ToList();

                    if (string.IsNullOrEmpty(sourceGdtPath) || !File.Exists(sourceGdtPath))
                    {
                        Logger.Warning($"Source GDT path is invalid for {attachmentsList.Count} attachments, skipping...");
                        continue;
                    }

                    // Get the relative path of the SOURCE GDT from BO3 root
                    // Example: source_data\t9_weapons\_wpn_t9_common.gdt
                    // Output: source_data\t9_weapons\_wpn_t9_common.gdt (EXACT SAME NAME)
                    var gdtRelativePath = GetRelativePathFromBo3Root(sourceGdtPath, bo3RootPath);
                    
                    // Use the exact same path as the source GDT
                    var attachmentGdtRelativePath = gdtRelativePath;
                    var attachmentGdtFullPath = Path.Combine(tempDir, attachmentGdtRelativePath);
                    
                    // Create directory if it doesn't exist
                    var destDir = Path.GetDirectoryName(attachmentGdtFullPath);
                    if (!string.IsNullOrEmpty(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }
                    
                    // Generate GDT content for this group
                    var gdtContent = GenerateAttachmentGdt(attachmentsList);
                    
                    // Write to file
                    File.WriteAllText(attachmentGdtFullPath, gdtContent);
                    
                    Logger.Info($"Created attachment GDT: {attachmentGdtRelativePath} ({attachmentsList.Count} attachments from {Path.GetFileName(sourceGdtPath)})");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to create attachment GDT: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the relative path of a file from the BO3 root directory
        /// </summary>
        private static string GetRelativePathFromBo3Root(string fullPath, string bo3RootPath)
        {
            var fullUri = new Uri(fullPath);
            var rootUri = new Uri(bo3RootPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
            
            var relativeUri = rootUri.MakeRelativeUri(fullUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());
            
            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
