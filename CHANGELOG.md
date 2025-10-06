# Changelog

All notable changes to Echo - Black Ops 3 GDT Parser & Packer will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.2] - 2025-10-06

### 🐛 Bug Fixes

- **Version Display Fix**: Version number now correctly displays in launcher window
  - Switched from manual AssemblyInfo.cs to auto-generated assembly attributes
  - Version is now read from Echo.csproj (single source of truth)
  - Eliminated duplicate attribute errors during compilation

- **Update Detection Fix**: Update checker no longer incorrectly reports current version as available update
  - Assembly version now properly set to match release version
  - Version comparison logic working correctly

### 🔧 Technical Improvements

- **Simplified Version Management**: Version only needs to be changed in one place (Echo.csproj)
  - Removed manual Properties/AssemblyInfo.cs file
  - Enabled auto-generation of assembly attributes from project properties
  - Version automatically propagates to all assembly metadata

## [1.0.1] - 2025-10-06

### 🎨 UI/UX Enhancements

**This release focuses on visual polish and user experience improvements with comprehensive theming and tooltip additions.**

#### New Features
- **4 Complete Themes**: Choose from multiple professionally designed themes
  - 🌙 **Dark Theme**: Classic dark mode with blue accents (default)
  - ☀️ **Light Theme**: Bright and clean with blue accents
  - 🔥 **DevRaw**: Sleek tactical theme with orange accents and deep black
  - 💜 **Midnight Purple**: Elegant modern theme with vivid purple and mint green
  
- **Theme Selector**: Easy theme switching from Settings window
  - Instant theme preview
  - Auto-restart prompt when theme changes
  - Persistent theme preference
  - Error handling for missing/corrupted themes

- **Comprehensive Tooltips**: 29 helpful tooltips across all windows
  - File management buttons with clear descriptions
  - Settings controls explaining each option
  - Navigation buttons with action previews
  - Themed tooltip styling matching each theme

#### Improvements
- **Consistent Theme Coverage**: All UI elements properly themed
  - Buttons (standard, tool, primary)
  - ComboBox dropdowns (overrides system defaults)
  - DataGrid headers and rows
  - ListBox items and selection
  - Borders, backgrounds, and surfaces
  - Text colors and subtle text
  
- **Enhanced Accessibility**: Better user guidance
  - Tooltips explain complex features
  - Clear action descriptions
  - Visual feedback for all interactions
  - Readable text contrast in all themes

#### Technical Changes
- Added ToolTip styles to all theme files
- Created DarkTheme.xaml, LightTheme.xaml, DevRawTheme.xaml, MidnightPurpleTheme.xaml
- Implemented AssemblyResolve event handler to fix WPF pack:// URI loading in published builds
- Configured single-file self-contained deployment for simplified distribution
- Added comprehensive global exception handlers with crash logging
- Removed debug symbols (PDB files) from release builds
- Enabled compression for single-file deployments

### 🐛 Bug Fixes
- **CRITICAL**: Fixed WPF assembly loading issue in Release builds
  - Resolved "Could not load file or assembly 'Echo'" error in published applications
  - Added custom AssemblyResolve handler in App.xaml.cs
  - WPF pack:// URIs now work correctly in single-file deployments
  
- **Build Configuration**: Optimized for deployment
  - Single-file executables (Echo.exe, EchoUpdater.exe only)
  - Self-contained builds include .NET runtime
  - No external dependencies required
  - Clean release packages without JSON/DLL files
- Refactored settings to use Theme property (string) instead of UseDarkMode (bool)
- Added theme fallback handling in App.xaml.cs
- Fixed Midnight Purple SubtleBrush for better text contrast (#4A3A66)
- Implemented AssemblyResolve handler to fix WPF pack:// URI loading in published builds
- Configured single-file self-contained deployment for cleaner releases
- Added global exception handlers for better error reporting and crash diagnostics
- Set DebugType to 'none' to remove .pdb files from release builds

#### Bug Fixes
- **CRITICAL**: Fixed WPF assembly loading crash in published Release builds
  - Added custom AssemblyResolve event handler in App.xaml.cs
  - Resolves "Could not load file or assembly 'Echo'" error
  - Fixes pack:// URI resource loading in .NET 8 single-file deployments
- Fixed white backgrounds in themed windows
- Fixed navigation button styles not matching theme
- Fixed ComboBox dropdowns showing light blue system colors
- Fixed DataGrid headers missing from original themes
- Fixed button alignment and padding issues
- Fixed hardcoded colors replaced with DynamicResource
- Fixed release builds now properly self-contained (no external DLLs required)

#### Build & Deployment
- Release builds now generate single-file executables only
- Echo.exe: 68.69 MB (self-contained, includes .NET runtime)
- EchoUpdater.exe: 33.51 MB (self-contained)
- No additional DLL or JSON files required for distribution
- Enabled compression in single-file builds for smaller size

---

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

