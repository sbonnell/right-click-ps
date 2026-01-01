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
RightClickPS.exe register                                    # Create context menu entries from scripts folder
RightClickPS.exe unregister                                  # Remove all context menu entries
RightClickPS.exe execute "<script>" "<file1>" "<file2>" ...  # Run a script with selected files
RightClickPS.exe help                                        # Show usage information
```

## Registry Paths

- Files: `HKCU\Software\Classes\*\shell\RightClickPS`
- Folders: `HKCU\Software\Classes\Directory\shell\RightClickPS`

## Agent-Based Implementation Workflow

Each task in `implementation-plan.json` is completed by an agent following this workflow:

### Agent Input
- This file (`CLAUDE.md`)
- `implementation-plan.json` (full plan with task details)
- Assigned task ID to work on

### Pre-Implementation Verification
Before coding, agents MUST verify all prerequisite tasks are complete:

1. **Check JSON flags**: Read `implementation-plan.json`, verify all tasks listed in `dependencies` array have `"completed": true`
2. **Check files exist**: Verify files listed in prerequisite tasks exist in the codebase
3. **Verify build**: Run `dotnet build` to confirm existing code compiles

If any check fails, agent should STOP and report which prerequisites are incomplete.

### Git Workflow
```bash
# 1. Create task branch from main
git checkout main
git pull
git checkout -b feature/task-{id}-{short-description}

# 2. Implement the task

# 3. Run tests
dotnet test

# 4. Commit with informative message
git add .
git commit -m "Task {id}: {title}

- {bullet points describing implementation details}
- {any design decisions made}
- {files created/modified}

Completes: Task {id}
Dependencies: Tasks {list of dependency IDs}"

# 5. Merge to main
git checkout main
git merge feature/task-{id}-{short-description}
git branch -d feature/task-{id}-{short-description}
```

### Testing Requirements
- **Framework**: xUnit
- **Test project**: `tests/RightClickPS.Tests/RightClickPS.Tests.csproj`
- Each task must include unit tests for the code created
- All tests (existing + new) must pass before merge
- Tests become part of verification for dependent tasks

### Post-Implementation
1. Update `implementation-plan.json`: Set `"completed": true` and `"tested": true` for the task
2. Run full test suite: `dotnet test`
3. Merge branch to main
4. Subsequent agents will use commit messages and tests to understand completed work

---

## CRITICAL: Implementation Plan Integrity Rules

**AGENTS MUST NOT MODIFY THE IMPLEMENTATION PLAN EXCEPT FOR STATUS FLAGS.**

The `implementation-plan.json` file is READ-ONLY except for two fields per task:
- `"completed": true/false`
- `"tested": true/false`

### PROHIBITED Actions (Will Corrupt the Plan)
- Adding new tasks
- Removing existing tasks
- Changing task IDs
- Modifying task titles
- Altering task descriptions
- Changing file lists
- Modifying dependencies
- Reordering tasks
- Changing phase names
- Altering any other field

### PERMITTED Actions (Only These)
```json
// BEFORE agent work:
{ "id": 5, ..., "completed": false, "tested": false }

// AFTER agent completes task 5:
{ "id": 5, ..., "completed": true, "tested": true }
```

### Why This Matters
- The plan was designed holistically with inter-task dependencies
- Changing one task can break assumptions in dependent tasks
- Other agents rely on the plan being stable and predictable
- Task IDs are used in git branches and commit messages

**If an agent believes a task is incorrect or incomplete, it must STOP and report the issue rather than modify the plan.**

### Test Commands
```bash
dotnet test                                    # Run all tests
dotnet test --filter "FullyQualifiedName~TaskN"  # Run specific test class
```
