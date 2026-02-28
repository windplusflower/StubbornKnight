# SDD Spec: StubbornKnight 战斗机制重构

## 0. 🚨 Open Questions (MUST BE CLEAR BEFORE CODING)
- [x] 攻击方向如何获取？通过 Input.GetAxisRaw + HeroController 朝向
- [x] 法术拦截方式？Hook "Spell Control" FSM 的关键状态
- [x] 取消攻击/法术后是否消耗资源？攻击不消耗，法术不消耗灵魂
- [x] 滚动箭头的时机？攻击/法术成功执行后

*(If all clear, mark as "None")*

## 1. Requirements (Context)
- **Goal**: 将方向键匹配机制替换为攻击和法术匹配机制
- **In-Scope**: 
  - 拦截普通攻击（上下左右斩击）
  - 拦截三种法术（波/吼/砸）
  - 根据底部箭头方向判断是否允许执行
  - 执行成功后滚动箭头
- **Out-of-Scope**: 
  - 骨钉技艺（Nail Arts）
  - 梦之钉
  - 其他特殊攻击

## 1.5 Code Map (Project Topology)
- **Core Logic**:
  - `StubbornKnight.cs:ArrowGame`: 核心游戏组件，管理箭头显示和匹配逻辑
- **Entry Points**:
  - `On.HeroController.Attack`: 普通攻击拦截
  - `PlayMakerFSM_OnEnable`: Spell Control FSM 修改
- **Data Models**:
  - `ArrowDirection` 枚举: Up, Down, Left, Right
  - `_currentArrows[3]`: 底部箭头（匹配目标）
- **Dependencies**:
  - PlayMaker FSM: 法术系统控制
  - InputHandler: 输入检测

## 2. Architecture (Populated in INNOVATE)
- **Strategy/Pattern**: Hook-based interception
- **攻击拦截**: 使用 `On.HeroController.Attack` hook，在调用 orig 前检查方向
- **法术拦截**: 在 "Spell Control" FSM 的 "QC" 和 "Spell Choice" 状态注入自定义 Action
- **方向检测**: 
  - 攻击：通过垂直输入 + HeroController 朝向判断
  - 法术：通过 FSM 变量判断（读取 "Pressed Up"/"Pressed Down" 布尔变量）
- **Trade-offs**:
  - 攻击 Hook vs FSM 修改：Hook 更直接，FSM 更复杂但精细
  - 使用公共字段暴露 ArrowGame 状态供外部检查

## 3. Detailed Design & Implementation (Populated in PLAN)

### 3.1 Data Structures & Interfaces
- `File: StubbornKnight.cs`
    - `class ArrowGame`:
        - **新增字段**:
            - `public ArrowDirection CurrentTargetArrow`: 获取 `_currentArrows[3]` 的公共访问器
            - `public bool IsAttackAllowed(AttackDirection dir)`: 检查攻击方向是否匹配
            - `public bool IsSpellAllowed(SpellType type)`: 检查法术类型是否匹配
        - **新增方法**:
            - `public void OnSuccessfulAction()`: 成功执行动作后调用，触发 RollArrows()
    - **新增枚举**:
        - `AttackDirection`: Up, Down, Left, Right
        - `SpellType`: Fireball, Shriek, Quake

### 3.2 Implementation Checklist
- [x] 1. **修改 ArrowGame 类**
  - [x] 1.1 添加 `CurrentTargetArrow` 公共属性
  - [x] 1.2 添加 `IsAttackAllowed(AttackDirection dir)` 方法
  - [x] 1.3 添加 `IsSpellAllowed(SpellType type)` 方法
  - [x] 1.4 添加 `OnSuccessfulAction()` 方法
  - [x] 1.5 移除 `Update()` 中的方向键检测逻辑

