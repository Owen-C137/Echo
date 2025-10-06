# Echo - Quick Start Guide

## Running the Application

### Option 1: Using Visual Studio
1. Open `Echo.sln` in Visual Studio 2022
2. Press F5 or click "Start" to run the application

### Option 2: Using Command Line
```powershell
dotnet run --project Echo\Echo.csproj
```

### Option 3: Run the Executable
After building, you can run the executable directly:
```powershell
.\Echo\bin\Debug\net8.0-windows\Echo.exe
```

## First Time Setup

1. **Launch Echo** - The application will start with a dark-themed interface
2. **Open Settings** - Click "File" → "Settings" or click the "Open Settings" button
3. **Configure Black Ops III Path** - Click "Browse" and select your Black Ops III installation folder
4. **Save Settings** - Click "Save" to store your configuration

## Features Overview

### Main Window
- **Menu Bar**: Access all application features
- **Status Panel**: View current application status
- **Work Area**: Main content area for future AGDT operations
- **Status Bar**: Quick status information

### Settings Window
- **Black Ops III Path**: Configure the game installation directory
- Settings are automatically saved to `settings.json`

### Logging
- All operations are logged to `Logs.txt` in the application directory
- View logs via "Help" → "View Logs" menu
- Logs include timestamps and severity levels (Info, Warning, Error, Debug)

## Next Steps

The application is now ready for implementing AGDT parsing and packing functionality:

1. **Parser Implementation** - Add AGDT file parsing logic
2. **Packer Implementation** - Add AGDT file packing logic
3. **File Browser** - Add file selection and management
4. **Batch Operations** - Add support for processing multiple files

## Troubleshooting

### Application won't start
- Ensure .NET 8.0 runtime is installed
- Check `Logs.txt` for error messages

### Settings not saving
- Ensure the application has write permissions to its directory
- Check `Logs.txt` for file access errors

### Can't find Black Ops III path
- Look for `Call of Duty Black Ops III` in:
  - `C:\Program Files (x86)\Steam\steamapps\common\`
  - `C:\Program Files\Steam\steamapps\common\`
  - Your custom Steam library location

## Development

### Project Structure
```
Echo/
├── Services/          # Core services (Logger, Settings)
├── Views/             # XAML windows and UI
├── Styles/            # WPF styling resources
├── App.xaml          # Application entry point
└── Echo.csproj       # Project configuration
```

### Building
```powershell
dotnet build
```

### Cleaning Build
```powershell
dotnet clean
```

### Publishing (Release Build)
```powershell
dotnet publish -c Release
```

The published application will be in `Echo\bin\Release\net8.0-windows\publish\`
