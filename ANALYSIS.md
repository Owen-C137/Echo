# Echo - Black Ops III GDT Asset Packaging System Analysis

## 📋 Understanding the Black Ops III Asset Structure

### **What is a GDT?**
A GDT (Game Data Tool) file is a text-based database that defines game assets in Black Ops III. It contains entries for:
- **Images/Textures** - References to texture files (.tif, .png, .tiff)
- **XModels** - 3D models (.xmodel_bin)
- **Materials** - Shader definitions and material properties
- **AI Types** - Character definitions
- **Animations** - References to animation files

### **Current Asset Structure**
```
Call of Duty Black Ops III/
├── source_data/              # GDT files (main location)
│   ├── *.gdt                 # Various GDT files
│   └── _custom/              # User custom folders
│
├── model_export/             # 3D Models & Source Files
│   ├── _owens_mc/            # Custom models folder
│   │   ├── *.xmodel_bin      # Compiled models (817 files)
│   │   ├── textures/         # Texture files
│   │   │   ├── *.png         # PNG textures (1779 files)
│   │   │   ├── *.tif         # TIF textures (557 files)
│   │   │   └── *.tiff        # TIFF textures (13 files)
│   │   └── *.ma/mb           # Maya source files
│
├── xanim_export/             # Animation files
│   └── _custom/              # Custom animations
│
├── sound/                    # Sound assets
├── texture_assets/           # Additional textures
└── zone_source/              # Zone/map compilation files
```

### **Key Observations from GDT Analysis**

1. **Asset References Are Path-Based**
   - Images: `"baseImage" "model_export\\_owens_mc\\Weapons\\dp28\\dp28_1.png"`
   - Models: `"filename" "_minecraft\\blocks\\_mc_block_brewing_stand.xmodel_bin"`
   - Paths are relative to the BO3 root directory

2. **Asset Types in GDTs**
   - `image.gdf` - Texture definitions
   - `xmodel.gdf` - 3D model definitions
   - `material.gdf` - Material/shader definitions
   - `aitype.gdf` - AI character types
   - Each has extensive properties and settings

3. **Dependencies Are Deep**
   - A material references multiple images (diffuse, normal, specular, etc.)
   - Models reference materials
   - Materials reference textures
   - Everything is interconnected!

---

## 🎯 The Problem We're Solving

**Modders want to share custom assets**, but currently they need to:
1. Manually find all referenced files in the GDT
2. Track down textures in model_export folders
3. Find associated animations, sounds, etc.
4. Package everything correctly
5. Explain to others where to extract files

**This is tedious, error-prone, and time-consuming!**

---

## 💡 Proposed Solution: Echo Asset Packager

### **Core Functionality**

#### **1. GDT Asset Analyzer**
- Parse GDT files to extract all asset definitions
- Build a dependency tree showing:
  - What assets are defined
  - What files each asset references
  - What other assets they depend on

#### **2. Intelligent File Collector**
- Scan referenced paths to locate actual files
- Support multiple search locations:
  - `model_export/`
  - `xanim_export/`
  - `sound/`
  - `texture_assets/`
  - Custom user folders
- Detect missing files and warn user

#### **3. Package Creator**
- Create a shareable package containing:
  - The GDT file(s)
  - All referenced asset files
  - A manifest describing the package
  - Installation instructions
- Support multiple package formats:
  - ZIP (simple)
  - Custom .echo format (with metadata)

#### **4. Package Extractor/Installer**
- Read Echo packages
- Extract files to correct BO3 directory structure
- Verify file integrity
- Report installation success/issues

---

## 🎨 Proposed UI Design Concept

### **Main Window - 3 Tab Layout**