- [x] 2. **添加攻击 Hook**
  - [x] 2.1 在 `StubbornKnight.Initialize` 中注册 `On.HeroController.Attack`
  - [x] 2.2 实现 `HeroController_Attack` 方法
  - [x] 2.3 获取攻击方向（通过 HK 原生 `AttackDirection` 枚举）
  - [x] 2.4 调用 `ArrowGame.IsAttackAllowed()` 检查
  - [x] 2.5 若允许则调用 orig 并触发滚动
  - [x] 2.6 若不允许则不调用 orig（取消攻击）

- [x] 3. **添加法术 FSM 注入**
  - [x] 3.1 创建 `SpellInterceptAction : FsmStateAction` 类
  - [x] 3.2 在 `PlayMakerFSM_OnEnable` 中检测 "Spell Control" FSM
  - [x] 3.3 向 "QC" 和 "Spell Choice" 状态注入自定义 Action
  - [x] 3.4 实现法术类型判断逻辑（读取 FSM 变量 `Pressed Up`/`Pressed Down`）
  - [x] 3.5 调用 `ArrowGame.IsSpellAllowed()` 检查
  - [x] 3.6 若不允许则发送 "FSM CANCEL" 事件
  - [x] 3.7 若允许则触发 `OnSuccessfulAction()` 并执行原法术

- [x] 4. **更新箭头滚动时机**
  - [x] 4.1 在攻击成功后调用 `ArrowGame.OnSuccessfulAction()`
  - [x] 4.2 在法术成功后调用 `ArrowGame.OnSuccessfulAction()`

### 3.3 Function Signatures
```csharp
// ArrowGame 新增成员
public ArrowDirection CurrentTargetArrow => _currentArrows[3];

public bool IsAttackAllowed(AttackDirection dir)
{
    // 将 AttackDirection 转换为 ArrowDirection 并比较
}

public bool IsSpellAllowed(SpellType type)
{
    // 根据法术类型检查箭头方向
    // Fireball -> Left/Right
    // Shriek -> Up
    // Quake -> Down
}

public void OnSuccessfulAction()
{
    RollArrows();
}

// StubbornKnight 新增 Hook
private void HeroController_Attack(On.HeroController.orig_Attack orig, HeroController self, AttackDirection dir)
{
    // 拦截逻辑
}

// SpellInterceptAction
public class SpellInterceptAction : FsmStateAction
{
    public override void OnEnter()
    {
        var arrowGame = HeroController.instance?.GetComponent<ArrowGame>();
        if (arrowGame == null) { Finish(); return; }
        
        // 从 FSM 变量读取法术类型
        bool pressedUp = Fsm.Variables.GetFsmBool("Pressed Up").Value;
        bool pressedDown = Fsm.Variables.GetFsmBool("Pressed Down").Value;
        
        SpellType spellType;
        if (pressedUp) spellType = SpellType.Shriek;
        else if (pressedDown) spellType = SpellType.Quake;
        else spellType = SpellType.Fireball;
        
        if (!arrowGame.IsSpellAllowed(spellType))
        {
            Fsm.Event("FSM CANCEL");
        }
        else
        {
            arrowGame.OnSuccessfulAction();
        }
        
        Finish();
    }
}
```

## 4. Implementation Notes
- **攻击方向判断**:
  - `AttackDirection.upward`: 上斩
  - `AttackDirection.downward`: 下斩
  - `AttackDirection.normal`: 左/右斩（根据 `HeroController.cState.facingRight` 判断）
  
- **法术类型判断**:
  - 通过读取 FSM 变量判断（比 Input 更准确，因为 FSM 已经处理过了）
  - "Pressed Up" == true: Shriek (吼)
  - "Pressed Down" == true: Quake (砸)
  - 两者都为 false: Fireball (波)

- **FSM 注入要点**:
  - 在 `PlayMakerFSM_OnEnable` 中检测 FsmName == "Spell Control"
  - 需要同时注入 "QC" 和 "Spell Choice" 两个状态（法术必定经过其中之一）
  - 使用反射修改 state.Actions 数组，将自定义 Action 插入到索引 0
  - OnEnter 在状态进入时立即执行，此时可以取消法术
