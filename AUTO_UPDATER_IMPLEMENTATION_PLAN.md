# Echo Auto-Updater Implementation Plan

**Version:** 1.0  
**Date:** October 6, 2025  
**Status:** Completed

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Implementation Phases](#implementation-phases)
4. [GitHub Release Workflow](#github-release-workflow)
5. [Technical Specifications](#technical-specifications)
6. [File Structure](#file-structure)
7. [Update Flow Diagrams](#update-flow-diagrams)
8. [Security & Safety](#security--safety)
9. [Testing Strategy](#testing-strategy)
10. [Deployment Checklist](#deployment-checklist)

---

## 🎯 Overview

### Goal
Implement a robust, one-click auto-updater for Echo that allows users to seamlessly update to the latest version without manual file management.

### Key Requirements
- ✅ Check for updates on app startup
- ✅ Download updates automatically
- ✅ One-click installation
- ✅ Safe rollback on failure
- ✅ Show changelog before updating
- ✅ User can skip versions
- ✅ No interruption of running processes

### Technology Stack
- **Distribution:** GitHub Releases (free, reliable)
- **API:** GitHub REST API v3
- **Updater:** Separate EchoUpdater.exe (C# console app)
- **Download:** HttpClient with progress tracking
- **Extraction:** System.IO.Compression.ZipFile
- **Verification:** SHA256 hash checking

---

## 🏗️ Architecture

### Two-Executable Design

```
Echo Solution/
├── Echo/                          (Main WPF Application)
│   ├── Services/
│   │   ├── UpdateChecker.cs       ← NEW: Checks GitHub for updates
│   │   ├── UpdateDownloader.cs    ← NEW: Downloads update ZIP
│   │   └── UpdateService.cs       ← NEW: Orchestrates update process
│   ├── Models/
│   │   └── UpdateInfo.cs          ← NEW: Update metadata
│   └── Views/
│       └── UpdateDialog.xaml      ← NEW: Update notification UI
│
└── EchoUpdater/                   (Separate Console Application)
    └── Program.cs                 ← NEW: Replaces files and restarts Echo
```

### Why Separate Updater?
1. **Echo.exe can be overwritten** while updater runs
2. **Updater.exe stays small** (~50KB) and fast
3. **Better error recovery** - if update fails, updater handles rollback
4. **Clean separation** of concerns

---

## 📅 Implementation Phases

### Phase 1: Update Detection (Week 1)
**Goal:** Detect when updates are available

**Tasks:**
1. Create `UpdateChecker.cs` service
   - Call GitHub API on startup
   - Parse JSON response
   - Compare versions (current vs latest)
   
2. Create `UpdateInfo.cs` model
   - Version number
   - Download URL
   - Changelog text
   - Release date
   - File size
   - SHA256 hash

3. Add settings
   - `AutoCheckForUpdates` (bool, default: true)
   - `LastUpdateCheck` (DateTime?)
   - `SkippedVersions` (List<string>)

4. Show notification in launcher
   - Subtle banner: "🔔 Update available: v1.1.0"
   - Click to view details

**Deliverable:** App checks for updates and notifies user

---

### Phase 2: Update Dialog & Changelog (Week 2)
**Goal:** Show update information to user

**Tasks:**
1. Create `UpdateDialog.xaml`
   - Show current vs new version
   - Display changelog from GitHub
   - Show file size and release date
   - "Update Now" button
   - "Remind Me Later" button
   - "Skip This Version" checkbox

2. Parse Markdown changelog
   - Convert GitHub release body to readable format
   - Support basic Markdown (bold, lists, headers)

3. Add update settings page
   - Enable/disable auto-check
   - View update history
   - Clear skipped versions

**Deliverable:** Beautiful update dialog with changelog

---

### Phase 3: Download & Progress (Week 2-3)
**Goal:** Download update package

**Tasks:**
1. Create `UpdateDownloader.cs`
   - Download ZIP from GitHub
   - Show progress bar (% complete)
   - Support resume on network failure
   - Save to temp directory

2. Add progress window
   - "Downloading Echo v1.1.0..."
   - Progress bar (0-100%)
   - Download speed (MB/s)
   - Cancel button

3. Verify download
   - Calculate SHA256 hash
   - Compare with GitHub release hash
   - Retry on mismatch (max 3 attempts)

**Deliverable:** Download updates with progress tracking

---

### Phase 4: EchoUpdater.exe (Week 3)
**Goal:** Create separate updater executable

**Tasks:**
1. Create new Console project: `EchoUpdater`
   - Target: .NET 8.0
   - Output: Single-file executable
   - Size: <100KB

2. Implement updater logic:
   ```
   1. Wait for Echo.exe to close (timeout: 10 seconds)
   2. Create backup of current version
   3. Extract new files from ZIP
   4. Replace old files with new files
   5. Delete backup on success
   6. Restart Echo.exe
   7. Exit updater
   ```

3. Command-line parameters:
   ```bash
   EchoUpdater.exe 
     --zip "C:\Temp\Echo-v1.1.0.zip"
     --install "C:\Program Files\Echo"
     --exe "C:\Program Files\Echo\Echo.exe"
     --backup "C:\Temp\Echo_Backup_v1.0.0"
   ```

4. Error handling:
   - If update fails → restore from backup
   - If Echo.exe doesn't close → show error dialog
   - Log all actions to UpdaterLog.txt

**Deliverable:** Working updater executable

---

### Phase 5: Integration & One-Click Update (Week 4)
**Goal:** Complete end-to-end update flow

**Tasks:**
1. Create `UpdateService.cs` orchestrator
   - Coordinates all update components
   - Handles state machine:
     ```
     Idle → Checking → UpdateAvailable → Downloading → 
     Downloaded → Installing → Complete → Idle
     ```

2. Implement one-click flow:
   ```csharp
   User clicks "Update Now"
     ↓
   Download ZIP (with progress)
     ↓
   Verify download hash
     ↓
   Launch EchoUpdater.exe with parameters
     ↓
   Close Echo.exe
     ↓
   [Updater takes over]
     ↓
   Backup current version
     ↓
   Extract and replace files
     ↓
   Restart Echo.exe
     ↓
   Show success message
   ```

3. Add update history tracking
   - Log successful updates
   - Show in settings: "Last updated: Oct 6, 2025"

**Deliverable:** Complete one-click auto-updater

---

### Phase 6: Polish & Safety (Week 5)
**Goal:** Production-ready features

**Tasks:**
1. Background update checks
   - Check on startup (if last check > 24 hours)
   - Optional: Check every 6 hours while app running
   - Don't interrupt user workflow

2. Rollback mechanism
   - Keep backup for 7 days
   - "Restore Previous Version" option in settings
   - Automatic rollback if new version crashes

3. Beta/Pre-release support
   - Setting: "Receive beta updates"
   - Filter GitHub pre-releases
   - Show "BETA" badge in update dialog

4. Telemetry (optional, privacy-focused)
   - Track successful updates
   - Track rollbacks
   - Anonymous version statistics

**Deliverable:** Production-ready auto-updater

---

## 📦 GitHub Release Workflow

### Creating a Release (Step-by-Step)

#### 1. Prepare Release Files

**Build the application:**
```bash
cd Echo
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false
```

**Package structure:**
```
Echo-v1.1.0/
├── Echo.exe
├── EchoUpdater.exe
├── Newtonsoft.Json.dll
├── Echo.dll
└── README.txt (installation instructions)
```

**Create ZIP:**
```bash
# PowerShell
Compress-Archive -Path "Echo-v1.1.0\*" -DestinationPath "Echo-v1.1.0-win-x64.zip"
```

**Calculate SHA256:**
```bash
# PowerShell
Get-FileHash "Echo-v1.1.0-win-x64.zip" -Algorithm SHA256 | Select-Object Hash
```

Copy the hash - you'll need it!

---

#### 2. Create GitHub Release

**A. Go to GitHub repository:**
```
https://github.com/YourUsername/Echo/releases/new
```

**B. Fill in release details:**

**Tag version:** (REQUIRED - must match exactly)
```
v1.1.0
```
**Format:** `v{Major}.{Minor}.{Patch}`  
**Examples:** v1.0.0, v1.1.0, v2.0.0

**Release title:**
```
Echo v1.1.0 - Animation Sound Support
```

**Description (Markdown):**
```markdown
## 🎉 What's New

### New Features
- **Animation Sound Support** - Automatically detects sound aliases from reload and first raise animations
- **Improved Changelog Window** - Latest version now shows at the top

### Bug Fixes
- Fixed duplicate animation files in asset tree
- Fixed missing reload sound aliases

### Improvements
- Enhanced GDT parsing performance
- Better error messages for missing files

## 📥 Installation

1. Download `Echo-v1.1.0-win-x64.zip`
2. Extract to your desired location
3. Run `Echo.exe`

**Existing users:** Use the built-in auto-updater (Settings → Check for Updates)

## 🔐 Verification

**SHA256:** `abc123def456...` (paste hash from step 1)

## 📝 Full Changelog

See [CHANGELOG.md](https://github.com/YourUsername/Echo/blob/main/CHANGELOG.md)
```

**C. Upload files:**
- Click "Attach binaries"
- Upload `Echo-v1.1.0-win-x64.zip`
- (Optional) Upload `SHA256SUMS.txt` with hash

**D. Pre-release checkbox:**
- ✅ Check if beta/testing version
- ⬜ Uncheck for stable release

**E. Publish:**
- Click "Publish release"

---

#### 3. Verify Release

**Check GitHub API response:**
```bash
# PowerShell
Invoke-RestMethod -Uri "https://api.github.com/repos/YourUsername/Echo/releases/latest"
```

**Expected response:**
```json
{
  "tag_name": "v1.1.0",
  "name": "Echo v1.1.0 - Animation Sound Support",
  "body": "## 🎉 What's New\n\n...",
  "published_at": "2025-10-07T10:00:00Z",
  "assets": [
    {
      "name": "Echo-v1.1.0-win-x64.zip",
      "browser_download_url": "https://github.com/.../Echo-v1.1.0-win-x64.zip",
      "size": 5242880
    }
  ]
}
```

---

#### 4. Update Echo.csproj Version

**IMPORTANT:** Update version in project file to match release:

```xml
<PropertyGroup>
  <Version>1.1.0</Version>
  <AssemblyVersion>1.1.0.0</AssemblyVersion>
  <FileVersion>1.1.0.0</FileVersion>
</PropertyGroup>
```

Commit and push this change AFTER creating the release.

---

### Release Checklist Template

```markdown
## Pre-Release
- [ ] Update version in Echo.csproj
- [ ] Update CHANGELOG.md
- [ ] Update ChangelogWindow.xaml with new version
- [ ] Test all features manually
- [ ] Run automated tests (if any)
- [ ] Build release version
- [ ] Test release build

## Release
- [ ] Create ZIP package
- [ ] Calculate SHA256 hash
- [ ] Create GitHub release with tag v{version}
- [ ] Upload ZIP file
- [ ] Write release notes
- [ ] Publish release

## Post-Release
- [ ] Test auto-updater with new release
- [ ] Verify download works
- [ ] Announce on social media / Discord
- [ ] Monitor for issues
- [ ] Update documentation site (if any)
```

---

## 🔧 Technical Specifications

### UpdateChecker.cs

```csharp
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Reflection;

namespace Echo.Services
{
    public class UpdateChecker
    {
        private const string GITHUB_API_URL = "https://api.github.com/repos/YourUsername/Echo/releases/latest";
        private static readonly HttpClient _httpClient = new HttpClient();

        static UpdateChecker()
        {
            // GitHub API requires User-Agent header
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Echo-UpdateChecker");
        }

        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                // Get current version
                var currentVersion = GetCurrentVersion();
                Logger.Info($"Current version: {currentVersion}");

                // Call GitHub API
                var response = await _httpClient.GetStringAsync(GITHUB_API_URL);
                var release = JsonSerializer.Deserialize<GitHubRelease>(response);

                if (release == null) return null;

                // Parse version from tag (e.g., "v1.1.0" -> "1.1.0")
                var latestVersionString = release.TagName.TrimStart('v');
                var latestVersion = Version.Parse(latestVersionString);

                Logger.Info($"Latest version: {latestVersion}");

                // Compare versions
                if (latestVersion > currentVersion)
                {
                    // Find the ZIP asset
                    var asset = release.Assets.FirstOrDefault(a => a.Name.EndsWith(".zip"));
                    if (asset == null)
                    {
                        Logger.Warning("No ZIP asset found in release");
                        return null;
                    }

                    return new UpdateInfo
                    {
                        Version = latestVersion,
                        DownloadUrl = asset.BrowserDownloadUrl,
                        Changelog = release.Body,
                        ReleaseDate = release.PublishedAt,
                        FileSize = asset.Size,
                        FileName = asset.Name
                    };
                }

                return null; // Up to date
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to check for updates", ex);
                return null;
            }
        }

        private Version GetCurrentVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version 
                   ?? new Version(1, 0, 0);
        }
    }

    // GitHub API response models
    public class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("body")]
        public string Body { get; set; } = "";

        [JsonPropertyName("published_at")]
        public DateTime PublishedAt { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubAsset> Assets { get; set; } = new();
    }

    public class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = "";

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }
}
```

---

### UpdateInfo.cs

```csharp
using System;

namespace Echo.Models
{
    public class UpdateInfo
    {
        public Version Version { get; set; } = new Version(1, 0, 0);
        public string DownloadUrl { get; set; } = "";
        public string Changelog { get; set; } = "";
        public DateTime ReleaseDate { get; set; }
        public long FileSize { get; set; }
        public string FileName { get; set; } = "";
        
        public string FormattedSize => FormatBytes(FileSize);
        public string VersionString => $"v{Version.Major}.{Version.Minor}.{Version.Build}";

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
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
```

---

### UpdateDownloader.cs

```csharp
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
                // Create temp directory
                var tempDir = Path.Combine(Path.GetTempPath(), "EchoUpdates");
                Directory.CreateDirectory(tempDir);

                var outputPath = Path.Combine(tempDir, fileName);

                // Download with progress
                using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var bytesRead = 0L;

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                int read;

                while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, read);
                    bytesRead += read;

                    // Report progress
                    var progress = totalBytes > 0 ? (int)((bytesRead * 100) / totalBytes) : 0;
                    ProgressChanged?.Invoke(this, new DownloadProgressEventArgs
                    {
                        BytesReceived = bytesRead,
                        TotalBytes = totalBytes,
                        ProgressPercentage = progress
                    });
                }

                Logger.Info($"Downloaded update to: {outputPath}");
                return outputPath;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to download update", ex);
                throw;
            }
        }

        public async Task<bool> VerifyDownloadAsync(string filePath, string expectedHash)
        {
            // TODO: Implement SHA256 verification
            // For now, just check file exists
            return File.Exists(filePath);
        }
    }

    public class DownloadProgressEventArgs : EventArgs
    {
        public long BytesReceived { get; set; }
        public long TotalBytes { get; set; }
        public int ProgressPercentage { get; set; }
    }
}
```

---

### EchoUpdater/Program.cs

```csharp
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
            Console.WriteLine("Echo Updater v1.0");
            Console.WriteLine("==================\n");

            try
            {
                // Parse arguments
                if (args.Length < 4)
                {
                    Console.WriteLine("Usage: EchoUpdater.exe --zip <path> --install <path> --exe <path> --backup <path>");
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
                    Console.WriteLine("ERROR: Missing required arguments");
                    return;
                }

                // Step 1: Wait for Echo.exe to close
                Console.WriteLine("Waiting for Echo.exe to close...");
                WaitForProcessToExit("Echo", timeout: 10000);

                // Step 2: Backup current version
                Console.WriteLine($"Creating backup to: {backupPath}");
                if (Directory.Exists(backupPath))
                    Directory.Delete(backupPath, true);
                
                CopyDirectory(installPath, backupPath);

                // Step 3: Extract new version
                Console.WriteLine($"Extracting update from: {zipPath}");
                ZipFile.ExtractToDirectory(zipPath, installPath, overwriteFiles: true);

                // Step 4: Cleanup
                Console.WriteLine("Cleaning up...");
                File.Delete(zipPath);

                // Step 5: Restart Echo
                Console.WriteLine("Restarting Echo...");
                Thread.Sleep(1000);
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                });

                Console.WriteLine("\n✅ Update completed successfully!");
                Thread.Sleep(2000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ ERROR: {ex.Message}");
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
        }

        static void WaitForProcessToExit(string processName, int timeout)
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0) return;

            var process = processes[0];
            if (!process.WaitForExit(timeout))
            {
                throw new Exception($"Process {processName} did not exit within {timeout}ms");
            }
        }

        static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite: true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }
    }
}
```

---

## 📊 Update Flow Diagrams

### High-Level Flow
```
┌─────────────┐
│ Echo Starts │
└──────┬──────┘
       │
       ▼
┌────────────────────┐
│ Check for Updates  │◄─── Background task (async)
│ (GitHub API)       │
└──────┬─────────────┘
       │
       ▼
  ┌─────────┐
  │ Updated?│
  └────┬────┘
       │
   ┌───┴───┐
   │  Yes  │
   └───┬───┘
       │
       ▼
┌──────────────────┐
│ Show Notification│
│ in Launcher      │
└──────┬───────────┘
       │
 User clicks
       │
       ▼
┌──────────────────┐
│ Update Dialog    │
│ (Show Changelog) │
└──────┬───────────┘
       │
 User clicks
 "Update Now"
       │
       ▼
┌──────────────────┐
│ Download ZIP     │◄─── Show progress bar
└──────┬───────────┘
       │
       ▼
┌──────────────────┐
│ Verify Download  │◄─── Check SHA256 hash
└──────┬───────────┘
       │
       ▼
┌──────────────────┐
│ Launch Updater   │
│ EchoUpdater.exe  │
└──────┬───────────┘
       │
       ▼
┌──────────────────┐
│ Close Echo.exe   │
└──────┬───────────┘
       │
       ▼
┌──────────────────┐
│ Backup Old Files │◄─── EchoUpdater running
└──────┬───────────┘
       │
       ▼
┌──────────────────┐
│ Extract New Files│
└──────┬───────────┘
       │
       ▼
┌──────────────────┐
│ Restart Echo.exe │
└──────┬───────────┘
       │
       ▼
┌──────────────────┐
│ Show Success Msg │
└──────────────────┘
```

---

### Error Handling Flow
```
┌──────────────┐
│ Download Fail│
└──────┬───────┘
       │
       ▼
┌──────────────┐     3 attempts
│ Retry 3 times│─────────────┐
└──────┬───────┘             │
       │                     │
  Still fails?               │
       │                     │
       ▼                     │
┌──────────────┐             │
│ Show Error   │◄────────────┘
│ "Try Later"  │
└──────────────┘

┌──────────────┐
│Extract Fail  │
└──────┬───────┘
       │
       ▼
┌──────────────┐
│ Restore      │
│ From Backup  │
└──────┬───────┘
       │
       ▼
┌──────────────┐
│ Show Error   │
│ "Rollback OK"│
└──────────────┘
```

---

## 🔒 Security & Safety

### Hash Verification
```csharp
using System.Security.Cryptography;

public async Task<string> CalculateSHA256(string filePath)
{
    using var sha256 = SHA256.Create();
    using var stream = File.OpenRead(filePath);
    var hash = await sha256.ComputeHashAsync(stream);
    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
}

public async Task<bool> VerifyDownload(string filePath, string expectedHash)
{
    var actualHash = await CalculateSHA256(filePath);
    var match = actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
    
    if (!match)
    {
        Logger.Warning($"Hash mismatch! Expected: {expectedHash}, Got: {actualHash}");
    }
    
    return match;
}
```

### Backup Strategy
- Create backup before updating
- Keep backup for 7 days
- Auto-delete old backups (>7 days)
- Manual "Restore Previous Version" in settings

### Rollback Triggers
1. Update extraction fails
2. New version crashes on startup (3+ crashes in 5 minutes)
3. User manually chooses to rollback

### Permissions
- **Windows:** May require admin for Program Files installation
- **Solution:** Recommend installing to user directory (C:\Users\{user}\AppData\Local\Echo)
- **Alternative:** Use ClickOnce deployment (auto-handles updates)

---

## 🧪 Testing Strategy

### Unit Tests
```csharp
[TestClass]
public class UpdateCheckerTests
{
    [TestMethod]
    public async Task CheckForUpdates_WhenNewerVersionAvailable_ReturnsUpdateInfo()
    {
        var checker = new UpdateChecker();
        var updateInfo = await checker.CheckForUpdatesAsync();
        
        // Mock GitHub API response
        Assert.IsNotNull(updateInfo);
        Assert.IsTrue(updateInfo.Version > new Version(1, 0, 0));
    }
    
    [TestMethod]
    public async Task CheckForUpdates_WhenUpToDate_ReturnsNull()
    {
        // Test when current version matches latest
    }
}
```

### Manual Testing Checklist
- [ ] Test update check on startup
- [ ] Test update dialog displays correctly
- [ ] Test download with slow internet (throttle)
- [ ] Test download cancellation
- [ ] Test hash verification failure
- [ ] Test updater with missing permissions
- [ ] Test rollback after failed update
- [ ] Test "Skip This Version" functionality
- [ ] Test multiple rapid updates (1.0 → 1.1 → 1.2)

### Edge Cases
1. **No internet connection** - Gracefully fail, don't block startup
2. **GitHub API rate limit** - Cache last check, retry later
3. **Corrupted download** - Hash mismatch triggers re-download
4. **Updater crashes** - Original version stays intact
5. **User closes app during update** - Updater continues or cancels safely

---

## ✅ Deployment Checklist

### Pre-Release
- [ ] Increment version in `Echo.csproj`
- [ ] Update `ChangelogWindow.xaml` with new version
- [ ] Update `CHANGELOG.md` file
- [ ] Test all features manually
- [ ] Build Release configuration
- [ ] Test release build (run on clean machine)

### Build Process
```bash
# Build main app
cd Echo
dotnet publish -c Release -r win-x64 --self-contained false

# Build updater
cd ../EchoUpdater
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Package
mkdir Echo-v1.1.0
copy Echo\bin\Release\net8.0-windows\win-x64\publish\* Echo-v1.1.0\
copy EchoUpdater\bin\Release\net8.0\win-x64\publish\EchoUpdater.exe Echo-v1.1.0\

# Create ZIP
Compress-Archive -Path Echo-v1.1.0\* -DestinationPath Echo-v1.1.0-win-x64.zip

# Calculate hash
Get-FileHash Echo-v1.1.0-win-x64.zip -Algorithm SHA256
```

### GitHub Release
- [ ] Create tag: `v{version}`
- [ ] Write release notes (features, fixes, improvements)
- [ ] Upload ZIP file
- [ ] Include SHA256 hash in description
- [ ] Mark as pre-release if beta
- [ ] Publish release

### Post-Release
- [ ] Test auto-updater downloads new version
- [ ] Verify update installs correctly
- [ ] Check updater restarts app successfully
- [ ] Monitor for crash reports
- [ ] Announce release (social media, Discord, etc.)

---

## 📝 Notes & Considerations

### GitHub API Rate Limits
- **Unauthenticated:** 60 requests/hour per IP
- **Authenticated:** 5000 requests/hour
- **Solution:** Cache last check, only check once per 24 hours by default
- **Future:** Add GitHub token for higher limits (optional)

### Portable vs Installed
- **Portable:** Simple, no updater admin issues
- **Installed:** Better Windows integration, might need admin for updates
- **Recommendation:** Start with portable deployment

### Alternative: ClickOnce
- **Pros:** Built-in auto-update, code signing, easy deployment
- **Cons:** Less control, Windows-only, requires certificate for trust
- **Decision:** Stick with custom updater for more control

### Code Signing
- **Important:** Users may see "Unknown Publisher" warning
- **Solution:** Get code signing certificate (~$100-300/year)
- **Alternative:** Build trust through community, open source

### Future Enhancements
- [ ] Delta updates (only download changed files)
- [ ] Auto-update in background (download while app runs)
- [ ] Notification system (toast/tray icon)
- [ ] Update scheduling (update at specific time)
- [ ] Multi-platform support (macOS, Linux)

---

## 🎯 Success Metrics

### User Experience Goals
- ✅ Update check completes in <2 seconds
- ✅ Download progress is accurate within 5%
- ✅ Update installs in <30 seconds
- ✅ App restarts automatically after update
- ✅ User sees changelog before updating
- ✅ Zero data loss during update

### Technical Goals
- ✅ 99% update success rate
- ✅ <1% rollback rate
- ✅ Support for offline use (updates optional)
- ✅ <100KB updater executable size
- ✅ Compatible with Windows 10/11

---

## 📚 Resources

### GitHub API Documentation
- **Releases:** https://docs.github.com/en/rest/releases/releases
- **Rate Limiting:** https://docs.github.com/en/rest/overview/resources-in-the-rest-api#rate-limiting
- **Authentication:** https://docs.github.com/en/rest/overview/authenticating-to-the-rest-api

### .NET Documentation
- **System.IO.Compression:** https://learn.microsoft.com/en-us/dotnet/api/system.io.compression
- **HttpClient:** https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient
- **Process Management:** https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process

### Best Practices
- **Semantic Versioning:** https://semver.org/
- **Application Updates:** https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/desktop-to-uwp-extensions

---

## 📞 Support & Feedback

When implemented, users should be able to:
1. Check for updates manually (Settings → Check for Updates)
2. Enable/disable auto-check (Settings → Update Settings)
3. View update history (Settings → About)
4. Report update issues (include UpdaterLog.txt)

---

**Document Version:** 1.0  
**Last Updated:** October 6, 2025  
**Status:** Ready for Implementation  
**Estimated Implementation Time:** 4-5 weeks

---

## 🚀 Ready to Start?

When you come back, we'll begin with **Phase 1: Update Detection**

**First task:** Create `UpdateChecker.cs` service to call GitHub API

Let's build this! 💪
