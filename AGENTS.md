# AGENTS.md - Coding Guidelines for StubbornKnight

## Project Overview
Hollow Knight rhythm game mod (C# / .NET Framework 4.7.2 / Unity)

## Build Commands

```bash
# Build the project
dotnet build

# Build in Release mode
dotnet build -c Release

# Clean build artifacts
dotnet clean

# Restore NuGet packages
dotnet restore
```

## Project Structure

- `StubbornKnight.cs` - Main mod code with ArrowGame component
- `StubbornKnight.csproj` - Project file with Hollow Knight references
- `assets/` - Embedded resources (sprites, etc.)
- `mydocs/specs/` - Design specifications

## Code Style Guidelines

### Naming Conventions
- **Classes/Structs/Enums**: PascalCase (e.g., `ArrowGame`, `ArrowDirection`)
- **Public members**: PascalCase (e.g., `GetVersion()`, `Initialize()`)
- **Private fields**: `_camelCase` with underscore prefix (e.g., `_arrowRenderers`, `_isAnimating`)
- **Constants**: PascalCase (e.g., `ArrowCount`, `AnimationDuration`)
- **Namespaces**: PascalCase matching folder structure

### Formatting
- 4 spaces for indentation (no tabs)
- Opening braces on same line
- Single blank line between methods
- Maximum line length: 120 characters

### Types
- Use explicit types instead of `var`
- Target framework: .NET Framework 4.7.2 (`net472`)
- Language version: `latest` (C# 11 features available)

### Imports
Order imports by:
1. System.* namespaces
2. Third-party libraries (UnityEngine, HutongGames.PlayMaker, etc.)
3. Modding libraries (Modding, Satchel)
4. Project namespace

Example:
```csharp
using System;
using System.Collections;
using UnityEngine;
using Modding;
using Satchel;
using StubbornKnight;
```

### Mod Structure
- Main mod class inherits from `Mod` and implements `IGlobalSettings<T>`, `IMenuMod`
- Use `On.*` hooks for game event interception
- Component classes inherit from `MonoBehaviour`
- Use `[Serializable]` for settings classes

### Error Handling
- Wrap resource loading in try-catch blocks
- Use `Log()` method for debug output
- Fail gracefully - don't crash the game

### Logging
```csharp
// Use mod instance logger
StubbornKnight.instance.Log($"[ComponentName] Message");
```

### Game Integration
- Use `HeroController.instance` for player access
- Use `InputHandler.Instance.inputActions` for input
- Preload objects via `GetPreloadNames()`
- Apply FSM modifications in `PlayMakerFSM_OnEnable`

### Asset Management
- Embed resources using `<EmbeddedResource>` in .csproj
- Load via `Assembly.GetManifestResourceStream()`
- Resource path format: `{Namespace}.assets.{filename}`

### Settings Pattern
```csharp
[Serializable]
public class Settings
{
    public bool on = true;
}
```

## Important Notes

- Mod auto-installs to Hollow Knight's Mods folder on build
- Game directory configured via `GameDir` property in .csproj
- Uses MMHOOK for method hooking
- Comments can be in Chinese (maintain consistency with existing code)
- Unity components use `[SerializeField]` for inspector visibility
