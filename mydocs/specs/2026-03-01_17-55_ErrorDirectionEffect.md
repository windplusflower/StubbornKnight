# SDD Spec: 错误方向释放特效

## 0. 🚨 Open Questions (MUST BE CLEAR BEFORE CODING)
- [x] 无问题，需求清晰

## 1. Requirements (Context)
- **Goal**: 当角色在错误方向释放骨钉、法术或剑技时，最下面的箭头（玩家需遵守的目标箭头）显示红色外发光并原地快速震动
- **In-Scope**: 
  - ArrowGame.cs 添加错误特效方法
  - SpellInterceptAction.cs 在失败时触发特效
  - NailArtInterceptAction.cs 在失败时触发特效
- **Out-of-Scope**: 其他视觉特效修改

## 1.5 Code Map (Project Topology)
- **Core Logic**:
  - `ArrowGame.cs`: 箭头游戏核心逻辑，箭头渲染在 `_arrowRenderers` 数组中，**索引0是最下面的箭头**（滚动时淡出）
- **Entry Points**:
  - `SpellInterceptAction.cs`: 法术拦截，失败时发送 "FSM CANCEL" 事件
  - `NailArtInterceptAction.cs`: 剑技拦截，失败时发送 "CANCEL" 事件

## 2. Architecture (Optional)
- 使用Unity Material的Emission实现红色外发光
- 使用协程实现原地震动动画

## 3. Detailed Design & Implementation
### 3.1 Data Structures & Interfaces
- **File: ArrowGame.cs**
  - 新增方法: `public void TriggerErrorEffect()`
  - 新增协程: `private IEnumerator PlayErrorEffectCoroutine()`

- **File: SpellInterceptAction.cs**
  - 修改: 在 `isSuccess == false` 分支调用 `arrowGame.TriggerErrorEffect()`

- **File: NailArtInterceptAction.cs**
  - 修改: 在 `isSuccess == false` 分支调用 `arrowGame.TriggerErrorEffect()`

### 3.2 Implementation Checklist
- [ ] 1. 在 ArrowGame.cs 添加 `TriggerErrorEffect()` 公共方法
- [ ] 2. 实现红色外发光效果（通过Material.SetColor设置Emission）
- [ ] 3. 实现原地快速震动（使用随机小幅度位移，震动时长约0.3秒）
- [ ] 4. 在 StubbornKnight.cs HeroController_Attack 失败分支调用 `arrowGame.TriggerErrorEffect()`
- [ ] 5. 在 SpellInterceptAction.cs 失败分支调用 `arrowGame.TriggerErrorEffect()`
- [ ] 6. 在 NailArtInterceptAction.cs 失败分支调用 `arrowGame.TriggerErrorEffect()`
- [ ] 7. 构建验证