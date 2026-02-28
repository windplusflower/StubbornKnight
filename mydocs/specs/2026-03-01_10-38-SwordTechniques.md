# SDD Spec: 剑技拦截逻辑实现

## 0. 🚨 Open Questions (MUST BE CLEAR BEFORE CODING)
- [x] 蓄力斩(Great Slash)和冲刺劈砍(Dash Slash)的方向判定 → 同波，左右箭头
- [x] 旋风斩(Cyclone Slash)的方向判定 → 上+蓄力=上箭头，下+蓄力=下箭头

## 1. Requirements (Context)
- **Goal**: 为骨钉剑技(蓄力斩、冲刺劈砍、旋风斩)添加拦截逻辑，使其与箭头方向匹配
- **In-Scope**: 
  - 蓄力斩(Great Slash)拦截
  - 冲刺劈砍(Dash Slash)拦截  
  - 旋风斩(Cyclone Slash)拦截
  - 基于输入轴判断剑技类型和方向
- **Out-of-Scope**: 
  - 其他攻击方式的修改
  - UI显示改动

## 1.5 Code Map (Project Topology)
- **Core Logic**:
  - `StubbornKnight.cs` (27-318): ArrowGame组件，管理箭头UI和方向匹配逻辑
  - `StubbornKnight.cs` (320-390): SpellInterceptAction，法术拦截Action
  - `StubbornKnight.cs` (399-547): StubbornKnight主类，Mod入口和Hook注册
- **Entry Points**:
  - `HeroController_Attack` (436-476): 骨钉攻击Hook
  - `PlayMakerFSM_OnEnable` (478-490): FSM修改入口
  - `ModifySpellControlFSM` (492-500): 法术FSM修改
- **拦截点**:
  - 法术FSM: "Spell Control" → "QC"/"Spell Choice" 状态
  - 攻击Hook: `On.HeroController.Attack`
  - **剑技FSM**: "Nail Art" (需注入)

## 2. Architecture
- **Strategy**: 复用现有FSM注入模式，为剑技FSM添加拦截Action
- **Pattern**: 
  1. 创建 `NailArtInterceptAction` 类继承 `FsmStateAction`
  2. 在 `PlayMakerFSM_OnEnable` 中检测 "Nail Art" FSM
  3. 注入到剑技触发状态(G Slash, D Slash, C Slash)
  4. 通过输入轴判断剑技类型和方向
  5. 调用 `ArrowGame.IsSpellAllowed()` 进行匹配检查
- **Trade-offs**: 
  - ✅ 复用现有架构，代码一致性强
  - ⚠️ 需确认剑技FSM的确切状态名称

## 3. Detailed Design & Implementation

### 3.1 Data Structures & Interfaces
- **Existing**:
  - `ArrowDirection` enum: Up, Down, Left, Right (19-25)
  - `ArrowGame.IsSpellAllowed(ArrowDirection dir)` (66-69): 法术方向匹配检查
  - `ArrowGame.OnSuccessfulAction()` (72-78): 成功执行后触发箭头滚动
- **New**:
  - `NailArtInterceptAction` class: 继承 `FsmStateAction`
    - `override void OnEnter()`: 拦截逻辑入口

### 3.2 Implementation Checklist
- [ ] 1. 创建 `NailArtInterceptAction` 类，实现剑技拦截逻辑
  - 读取输入轴判断剑技类型和方向
  - 蓄力斩/冲刺劈砍: 根据朝向或水平输入判断左右
  - 旋风斩: 上/下蓄力判断上下
  - 调用 `ArrowGame.IsSpellAllowed()` 检查匹配
  - 匹配失败则 `Fsm.Event("FSM CANCEL")`
  - 匹配成功则调用 `arrowGame.OnSuccessfulAction()`
  
- [ ] 2. 在 `PlayMakerFSM_OnEnable` 中添加 "Nail Art" FSM检测
  - 检测条件: `self.FsmName == "Nail Art"`
  - 注入目标状态: "G Slash", "D Slash", "C Slash"
  
- [ ] 3. 添加 `ModifyNailArtFSM` 方法
  - 调用 `InjectNailArtAction` 为每个剑技状态注入拦截Action
  
- [ ] 4. 添加 `InjectNailArtAction` 辅助方法
  - 复用 `InjectSpellAction` 模式
  
- [ ] 5. 添加日志输出，与法术拦截格式保持一致

### 3.3 State Injection Points
| 剑技 | FSM状态名 | 方向判定逻辑 | 期望箭头 |
|------|----------|-------------|----------|
| 蓄力斩(Great Slash) | "G Slash" | 根据Hero朝向(facingRight) | 左/右 |
| 冲刺劈砍(Dash Slash) | "D Slash" | 根据Hero朝向(facingRight) | 左/右 |
| 旋风斩(Cyclone Slash) | "C Slash" | Vertical > 0 = 上, Vertical < 0 = 下 | 上/下 |

### 3.4 Logic Flow
```
NailArtInterceptAction.OnEnter()
  ├── 获取ArrowGame组件
  ├── 读取输入轴(Vertical)
  ├── 判断当前FSM状态名
  │   ├── "G Slash" → 方向 = facingRight ? Right : Left
  │   ├── "D Slash" → 方向 = facingRight ? Right : Left
  │   └── "C Slash" → 方向 = Vertical > 0 ? Up : Down
  ├── 调用 IsSpellAllowed(方向)
  │   ├── false → Fsm.Event("FSM CANCEL") + 日志
  │   └── true → OnSuccessfulAction() + 日志
  └── Finish()
```

## 4. Testing Notes
- 测试场景: 在神居或普通场景中使用三种剑技
- 验证点:
  - 蓄力斩/冲刺劈砍在箭头为左右时正确触发/拦截
  - 旋风斩在上+蓄力时匹配上箭头，下+蓄力时匹配下箭头
  - 成功释放后箭头正确滚动
  - 拦截时显示正确日志
