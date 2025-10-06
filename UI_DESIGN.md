# Echo UI Design Concept

## 🎨 Main Application Layout

### **Material Design Color Scheme**
- **Primary**: Deep Blue (#1976D2) - Professional, trustworthy
- **Accent**: Cyan (#00BCD4) - Tech-focused, modern
- **Dark Theme**: For reduced eye strain during long modding sessions
- **Status Colors**:
  - ✓ Success: Green (#4CAF50)
  - ⚠ Warning: Amber (#FFC107)
  - ✗ Error: Red (#F44336)
  - ℹ Info: Blue (#2196F3)

---

## 📱 Application Flow

```
┌─────────────────────────────────────────────────┐
│         ECHO - Main Window                      │
├─────────────────────────────────────────────────┤
│                                                 │
│  ┌──────────────────────────────────────────┐  │
│  │  TAB: Scan & Create  │  Extract  │  ...  │  │
│  └──────────────────────────────────────────┘  │
│                                                 │
│  Step 1: Select GDT(s)                          │
│  ┌──────────────────────────────────────────┐  │
│  │  📄 my_weapons.gdt                       │  │
│  │  📄 my_perks.gdt                         │  │
│  │  [+ Add GDT]  [- Remove]                 │  │
│  └──────────────────────────────────────────┘  │
│                                                 │
│  Step 2: Scan Assets                            │
│  [🔍 SCAN ASSETS] ────────────> Results Panel   │
│                                                 │
│  Step 3: Review & Package                       │
│  [📦 CREATE PACKAGE]                            │
│                                                 │
└─────────────────────────────────────────────────┘
```

---

## 🖼️ Detailed Screen Designs

### **Screen 1: GDT Selection & Scan**

**Purpose**: Let users select GDT files and initiate scanning

```
╔═══════════════════════════════════════════════════════╗
║  Echo                                    [─] [□] [×]  ║
╠═══════════════════════════════════════════════════════╣
║  File  Edit  Tools  Help                              ║
╠═══════════════════════════════════════════════════════╣
║                                                       ║
║  ┌─ Scan & Create ─┬─ Extract Package ─┬─ About ─┐  ║
║  │                                                  │  ║
║  │  📂 GDT FILES TO PACKAGE                        │  ║
║  │  ┌──────────────────────────────────────────┐  │  ║
║  │  │                                          │  │  ║
║  │  │  ┌─────────────────────────────────┐    │  │  ║
║  │  │  │ 📄 my_custom_weapons.gdt        │ ✕  │  │  ║
║  │  │  │ 📁 source_data\_owens\          │    │  │  ║
║  │  │  │ 📊 Not yet scanned              │    │  │  ║
║  │  │  └─────────────────────────────────┘    │  │  ║
║  │  │                                          │  │  ║
║  │  │  ┌─────────────────────────────────┐    │  │  ║
║  │  │  │ 📄 perk_machines.gdt            │ ✕  │  │  ║
║  │  │  │ 📁 source_data\_owens\          │    │  │  ║
║  │  │  │ 📊 Not yet scanned              │    │  │  ║
║  │  │  └─────────────────────────────────┘    │  │  ║
║  │  │                                          │  │  ║
║  │  │  [+ BROWSE FOR GDT FILES]               │  │  ║
║  │  │                                          │  │  ║
║  │  └──────────────────────────────────────────┘  │  ║
║  │                                                  │  ║
║  │  💡 Tip: You can select multiple GDT files to   │  ║
║  │     package them together!                      │  ║
║  │                                                  │  ║
║  │  ┌────────────────────────────────────────────┐ │  ║
║  │  │  [🔍 SCAN ASSETS]      [🗑️ CLEAR ALL]     │ │  ║
║  │  └────────────────────────────────────────────┘ │  ║
║  │                                                  │  ║
║  └──────────────────────────────────────────────────┘  ║
║                                                       ║
║  Status: Ready │ BO3 Path: D:\SteamLibrary\...        ║
╚═══════════════════════════════════════════════════════╝
```

---

### **Screen 2: Scan Results with Asset Tree**

**Purpose**: Show discovered assets in hierarchical view

```
╔═══════════════════════════════════════════════════════╗
║  Echo - Scan Results                     [─] [□] [×]  ║
╠═══════════════════════════════════════════════════════╣
║                                                       ║
║  ┌────────────────────────────┬────────────────────┐  ║
║  │  📊 SCAN SUMMARY           │  📝 ASSET DETAILS  │  ║
║  ├────────────────────────────┤                    │  ║
║  │                            │                    │  ║
║  │  ✓ 2 GDT Files             │  Selected:         │  ║
║  │  ✓ 23 Assets Found         │  i_dp28_c          │  ║
║  │    • 15 Images             │                    │  ║
║  │    • 8 Models              │  Type: Image       │  ║
║  │    • 12 Materials          │  Format: PNG       │  ║
║  │                            │                    │  ║
║  │  ⚠ 2 Missing Files         │  File Path:        │  ║
║  │  📦 Total Size: 45.2 MB    │  model_export\     │  ║
║  │                            │  _owens_mc\        │  ║
║  │  [📋 VIEW MISSING]         │  Weapons\dp28\     │  ║
║  │                            │  dp28_1.png        │  ║
║  ├────────────────────────────┤                    │  ║
║  │  🌲 ASSET TREE             │  Size: 2.4 MB      │  ║
║  │  ┌──────────────────────┐  │  Status: ✓ Found   │  ║
║  │  │▼ 📄 my_weapons.gdt   │  │                    │  ║
║  │  │  ▼ 🖼️ Images (15)    │  │  Dependencies:     │  ║
║  │  │    ✓ i_dp28_c       │◄─┼─ None              │  ║
║  │  │    ✓ i_aa12_c       │  │                    │  ║
║  │  │    ⚠ i_missing      │  │  Used By:          │  ║
║  │  │  ▼ 🎮 Models (8)    │  │  • m_dp28          │  ║
║  │  │    ✓ m_dp28_view    │  │                    │  ║
║  │  │    ✓ m_dp28_world   │  │  [🔍 LOCATE FILE]  │  ║
║  │  │  ▼ 🎨 Materials (12)│  │                    │  ║
║  │  │    ✓ mtl_dp28_base  │  │                    │  ║
║  │  │▼ 📄 perk_machines.gdt│  │                    │  ║
║  │  │  ▶ 🖼️ Images (8)    │  │                    │  ║
║  │  │  ▶ 🎮 Models (3)    │  │                    │  ║
║  │  └──────────────────────┘  │                    │  ║
║  │                            │                    │  ║
║  │  Filter: [_________] 🔍    │                    │  ║
║  │  ☐ Show Missing Only       │                    │  ║
║  │                            │                    │  ║
║  └────────────────────────────┴────────────────────┘  ║
║                                                       ║
║  [◀ BACK]  [📦 CREATE PACKAGE]                        ║
║                                                       ║
╚═══════════════════════════════════════════════════════╝
```

---

### **Screen 3: Package Creation**

**Purpose**: Configure package metadata and create the final package

```
╔═══════════════════════════════════════════════════════╗
║  Echo - Create Package                   [─] [□] [×]  ║
╠═══════════════════════════════════════════════════════╣
║                                                       ║
║  📦 PACKAGE INFORMATION                               ║
║  ┌───────────────────────────────────────────────┐   ║
║  │                                               │   ║
║  │  Package Name *                               │   ║
║  │  ┌─────────────────────────────────────────┐  │   ║
║  │  │ My Custom BO3 Weapons Pack              │  │   ║
║  │  └─────────────────────────────────────────┘  │   ║
║  │                                               │   ║
║  │  Author                                       │   ║
║  │  ┌─────────────────────────────────────────┐  │   ║
║  │  │ YourName                                │  │   ║
║  │  └─────────────────────────────────────────┘  │   ║
║  │                                               │   ║
║  │  Version                                      │   ║
║  │  ┌──────────┐                                │   ║
║  │  │ 1.0.0    │                                │   ║
║  │  └──────────┘                                │   ║
║  │                                               │   ║
║  │  Description                                  │   ║
║  │  ┌─────────────────────────────────────────┐  │   ║
║  │  │ Custom weapons pack including:          │  │   ║
║  │  │ - DP-28 LMG                             │  │   ║
║  │  │ - AA-12 Shotgun                         │  │   ║
║  │  │ - Custom textures and models            │  │   ║
║  │  │                                         │  │   ║
║  │  └─────────────────────────────────────────┘  │   ║
║  │                                               │   ║
║  └───────────────────────────────────────────────┘   ║
║                                                       ║
║  💾 OUTPUT SETTINGS                                   ║
║  ┌───────────────────────────────────────────────┐   ║
║  │                                               │   ║
║  │  Save Location                                │   ║
║  │  ┌─────────────────────────────────┐  [📁]   │   ║
║  │  │ C:\Users\...\MyWeaponsPack.echo │         │   ║
║  │  └─────────────────────────────────┘         │   ║
║  │                                               │   ║
║  │  Package Format                               │   ║
║  │  ● Echo Package (.echo) - Recommended         │   ║
║  │  ○ ZIP Archive (.zip) - Basic                 │   ║
║  │                                               │   ║
║  │  Options                                      │   ║
║  │  ✓ Include GDT files (2 files)                │   ║
║  │  ✓ Include all assets (23 files, 45.2 MB)    │   ║
║  │  ✓ Generate installation guide                │   ║
║  │  ✓ Compress files (estimated: 32.1 MB)        │   ║
║  │  ☐ Create backup of existing files            │   ║
║  │                                               │   ║
║  └───────────────────────────────────────────────┘   ║
║                                                       ║
║  [◀ BACK]            [📦 CREATE PACKAGE]              ║
║                                                       ║
╚═══════════════════════════════════════════════════════╝
```

---

### **Screen 4: Package Creation Progress**

**Purpose**: Show real-time progress during packaging

```
╔═══════════════════════════════════════════════════════╗
║  Echo - Creating Package...              [─] [□] [×]  ║
╠═══════════════════════════════════════════════════════╣
║                                                       ║
║  📦 PACKAGING IN PROGRESS                             ║
║  ┌───────────────────────────────────────────────┐   ║
║  │                                               │   ║
║  │  Creating: MyWeaponsPack.echo                 │   ║
║  │                                               │   ║
║  │  Current Step:                                │   ║
║  │  Copying model files...                       │   ║
║  │                                               │   ║
║  │  Overall Progress                             │   ║
║  │  ████████████████░░░░░░░░░░  65%             │   ║
║  │                                               │   ║
║  │  ┌─────────────────────────────────────────┐  │   ║
║  │  │ ✓ Validating GDT files                  │  │   ║
║  │  │ ✓ Creating package structure            │  │   ║
║  │  │ ✓ Copying GDT files (2/2)               │  │   ║
║  │  │ ⏳ Copying images (10/15)                │  │   ║
║  │  │   ├─ i_dp28_c.png                       │  │   ║
║  │  │   └─ Copying... (2.4 MB)                │  │   ║
║  │  │ ⏸ Copying models (0/8)                   │  │   ║
║  │  │ ⏸ Creating manifest                      │  │   ║
║  │  │ ⏸ Compressing package                    │  │   ║
║  │  └─────────────────────────────────────────┘  │   ║
║  │                                               │   ║
║  │  Files: 12 / 23  │  Size: 28.5 / 45.2 MB     │   ║
║  │  Estimated time remaining: 15 seconds        │   ║
║  │                                               │   ║
║  └───────────────────────────────────────────────┘   ║
║                                                       ║
║  [⏸ PAUSE]  [✕ CANCEL]                               ║
║                                                       ║
╚═══════════════════════════════════════════════════════╝
```

---

### **Screen 5: Package Extraction/Installation**

**Purpose**: Install a package to BO3 directory

```
╔═══════════════════════════════════════════════════════╗
║  Echo - Install Package                  [─] [□] [×]  ║
╠═══════════════════════════════════════════════════════╣
║                                                       ║
║  ┌─ Scan & Create ─┬─ Extract Package ─┬─ About ─┐  ║
║  │                                                  │  ║
║  │  📥 SELECT PACKAGE TO INSTALL                   │  ║
║  │  ┌──────────────────────────────────────────┐  │  ║
║  │  │ C:\Downloads\MyWeaponsPack.echo  │  [📁] │  │  ║
║  │  └──────────────────────────────────────────┘  │  ║
║  │                                                  │  ║
║  │  📋 PACKAGE INFORMATION                         │  ║
║  │  ┌──────────────────────────────────────────┐  │  ║
║  │  │  Name: My Custom BO3 Weapons Pack        │  │  ║
║  │  │  Author: YourName                        │  │  ║
║  │  │  Version: 1.0.0                          │  │  ║
║  │  │  Created: Oct 5, 2025                    │  │  ║
║  │  │  Size: 32.1 MB (23 files)                │  │  ║
║  │  │                                          │  │  ║
║  │  │  Description:                            │  │  ║
║  │  │  Custom weapons pack including:          │  │  ║
║  │  │  - DP-28 LMG                             │  │  ║
║  │  │  - AA-12 Shotgun                         │  │  ║
║  │  │  - Custom textures and models            │  │  ║
║  │  │                                          │  │  ║
║  │  │  Contents:                               │  │  ║
║  │  │  • 2 GDT files                           │  │  ║
║  │  │  • 15 Image files                        │  │  ║
║  │  │  • 8 Model files                         │  │  ║
║  │  └──────────────────────────────────────────┘  │  ║
║  │                                                  │  ║
║  │  🎯 INSTALLATION OPTIONS                        │  ║
║  │  ┌──────────────────────────────────────────┐  │  ║
║  │  │  Install To:                             │  │  ║
║  │  │  ✓ D:\SteamLibrary\...\Black Ops III    │  │  ║
║  │  │                                          │  │  ║
║  │  │  ☐ Create backup before installing       │  │  ║
║  │  │  ✓ Verify file integrity                 │  │  ║
║  │  │  ✓ Extract to correct folder structure   │  │  ║
║  │  │                                          │  │  ║
║  │  │  ⚠ Conflicts Detected:                   │  │  ║
║  │  │    • i_dp28_c.png already exists         │  │  ║
║  │  │      Action: ● Overwrite  ○ Skip         │  │  ║
║  │  └──────────────────────────────────────────┘  │  ║
║  │                                                  │  ║
║  │  ┌────────────────────────────────────────────┐ │  ║
║  │  │         [📥 INSTALL PACKAGE]               │ │  ║
║  │  └────────────────────────────────────────────┘ │  ║
║  │                                                  │  ║
║  └──────────────────────────────────────────────────┘  ║
║                                                       ║
╚═══════════════════════════════════════════════════════╝
```

---

## 🎨 UI Component Details

### **Cards (Material Design)**
- Use elevated cards for major sections
- Subtle shadows for depth
- Rounded corners (4px radius)
- Proper padding (16-24px)

### **Icons**
- Material Design Icons (PackIcon)
- Consistent sizing
- Color-coded by type:
  - 📄 GDT = Blue
  - 🖼️ Images = Purple
  - 🎮 Models = Green
  - 🎨 Materials = Orange
  - ⚠ Warnings = Amber
  - ✓ Success = Green
  - ✕ Error = Red

### **Buttons**
- Raised buttons for primary actions
- Outlined buttons for secondary actions
- Icon + text for clarity
- Proper hover/click states

### **Progress Indicators**
- Linear progress bars for overall progress
- Circular progress for small tasks
- Real-time updates
- ETA calculations

### **Tree View**
- Expandable/collapsible nodes
- Icons for each asset type
- Status indicators
- Search/filter capability
- Context menu support

---

## 📊 Data Visualization

### **Asset Statistics Card**
```
┌────────────────────────┐
│ 📊 Package Statistics  │
├────────────────────────┤
│                        │
│  Total Assets: 23      │
│  ┌──────────────────┐  │
│  │ Images    ████ 15│  │
│  │ Models    ███ 8  │  │
│  │ Materials ████ 12│  │
│  └──────────────────┘  │
│                        │
│  Total Size: 45.2 MB   │
│  Missing: 2 files      │
│                        │
└────────────────────────┘
```

This design provides a **clean, professional, and intuitive** interface that guides users through the packaging process step-by-step!
