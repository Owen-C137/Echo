# Changelog

All notable changes to Echo - Black Ops 3 GDT Parser & Packer will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-10-06

### 🎉 Initial Release

**Echo** is a comprehensive tool for parsing, analyzing, and packaging Black Ops 3 GDT (Game Data Table) files with all their asset dependencies.

### ✨ Features

#### Core Functionality
- **GDT Parser**: Parse and extract assets from Black Ops 3 GDT files
  - Supports 50+ file type extensions
  - Detects 70+ unique asset properties
  - Handles attachment definitions and references
  - Performance: ~1,000 assets/second

- **Asset Scanner**: Intelligent file resolution system
  - Scans BO3 installation for referenced assets
  - Resolves attachment dependencies automatically
  - Reports missing files with detailed logging
  - Performance: ~5,000 files/second

- **Sound Alias Parser**: Complete sound system support
  - Parse CSV alias files for sound references
  - Three packaging modes:
    - Consolidated: Merge all aliases into one file
    - Copy Full: Include complete original CSV files
    - Skip: Exclude sound files entirely
  - Performance: ~10,000 aliases/second

- **Package Creator**: ZIP package generation
  - Preserves BO3 directory structure
  - Includes manifest with metadata
  - Optional attachment GDT generation
  - Configurable compression levels (None/Fastest/Optimal)
  - Missing files report generation

#### User Interface
- **Modern Dark Theme**: Consistent, professional UI throughout
- **Launcher Window**: Central hub for tool selection
  - Quick access to GDT Packer
  - Settings, Documentation, and Changelog
  - BO3 path display and version info

- **GDT Packer Window**: Main packaging interface
  - Drag-and-drop GDT file loading
  - Real-time asset scanning and analysis
  - Asset tree visualization
  - Package creation with live progress
  - Detailed statistics and logging

- **Settings Window**: Comprehensive configuration
  - BO3 installation path setup
  - Sound alias handling modes
  - Attachment GDT generation toggle
  - Compression level selection
  - Persistent settings storage

- **Package Progress Window**: Real-time feedback
  - Live progress bar with percentage
  - Detailed operation logging
  - File copy tracking
  - Success/failure reporting

#### Auto-Update System
- **Automatic Update Checks**: Check for updates on startup
- **Manual Update Check**: Button in launcher footer
- **Update Notification Banner**: Visual indicator when updates available
- **GitHub Integration**: Pulls releases from Owen-C137/Echo repository
- **Download & Install**: Automated update process with progress tracking
- **EchoUpdater**: Separate updater executable for safe file replacement

#### Developer Features
- **Comprehensive Logging**: Detailed logs with timestamps
- **Error Handling**: Graceful failure recovery throughout
- **Settings Persistence**: JSON-based configuration storage
- **Modular Architecture**: Clean separation of concerns
- **Well-Documented Code**: Extensive comments and documentation

### 📋 Technical Details

#### Supported Asset Types
- Weapons (attachments, camos, variants)
- Models (xmodels, xanims)
- Images (materials, textures)
- Sounds (aliases, audio files)
- Effects (particles, beams)
- Scripts (GSC, CSC)
- And 40+ more file types

#### System Requirements
- Windows 10/11 (64-bit)
- .NET 8.0 Runtime
- ~50 MB disk space
- Black Ops 3 installation (for packaging)

#### Performance Metrics
- GDT Parsing: ~1,000 assets/second
- File Scanning: ~5,000 files/second
- Sound CSV Parsing: ~10,000 aliases/second
- Package Creation: Limited by disk I/O (~50-100 MB/s)

### 📦 Package Contents
- **Echo.exe**: Main application
- **EchoUpdater.exe**: Update installer
- **README.md**: Documentation
- **CHANGELOG.md**: This file

### 🔒 Security
- GitHub API integration with rate limit handling (5000 req/hour with token)
- Safe update process with backup creation
- No telemetry or data collection

### 🙏 Credits
- Built for the Black Ops 3 modding community
- Open source under MIT License

---

## [Unreleased]

### Planned Features
- Asset extraction and decompilation
- Batch processing support
- Custom asset validation rules
- Package installation manager
- Asset dependency visualization graph

