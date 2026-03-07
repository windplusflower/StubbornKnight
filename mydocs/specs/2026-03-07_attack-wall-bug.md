# SDD Spec: 错误方向攻击时墙壁特效和后坐力 Bug

## 0. 🚨 Open Questions (MUST BE CLEAR BEFORE CODING)
- [x] 问题已确认：代码正确阻止了原始 Attack 调用，但墙特效和后坐力仍触发

## 1. Requirements (Context)
- **Goal**: 修复 bug - 当攻击方向错误时，虽然正确阻止了攻击（无挥动画），但墙上仍有击中特效且小骑士受到后坐力
- **In-Scope**: 分析并修复攻击方向错误时的副作用
- **Out-of-Scope**: 正常攻击行为

## 1.5 Code Map (Project Topology)
- **Core Logic**:
  - `StubbornKnight.cs:63-96`: `HeroController_Attack` - 攻击拦截入口
  - `ArrowGame.cs:46-63`: `IsAttackAllowed()` - 检查攻击方向是否匹配
  - `ArrowGame.cs:136-177`: `TriggerErrorEffect()` - 错误方向时触发的视觉效果
- **Entry Points**:
  - `On.HeroController.Attack` hook - 拦截攻击
- **Data Models**:
  - `AttackDirection` enum (upward, downward, normal, left, right)
  - `ArrowDirection` enum (Up, Down, Left, Right)

## 2. Architecture (Optional - Populated in INNOVATE)
### Bug 根因分析
**当前实现**：
```csharp
private void HeroController_Attack(orig, self, dir) {
    if (!isSuccess) {
        arrowGame.TriggerErrorEffect();
        // ❌ 没有调用 orig，所以没有挥动画
    } else {
        orig(self, dir);  // 允许原始攻击
    }
}
```

**问题分析**：
- 用户看到的是"墙上击中特效"（挂载在墙上的组件）
- 角色受到"后坐力"（recoiling/recoilingLeft/recoilingRight）
- 这说明虽然攻击动画没播放，但碰撞检测确实发生了

**可能根因**：
1. `Attack` 方法内部在设置 cState.attacking 后，可能通过其他机制触发了碰撞
2. 或者 PlayMaker FSM 仍在处理攻击判定
3. 需要检查 slashComponent 是否被意外调用

## 3. Detailed Design & Implementation (Populated in PLAN)
### 3.1 修复方案（最终）

**根因定位**：
`CheckForTerrainThunk` coroutine！
- 第 3934 行：`nailTerrainImpactEffectPrefab.Spawn()` - 墙上击中特效
- 第 3939-3943 行：`RecoilLeft()` / `RecoilRight()` - 后坐力

这个 coroutine 在 `orig_DoAttack` 中启动，**即使拦截了 Attack 方法，它仍然会运行！**

**解决方案**：Hook DoAttack，完全阻止 Attack 和 CheckForTerrainThunk 启动

### 3.2 Implementation Checklist
- [x] 1. 改用 DoAttack Hook（在更底层拦截）
- [x] 2. 使用 Input.GetAxisRaw 判断攻击方向（与 Spell 拦截器一致）
- [x] 3. 方向错误时不调用 orig，直接触发错误特效
- [x] 4. 编译测试
- [x] 5. 验证修复效果 ✅