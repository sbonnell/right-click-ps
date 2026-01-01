# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

RightClickPS is a Windows context menu extension that executes PowerShell scripts on selected files and folders. The menu structure is dynamically built from a scripts folder hierarchy, where folders become submenus and .ps1 files become menu items.

## Architecture

- **Framework**: .NET 8 Windows console application
- **Menu Implementation**: Cascading registry entries (HKCU\Software\Classes)
- **No external dependencies**: Uses only .NET BCL (System.Text.Json, Microsoft.Win32.Registry)

### Components

- **Program.cs**: CLI entry point routing to register/unregister/execute commands
- **ConfigLoader**: Reads config.json from app directory
- **ScriptDiscovery**: Recursively scans scripts folder, builds menu hierarchy
- **ScriptParser**: Extracts metadata from .ps1 block comment headers
- **ScriptExecutor**: Runs PowerShell with $SelectedFiles array, handles elevation
- **ContextMenuRegistry**: Creates/removes cascading registry menu entries

### Script Metadata Format

Scripts use block comment headers:
```powershell
<#
@Name: Convert to JPG
@Description: Converts images to JPEG format
@Extensions: .png,.bmp,.gif
@TargetType: Files
@RunAsAdmin: false
#>

# Script code here, $SelectedFiles contains array of selected file paths
```

### Configuration (config.json)

```json
{
  "menuName": "PowerShell Scripts",
  "scriptsPath": "C:\\Scripts",
  "systemScriptsPath": "./SystemScripts",
  "maxDepth": 3
}
```

## Build Commands

```bash
dotnet build src/RightClickPS/RightClickPS.csproj
dotnet publish src/RightClickPS/RightClickPS.csproj -c Release -r win-x64
```

## CLI Usage

```bash
RightClickPS.exe register      # Create context menu entries from scripts folder
RightClickPS.exe unregister    # Remove all context menu entries
RightClickPS.exe execute "<script>" "<file1>" "<file2>" ...  # Run a script
```

## Registry Paths

- Files: `HKCU\Software\Classes\*\shell\RightClickPS`
- Folders: `HKCU\Software\Classes\Directory\shell\RightClickPS`
