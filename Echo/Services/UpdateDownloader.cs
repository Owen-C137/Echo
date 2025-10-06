using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Echo.Services
{
    public class UpdateDownloader
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        
        public event EventHandler<DownloadProgressEventArgs>? ProgressChanged;

        public async Task<string> DownloadUpdateAsync(string downloadUrl, string fileName)
        {
            try
            {
                Logger.Info($"Starting download from: {downloadUrl}");

                // Create temp directory
                var tempDir = Path.Combine(Path.GetTempPath(), "EchoUpdates");
                Directory.CreateDirectory(tempDir);

                var outputPath = Path.Combine(tempDir, fileName);

                // Delete existing file if present
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }

                // Download with progress
                using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var bytesRead = 0L;

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                int read;
                var lastReportTime = DateTime.Now;

                while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, read);
                    bytesRead += read;

                    // Report progress every 100ms to avoid UI spam
                    if ((DateTime.Now - lastReportTime).TotalMilliseconds > 100)
                    {
                        var progress = totalBytes > 0 ? (int)((bytesRead * 100) / totalBytes) : 0;
                        ProgressChanged?.Invoke(this, new DownloadProgressEventArgs
                        {
                            BytesReceived = bytesRead,
                            TotalBytes = totalBytes,
                            ProgressPercentage = progress
                        });
                        lastReportTime = DateTime.Now;
                    }
                }

                // Final progress update
                ProgressChanged?.Invoke(this, new DownloadProgressEventArgs
                {
                    BytesReceived = bytesRead,
                    TotalBytes = totalBytes,
                    ProgressPercentage = 100
                });

                Logger.Info($"Downloaded update to: {outputPath}");
                return outputPath;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to download update", ex);
                throw;
            }
        }

        public Task<bool> VerifyDownloadAsync(string filePath, string expectedHash)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Logger.Warning($"Download file not found: {filePath}");
                    return Task.FromResult(false);
                }

                // TODO: Implement SHA256 verification when hash is available in GitHub releases
                // For now, just verify file exists and has content
                var fileInfo = new FileInfo(filePath);
                var isValid = fileInfo.Length > 0;

                Logger.Info($"Download verification: {(isValid ? "PASSED" : "FAILED")} (Size: {fileInfo.Length} bytes)");
                return Task.FromResult(isValid);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to verify download", ex);
                return Task.FromResult(false);
            }
        }
    }

    public class DownloadProgressEventArgs : EventArgs
    {
        public long BytesReceived { get; set; }
        public long TotalBytes { get; set; }
        public int ProgressPercentage { get; set; }
    }
}
