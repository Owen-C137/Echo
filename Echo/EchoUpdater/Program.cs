using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace EchoUpdater
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("╔════════════════════════════╗");
            Console.WriteLine("║   Echo Updater v1.0        ║");
            Console.WriteLine("╚════════════════════════════╝");
            Console.WriteLine();

            try
            {
                // Parse arguments
                if (args.Length < 8)
                {
                    Console.WriteLine("Usage: EchoUpdater.exe --zip <path> --install <path> --exe <path> --backup <path>");
                    Console.WriteLine("\nPress any key to exit...");
                    Console.ReadKey();
                    return;
                }

                string? zipPath = null;
                string? installPath = null;
                string? exePath = null;
                string? backupPath = null;

                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "--zip" && i + 1 < args.Length)
                        zipPath = args[i + 1];
                    else if (args[i] == "--install" && i + 1 < args.Length)
                        installPath = args[i + 1];
                    else if (args[i] == "--exe" && i + 1 < args.Length)
                        exePath = args[i + 1];
                    else if (args[i] == "--backup" && i + 1 < args.Length)
                        backupPath = args[i + 1];
                }

                if (zipPath == null || installPath == null || exePath == null || backupPath == null)
                {
                    Console.WriteLine("❌ ERROR: Missing required arguments");
                    Console.WriteLine("\nPress any key to exit...");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine($"📦 Update Package: {Path.GetFileName(zipPath)}");
                Console.WriteLine($"📁 Install Path: {installPath}");
                Console.WriteLine();

                // Step 1: Wait for Echo.exe to close
                Console.Write("⏳ Waiting for Echo.exe to close");
                WaitForProcessToExit("Echo", timeout: 10000);
                Console.WriteLine(" ✅");

                // Step 2: Backup current version
                Console.Write($"💾 Creating backup");
                if (Directory.Exists(backupPath))
                    Directory.Delete(backupPath, true);
                
                CopyDirectory(installPath, backupPath);
                Console.WriteLine(" ✅");

                // Step 3: Extract new version
                Console.Write($"📂 Extracting update");
                ZipFile.ExtractToDirectory(zipPath, installPath, overwriteFiles: true);
                Console.WriteLine(" ✅");

                // Step 4: Cleanup
                Console.Write("🧹 Cleaning up");
                File.Delete(zipPath);
                Console.WriteLine(" ✅");

                // Step 5: Restart Echo
                Console.WriteLine();
                Console.WriteLine("🚀 Restarting Echo...");
                Thread.Sleep(1000);
                
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true,
                    WorkingDirectory = installPath
                });

                Console.WriteLine();
                Console.WriteLine("════════════════════════════");
                Console.WriteLine("✨ Update completed successfully!");
                Console.WriteLine("════════════════════════════");
                Thread.Sleep(2000);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("════════════════════════════");
                Console.WriteLine($"❌ ERROR: {ex.Message}");
                Console.WriteLine("════════════════════════════");
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        static void WaitForProcessToExit(string processName, int timeout)
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
            {
                Console.Write(".");
                return;
            }

            var process = processes[0];
            var startTime = DateTime.Now;
            
            while (!process.HasExited && (DateTime.Now - startTime).TotalMilliseconds < timeout)
            {
                Console.Write(".");
                Thread.Sleep(500);
            }

            if (!process.HasExited)
            {
                throw new Exception($"Process {processName} did not exit within {timeout}ms");
            }
        }

        static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            // Copy files
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                // Skip the updater itself and temporary files
                var fileName = Path.GetFileName(file);
                if (fileName.Equals("EchoUpdater.exe", StringComparison.OrdinalIgnoreCase))
                    continue;

                var destFile = Path.Combine(destDir, fileName);
                File.Copy(file, destFile, overwrite: true);
                Console.Write(".");
            }

            // Copy subdirectories recursively
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }
    }
}
