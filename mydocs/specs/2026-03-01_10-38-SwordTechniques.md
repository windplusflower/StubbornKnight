# SDD Spec: 剑技拦截逻辑实现

## 0. 🚨 Open Questions (MUST BE CLEAR BEFORE CODING)
- [x] 实现方案：通过 Hook CanNailArt 方法，读取用户输入判断剑技方向
- [x] 滚动时机：匹配成功后直接调用 OnSuccessfulAction

## 1. Requirements (Context)
- **Goal**: 为骨钉剑技 (蓄力斩、冲刺劈砍、旋风斩) 添加拦截逻辑，使其与箭头方向匹配
- **In-Scope**: 
  - Hook `CanNailArt` 方法进行剑技拦截
  - 读取垂直输入判断剑技方向（上/下/无）
  - 根据方向匹配底部箭头，不匹配则拦截
  - 匹配成功后直接调用 OnSuccessfulAction 触发滚动
- **Out-of-Scope**: 
  - 区分冲刺劈砍和蓄力斩（统一按左右处理）
  - FSM 相关修改

## 1.5 Code Map (Project Topology)
- **Core Logic**:
  - `StubbornKnight.cs` (27-318): ArrowGame 组件，管理箭头 UI 和方向匹配逻辑
  - `StubbornKnight.cs` (399-547): StubbornKnight 主类，Mod 入口和 Hook 注册
- **Entry Points**:
  - `HeroController_Attack` (436-476): 骨钉攻击 Hook
  - **新增**: `HeroController_CanNailArt`: 剑技拦截 Hook
- **依赖**:
  - HK API: `HeroController.CanNailArt()` 用于判断是否允许释放剑技
  - 输入轴：`Input.GetAxisRaw("Vertical")` 判断剑技方向

## 2. Architecture
- **Strategy**: 在 CanNailArt Hook 中通过输入轴判断剑技方向并拦截
- **Pattern**: 
  1. 注册 `On.HeroController.CanNailArt` Hook
  2. 在 Hook 中读取 `Input.GetAxisRaw("Vertical")`
  3. 根据垂直输入映射到方向（上→Up，下→Down，无→左/右）
  4. 调用 `ArrowGame.IsSpellAllowed()` 检查匹配
  5. 不匹配则返回 false，匹配则调用 OnSuccessfulAction 并返回 orig 结果
- **Trade-offs**: 
  - ✅ 实现简单，无需处理 FSM
  - ✅ 直接通过输入判断，逻辑清晰
  - ⚠️ 无法区分冲刺劈砍和蓄力斩（但符合需求）

## 3. Detailed Design & Implementation

### 3.1 Data Structures & Interfaces
- **Existing**:
  - `ArrowDirection` enum: Up, Down, Left, Right
  - `ArrowGame.IsSpellAllowed(ArrowDirection dir)`: 方向匹配检查
  - `ArrowGame.OnSuccessfulAction()`: 成功执行后触发箭头滚动
- **New Hook**:
  - `On.HeroController.CanNailArt`: 剑技可用性检查 Hook

### 3.2 Implementation Checklist
- [ ] 1. 在 `Initialize` 方法中添加剑技 Hook 注册
  - `On.HeroController.CanNailArt += HeroController_CanNailArt;`
  
- [ ] 2. 实现 `HeroController_CanNailArt` 方法
  - 调用 orig 获取原始结果
  - 如果 Mod 关闭或 orig 返回 false，直接返回结果
  - 获取 ArrowGame 组件，不存在则返回 orig 结果
  - 读取 `Input.GetAxisRaw("Vertical")` 判断剑技方向：
    - `> 0.1f` → ArrowDirection.Up (旋风斩)
    - `< -0.1f` → ArrowDirection.Down (冲刺劈砍)
    - 其他 → 根据 facingRight 映射到 Left/Right (蓄力斩)
  - 调用 `IsSpellAllowed(方向)` 检查匹配
  - 不匹配：返回 false，输出失败日志
  - 匹配：调用 `OnSuccessfulAction()`，返回 orig 结果，输出成功日志
  
- [ ] 3. 添加日志输出，与攻击/法术拦截格式保持一致
  - `[NailArt] Expected: {方向}, Actual: {剑技名}({方向}), Result: SUCCESS/FAILED`

### 3.3 Direction Mapping Logic
| 垂直输入 | 剑技类型 | ArrowDirection |
|---------|---------|----------------|
| `> 0.1f` | 旋风斩 | Up |
| `< -0.1f` | 冲刺劈砍 | Down |
| 其他 | 蓄力斩 | Left/Right (根据 facingRight) |

### 3.4 Logic Flow
```
HeroController_CanNailArt(orig, self)
  ├── result = orig(self)
  ├── if !mySettings.on || !result → return result
  ├── arrowGame = self.GetComponent<ArrowGame>()
  ├── if arrowGame == null → return result
  ├── vertical = Input.GetAxisRaw("Vertical")
  ├── if vertical > 0.1f → arrowDir = Up, name = "Cyclone Slash"
  ├── else if vertical < -0.1f → arrowDir = Down, name = "Dash Slash"
  ├── else → arrowDir = facingRight ? Right : Left, name = "Great Slash"
  ├── expected = arrowGame.CurrentTargetArrow
  ├── if arrowDir != expected
  │   ├── Log 失败
  │   └── return false  // 拦截剑技
  ├── else
  │   ├── arrowGame.OnSuccessfulAction()
  │   ├── Log 成功
  │   └── return result  // 允许剑技
```

## 4. Testing Notes
- 测试场景：在神居或普通场景中使用三种剑技
- 验证点:
  - 上 + 蓄力释放旋风斩时匹配上箭头
  - 下 + 蓄力释放冲刺劈砍时匹配下箭头
  - 无方向 + 蓄力释放蓄力斩时匹配左右箭头
  - 拦截时显示正确日志
  - 成功释放后箭头滚动
