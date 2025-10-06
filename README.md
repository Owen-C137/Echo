# Echo - Black Ops 3 GDT Parser & Packer

<div align="center">

**A comprehensive tool for parsing, analyzing, and packaging Black Ops 3 GDT files with all their asset dependencies.**

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/download)
[![Windows](https://img.shields.io/badge/Platform-Windows-blue)](https://www.microsoft.com/windows)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![GitHub Release](https://img.shields.io/github/v/release/Owen-C137/Echo)](https://github.com/Owen-C137/Echo/releases)

</div>

> **🤖 Development Transparency Notice**  
> This project was created with significant assistance from AI (GitHub Copilot & Claude). The AI handled most of the code generation, architecture, and implementation. As the project creator, I provided the vision, requirements, testing, and iteration feedback. I believe in being transparent about the development process and the role AI played in bringing this tool to life.
> 
> **All source code is open and available for review** - you can inspect every line to verify there's no malicious code, telemetry, or hidden functionality. The application only connects to GitHub's public API for update checks (which you can disable or verify in the source). This project is MIT licensed and built for the community.

---

## ✨ Features

### 🔍 **GDT Parser**
- Parse and extract assets from Black Ops 3 GDT (Game Data Table) files
- Supports **50+ file type extensions**
- Detects **70+ unique asset properties**
- Handles attachment definitions and references automatically
- **Performance**: ~1,000 assets/second

### 📂 **Asset Scanner**
- Intelligent file resolution system
- Scans BO3 installation for referenced assets
- Resolves attachment dependencies automatically
- Reports missing files with detailed logging
- **Performance**: ~5,000 files/second

### 🔊 **Sound Alias Parser**
- Parse CSV alias files for sound references
- **Three packaging modes**:
  - **Consolidated**: Merge all aliases into one file
  - **Copy Full**: Include complete original CSV files
  - **Skip**: Exclude sound files entirely
- **Performance**: ~10,000 aliases/second

### 📦 **Package Creator**
- ZIP package generation with preserved BO3 directory structure
- Includes manifest with metadata
- Optional attachment GDT generation
- Configurable compression levels (None/Fastest/Optimal)
- Missing files report generation

### 🎨 **Modern UI**
- **Dark Theme**: Professional, consistent interface
- **Launcher Window**: Central hub for tool selection
- **GDT Packer Window**: Main packaging interface with asset tree
- **Real-time Progress**: Live feedback during package creation
- **Settings Manager**: Comprehensive configuration options

### 🔄 **Auto-Update System**
- Automatic update checks on startup
- Manual update check button
- Visual update notification banner
- GitHub integration for seamless updates
- Safe update process with automatic backup

---

## 🚀 Getting Started

### Prerequisites

- **Windows 10/11** (64-bit)
- **.NET 8.0 Runtime** ([Download here](https://dotnet.microsoft.com/download/dotnet/8.0))
- **Black Ops 3** installation (for packaging features)

### Installation

1. Download the latest release from [Releases](https://github.com/Owen-C137/Echo/releases)
2. Extract the ZIP file to a folder of your choice
3. Run `Echo.exe`
4. Configure your Black Ops 3 installation path in Settings

### Building from Source

```powershell
# Clone the repository
git clone https://github.com/Owen-C137/Echo.git
cd Echo

# (Optional) Configure GitHub token for higher API rate limits
# Copy .env.example to .env and add your GitHub token
cp .env.example .env
# Edit .env and replace 'your_github_token_here' with your token

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run --project Echo\Echo.csproj
```

**Note on GitHub Token (Optional):**
- Without a token: 60 API requests/hour (sufficient for normal use)
- With a token: 5000 API requests/hour
- Generate token at: https://github.com/settings/tokens (only needs `public_repo` read access)
- Add to `.env` file (this file is gitignored and never pushed)

---

## 📖 Usage Guide

### 1. Configure Settings
- Open **Settings** from the launcher
- Set your **Black Ops 3 installation path**
- Choose your preferred **sound alias handling mode**
- Enable/disable **attachment GDT generation**
- Select **compression level** (Optimal recommended)

### 2. Create a Package
1. Click **GDT Packer** from the launcher
2. Click **Add GDT Files** and select your weapon/asset GDT files
3. Click **Scan Assets** to resolve all dependencies
4. Review the asset tree and statistics
5. Enter a **package name**
6. Click **Create Package** and wait for completion
7. Find your package in the same directory as your GDT files

### 3. Package Contents
Your generated package will include:
- All referenced asset files (models, images, sounds, etc.)
- GDT files with proper directory structure
- Sound alias CSV files (based on your settings)
- Optional attachment GDT file
- Manifest file with package metadata
- Missing files report (if any files weren't found)

---

## 🏗️ Project Structure

```
Echo_GDTParser_Packer/
├── Echo/                           # Main application
│   ├── Services/                   # Core business logic
│   │   ├── GdtParser.cs           # GDT file parsing engine
│   │   ├── AssetScanner.cs        # File system asset resolution
│   │   ├── SoundAliasParser.cs    # CSV sound alias parsing
│   │   ├── PackageCreator.cs      # ZIP package generation
│   │   ├── UpdateChecker.cs       # GitHub release checking
│   │   ├── UpdateService.cs       # Update orchestration
│   │   ├── UpdateDownloader.cs    # Download management
│   │   ├── SettingsManager.cs     # App settings persistence
│   │   └── Logger.cs              # Logging infrastructure
│   ├── Views/                      # UI Windows
│   │   ├── LauncherWindow.xaml    # Main launcher
│   │   ├── GdtPackerWindow.xaml   # GDT packing interface
│   │   ├── SettingsWindow.xaml    # Configuration
│   │   ├── UpdateDialog.xaml      # Update prompt
│   │   └── ...
│   └── Styles/
│       └── DarkTheme.xaml         # Consistent dark theme
├── EchoUpdater/                    # Separate update installer
│   └── Program.cs                 # Update execution logic
├── CHANGELOG.md                    # Version history
└── README.md                       # This file
```

---

## 📊 Performance Metrics

- **GDT Parsing**: ~1,000 assets/second
- **File Scanning**: ~5,000 files/second  
- **Sound CSV Parsing**: ~10,000 aliases/second
- **Package Creation**: Limited by disk I/O (~50-100 MB/s)

---

## 🔧 Supported Asset Types

Echo recognizes and packages **50+ file types** including:

**Models & Animations**
- `.xmodel_bin`, `.xmodel_export`
- `.xanim_bin`, `.xanim_export`

**Images & Materials**
- `.png`, `.jpg`, `.dds`, `.tga`
- Material definitions

**Sounds**
- `.wav`, `.flac`, `.mp3`
- Sound alias CSV files

**Scripts**
- `.gsc` (GameScript)
- `.csc` (ClientScript)

**Effects**
- Particle effects
- Beam definitions

**And many more...**

---

## ⚙️ Configuration

Settings are stored in `Settings.json` in the application directory:

```json
{
  "BlackOps3Path": "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Call of Duty Black Ops III",
  "SoundAliasHandling": 0,
  "IncludeAttachmentGdt": true,
  "CompressionLevel": 2
}
```

### Sound Alias Handling Modes
- **0**: Consolidated (merge all into one CSV)
- **1**: Copy Full (include complete original files)
- **2**: Skip (exclude sound files)

### Compression Levels
- **0**: None (fastest, largest)
- **1**: Fastest (quick, larger)
- **2**: Optimal (slower, smallest) - **Recommended**

---

## 🐛 Troubleshooting

### "BO3 Path Not Set" Error
- Open Settings and configure your Black Ops 3 installation path
- Path should point to the root BO3 folder (contains `BlackOps3.exe`)

### Missing Files in Package
- Check the `missing_files.txt` report in your package
- Verify your BO3 installation is complete
- Some custom assets may not be in the standard BO3 installation

### Update Check Fails
- Ensure you have an internet connection
- Check firewall settings for Echo.exe
- GitHub API may be temporarily unavailable

---

## 📝 Changelog

See [CHANGELOG.md](CHANGELOG.md) for a detailed version history.

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- Built for the **Black Ops 3 modding community**
- Special thanks to all contributors and testers
- Inspired by the need for better GDT asset management tools

---

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/Owen-C137/Echo/issues)
- **Discussions**: [GitHub Discussions](https://github.com/Owen-C137/Echo/discussions)
- **Updates**: Check the [Releases](https://github.com/Owen-C137/Echo/releases) page

---

<div align="center">

**Made with ❤️ for the Black Ops 3 modding community**

⭐ **Star this repo** if you find it useful!

</div>

On first run, open Settings (File → Settings) and configure:
- **Black Ops III Path**: The root installation directory of Black Ops III

Settings are automatically saved to `settings.json` in the application directory.

## Logging

All application events are logged to `Logs.txt` in the application directory. You can view the logs by selecting Help → View Logs from the menu.

## Development Roadmap

- [ ] AGDT parsing functionality
- [ ] AGDT packing functionality
- [ ] File browser/explorer
- [ ] Batch operations
- [ ] Advanced validation

## License

This project is provided as-is for educational and modding purposes.