#### **Tab 1: Scan & Analyze**
```
┌─────────────────────────────────────────────────────┐
│  📂 Select GDT File(s)                              │
│  ┌──────────────────────────────────────────────┐  │
│  │ C:\...\source_data\_owens\my_custom.gdt   [📁]│  │
│  └──────────────────────────────────────────────┘  │
│  [+ Add More GDTs]  [🗑️ Clear All]                  │
│                                                     │
│  📊 Analysis Results                                │
│  ┌──────────────────────────────────────────────┐  │
│  │ ✓ 15 Images found                            │  │
│  │ ✓ 8 Models found                             │  │
│  │ ✓ 12 Materials found                         │  │
│  │ ⚠ 2 Files missing                            │  │
│  │                                              │  │
│  │ Total Package Size: 45.2 MB                  │  │
│  └──────────────────────────────────────────────┘  │
│                                                     │
│  [🔍 SCAN GDT]  [📦 CREATE PACKAGE]                │
└─────────────────────────────────────────────────────┘
```

#### **Tab 2: Asset Tree View**
```
┌─────────────────────────────────────────────────────┐
│  📂 Asset Hierarchy                                 │
│  ┌──────────────────────────────────────────────┐  │
│  │ 📄 my_custom.gdt                             │  │
│  │  ├─ 🖼️ Images (15)                            │  │
│  │  │   ├─ ✓ i_dp28_c                          │  │
│  │  │   │    └─ 📁 model_export/.../dp28_1.png │  │
│  │  │   ├─ ✓ i_mc_warp_plate_c                 │  │
│  │  │   └─ ⚠ i_missing_texture (NOT FOUND)     │  │
│  │  │                                           │  │
│  │  ├─ 🎮 Models (8)                            │  │
│  │  │   ├─ ✓ _mc_block_brewing_stand_base      │  │
│  │  │   │    └─ 📁 .../_mc_block_brewing_stand. │  │
│  │  │   └─ ...                                 │  │
│  │  │                                           │  │
│  │  └─ 🎨 Materials (12)                        │  │
│  │       ├─ ✓ anvil2a                          │  │
│  │       └─ ...                                │  │
│  └──────────────────────────────────────────────┘  │
│                                                     │
│  📝 Selected Asset Details                          │
│  ┌──────────────────────────────────────────────┐  │
│  │ Name: i_dp28_c                               │  │
│  │ Type: Image (Texture)                        │  │
│  │ File: model_export/.../dp28_1.png            │  │
│  │ Size: 2.4 MB                                 │  │
│  │ Status: ✓ Found                              │  │
│  └──────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
```

#### **Tab 3: Create Package**
```
┌─────────────────────────────────────────────────────┐
│  📦 Package Settings                                │
│  ┌──────────────────────────────────────────────┐  │
│  │ Package Name:                                │  │
│  │ [My Awesome Mod Pack           ]             │  │
│  │                                              │  │
│  │ Author:                                      │  │
│  │ [YourName                      ]             │  │
│  │                                              │  │
│  │ Description:                                 │  │
│  │ ┌──────────────────────────────────────┐    │  │
│  │ │ Custom weapons and perk machines     │    │  │
│  │ │ for zombies mode                     │    │  │
│  │ └──────────────────────────────────────┘    │  │
│  │                                              │  │
│  │ Output Location:                             │  │
│  │ [C:\Users\...\Desktop\MyPack.echo    ] [📁] │  │
│  │                                              │  │
│  │ Package Format:                              │  │
│  │ ○ ZIP Archive (.zip)                         │  │
│  │ ● Echo Package (.echo) [Recommended]         │  │
│  │                                              │  │
│  │ ✓ Include GDT file(s)                        │  │
│  │ ✓ Include all referenced assets              │  │
│  │ ✓ Generate installation instructions         │  │
│  │ ✓ Create manifest file                       │  │
│  └──────────────────────────────────────────────┘  │
│                                                     │
│  [◀ BACK]  [📦 CREATE PACKAGE]                     │
└─────────────────────────────────────────────────────┘
```

