# SDD Spec: Arrow Mod Toggle Bug Fix

## 0. 🚨 Open Questions
- [x] 无（问题清晰）

## 1. Requirements (Context)
- **Goal**: 修复 mod 关闭后箭头仍然显示的问题
- **In-Scope**: 
  - `ArrowGame` 组件的启用/禁用逻辑
  - 箭头容器的可见性控制
- **Out-of-Scope**: 
  - 其他 mod 功能
  - 箭头滚动逻辑

## 1.5 Code Map
- **Core Logic**:
  - `StubbornKnight.cs`: 
    - `HeroController_Start` (line 427): 添加 ArrowGame 组件
    - `HeroController_Attack` (line 437): 攻击拦截
    - `HeroController_CanNailArt` (line 479): 剑技拦截
    - `PlayMakerFSM_OnEnable` (line 535): 法术 FSM 修改
- **Data Models**:
  - `Settings.on` (line 396): mod 开关配置
  - `ArrowGame` 组件：箭头显示和拦截逻辑
- **Dependencies**:
  - `HeroController`: 玩家控制
  - `ArrowGame._container`: 箭头容器 GameObject

## 2. Architecture (Strategy)
**问题分析**:
- 当 `mySettings.on` 从 `true` 切换到 `false` 时，`ArrowGame` 组件仍存在于 GameObject 上
- `ArrowGame.Start()` 创建的 `_container` 会持续显示
- 攻击/法术拦截会检查 `mySettings.on`，但箭头渲染不受控制

**解决方案**:
在 `ArrowGame` 中添加 `_enabled` 状态，当 mod 关闭时：
1. 隐藏箭头容器 (`_container.SetActive(false)`)
2. 在 `LateUpdate` 中停止更新

**修改位置**:
- `ArrowGame` 类：添加启用/禁用方法
- `StubbornKnight` 类：在相关 hook 中调用启用/禁用方法

## 3. Detailed Design & Implementation

### 3.1 Data Structures & Interfaces
- `File: StubbornKnight.cs`
    - `ArrowGame` 类新增方法:
        - `public void SetModEnabled(bool enabled)`: 控制箭头容器显示
    - `StubbornKnight` 类修改:
        - `HeroController_Start`: 无论开关都添加组件，但根据开关启用/禁用
        - 新增 `ToggleMod(bool enabled)` 方法：处理开关切换

### 3.2 Implementation Checklist
- [x] 1. `ArrowGame` 添加 `_isEnabled` 字段 (默认 `true`)
- [x] 2. `ArrowGame` 添加 `SetModEnabled(bool enabled)` 方法
- [x] 3. `StubbornKnight.HeroController_Start` 修改为始终添加组件
- [x] 4. `StubbornKnight` 添加 `ToggleModSetting(bool enabled)` 方法
- [x] 5. `StubbornKnight.Saver` 调用 `ToggleModSetting`

---

## 4. Additional Fixes (Discovered During Testing)

### 4.1 Spell Intercept Ignores Toggle
**Problem**: Spell intercept only depended on initial mod state, not current toggle.

**Root Cause**: 
- `SpellInterceptAction.OnEnter()` did not check `mySettings.on`
- FSM was only modified when mod was ON at game start

**Solution**:
- Add `StubbornKnight.IsModEnabled` static property for external access
- Check `IsModEnabled` at start of `SpellInterceptAction.OnEnter()`
- Always inject FSM regardless of mod state (intercept logic handles toggle)

**Checklist**:
- [x] Add `public static bool IsModEnabled` property
- [x] Check `IsModEnabled` in `SpellInterceptAction.OnEnter()`
- [x] Remove `mySettings.on` check from `PlayMakerFSM_OnEnable`

### 4.2 FSM Reset on SL/Re-enter Game
**Problem**: After save/load or re-entering game, FSM is recreated but not re-injected.

**Root Cause**: 
- `_fsmModified` static flag prevented re-injection

**Solution**:
- Remove `_fsmModified` flag
- Check if already injected in `InjectSpellAction()` (idempotent)

**Checklist**:
- [x] Remove `_fsmModified` field
- [x] Always call `ModifySpellControlFSM()` when Spell Control FSM detected
- [x] Add duplicate check in `InjectSpellAction()`
