# StubbornKnight - Hollow Knight Mod

**English Version** | [**中文说明**](README.md)

## 🎮 Game Mechanics

### Core Gameplay
StubbornKnight is a **stubborn knight mod**. The Knight's mind is always fixated on one direction; you can only attack in the direction it desires.

### Arrow System
- **4 vertically arranged arrows** (Up/Down/Left/Right) are displayed above the player's head
- **The bottommost arrow** is the current direction the Knight desires
- After successfully performing an action, the arrow queue scrolls down, generating a new direction

### Action Rules

| Action Type | Input Method | Required Direction |
|-------------|--------------|-------------------|
| **Normal Attack** | Attack button | Left/Right (based on character facing) |
| **Great Slash** | Attack + Hold | Left/Right (based on character facing) |
| **Dash Slash** | Attack + Down + Hold | Down ⬇️ |
| **Cyclone Slash** | Attack + Up + Hold | Up ⬆️ |
| **Vengeful Spirit/Shade Soul** | Spell button | Left/Right (based on character facing) |
| **Howling Wraiths** | Spell button + Up | Up ⬆️ |
| **Abyss Shriek** | Spell button + Down | Down ⬇️ |

### Failure Penalty
- When the action direction **does not match** the desired direction, the action will be **intercepted** (no attack hitbox/no mana consumed)
- The arrows will not scroll; you need to adjust your direction and try again

### Success
- After the direction matches, the action is released normally
- The arrow queue scrolls, advancing to the next direction

## ⚙️ Settings

The following settings can be adjusted in the Mod Options menu from the main menu:

| Setting | Description | Range |
|---------|-------------|-------|
| **StubbornKnight** | Master switch for the mod | On / Off |
| **Arrow Queue Length** | Number of arrows displayed above the player's head | 1 - 30 |
| **Arrow Opacity** | Transparency of the arrow display | 0.1 - 1.0 |
| **Sound Volume** | Volume of the success sound effect when direction matches | 0 - 10 |

When the mod is disabled, all actions are unrestricted and the game behaves as vanilla.

## 📋 Version
- Current Version: 1.0
- Supported Game Version: 1.5.78 (Hollow Knight)