#### **Tab 4: Extract/Install Package**
```
┌─────────────────────────────────────────────────────┐
│  📥 Install Package                                 │
│  ┌──────────────────────────────────────────────┐  │
│  │ Select Package File:                         │  │
│  │ [C:\Downloads\MyPack.echo          ] [📁]    │  │
│  └──────────────────────────────────────────────┘  │
│                                                     │
│  📋 Package Information                             │
│  ┌──────────────────────────────────────────────┐  │
│  │ Name: My Awesome Mod Pack                    │  │
│  │ Author: YourName                             │  │
│  │ Assets: 35 files (45.2 MB)                   │  │
│  │ Created: Oct 5, 2025                         │  │
│  │                                              │  │
│  │ Description:                                 │  │
│  │ Custom weapons and perk machines for         │  │
│  │ zombies mode                                 │  │
│  └──────────────────────────────────────────────┘  │
│                                                     │
│  📂 Install Location:                               │
│  [✓] Black Ops III Path: D:\SteamLibrary\...       │
│                                                     │
│  Installation Options:                              │
│  ✓ Extract to correct folders                      │  │
│  ✓ Verify file integrity                           │  │
│  ✓ Backup existing files (if any)                  │  │
│                                                     │
│  [📥 INSTALL PACKAGE]                               │
└─────────────────────────────────────────────────────┘
```

---

## 🔧 Technical Implementation Plan

### **Phase 1: GDT Parser**
- Create GDT file parser to read asset definitions
- Extract asset types, names, and file references
- Build dependency graph

### **Phase 2: File Scanner**
- Implement recursive file search in BO3 directories
- Create asset-to-file mapping
- Detect missing files

### **Phase 3: Package System**
- Define .echo package format (JSON manifest + ZIP)
- Implement package creation
- Implement package extraction

### **Phase 4: UI Implementation**
- Build Material Design UI with tabs
- Tree view for asset hierarchy
- Progress bars for scanning/packaging
- Error/warning display system

### **Phase 5: Advanced Features**
- Batch processing multiple GDTs
- Conflict detection
- Dependency analyzer
- Package validation

---

## 📦 Echo Package Format Proposal

### **.echo File Structure**
```
MyPack.echo (ZIP container)
├── manifest.json              # Package metadata
├── gdts/
│   └── my_custom.gdt          # GDT file(s)
├── model_export/              # Assets with preserved paths
│   └── _owens_mc/
│       └── Weapons/
│           └── dp28_1.png
└── README.txt                 # Installation instructions
```

### **manifest.json Structure**
```json
{
  "packageName": "My Awesome Mod Pack",
  "version": "1.0.0",
  "author": "YourName",
  "description": "Custom weapons and perk machines",
  "created": "2025-10-05T12:00:00Z",
  "echoVersion": "1.0.0",
  "assets": {
    "gdts": ["gdts/my_custom.gdt"],
    "totalFiles": 35,
    "totalSize": 47370240,
    "assetTypes": {
      "images": 15,
      "models": 8,
      "materials": 12
    }
  },
  "fileMapping": [
    {
      "packagePath": "model_export/_owens_mc/Weapons/dp28_1.png",
      "installPath": "model_export/_owens_mc/Weapons/dp28_1.png",
      "size": 2516582,
      "hash": "sha256:abc123..."
    }
  ]
}
```

---

## 🎯 Key Benefits

1. **One-Click Packaging** - No more manual file hunting
2. **Dependency Tracking** - Never miss a texture or model
3. **Easy Sharing** - Single file contains everything
4. **Foolproof Installation** - Automatic extraction to correct locations
5. **Integrity Verification** - Checksums ensure files aren't corrupted
6. **Professional Workflow** - Like a package manager for BO3 mods

---

## 🚀 Next Steps

1. **Review & Approve Design** - Get your feedback on this proposal
2. **Create UI Mockup** - Build visual prototype in WPF
3. **Implement GDT Parser** - Start with the core parsing logic
4. **Build File Scanner** - Add asset discovery functionality
5. **Package System** - Create .echo format handler
6. **Testing** - Test with real GDT files from your collection

---

## 💭 Questions for Discussion

1. Should we support other asset types (sounds, animations)?
2. Do you want version control features (track package updates)?
3. Should we add a "browse packages" feature with thumbnails?
4. Do you want integration with mod sharing platforms?
5. Should we support incremental updates (delta packages)?
