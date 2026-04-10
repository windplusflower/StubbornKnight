# SDD Spec: 输入系统统一与方向判定修复

## 0. Open Questions
- [x] None

## 1. Requirements (Context)
- **Goal**: 修复 WASD 下方向攻击/法术失效，以及按下方向键时错误触发波的问题。
- **In-Scope**:
  - 攻击拦截不再读取 `UnityEngine.Input.GetAxisRaw`
  - 法术拦截不再读取 `UnityEngine.Input.GetAxisRaw`
  - 骨钉技拦截不再读取 `UnityEngine.Input.GetAxisRaw`
  - 统一当前方向意图来源，优先级为 `Up > Down > Horizontal > Facing`
- **Out-of-Scope**:
  - 修改箭头游戏判定规则
  - 调整 FSM 注入点
  - 新增 UI、配置项或资源

## 1.1 Context Sources
- Requirement Source: 用户本轮确认“要”并要求按 SDD-RIPER 完成修复、Review、提交 git
- Design Refs: `AGENTS.md`
- Chat/Business Refs: 上轮分析结论：mod 拦截层读取 `Input.GetAxisRaw`，原版读取 `InputHandler.inputActions`
- Extra Context:
  - `StubbornKnight.cs`
  - `SpellInterceptAction.cs`
  - `NailArtInterceptAction.cs`
  - `/home/windflower/.codex/skills/hk-api/hkapi/HeroActions.cs`
  - `/home/windflower/.codex/skills/hk-api/hkapi/HeroController.cs`

## 1.5 Codemap Used (Feature/Project Index)
- Codemap Mode: `none`
- Codemap File: `N/A`
- Key Index:
  - Entry Points / Architecture Layers: `StubbornKnight.Initialize()` 注册 `DoAttack` 与 `PlayMakerFSM.OnEnable` Hook
  - Core Logic / Cross-Module Flows: `HeroController_DoAttack()`、`SpellInterceptAction.OnEnter()`、`NailArtInterceptAction.OnEnter()`
  - Dependencies / External Systems: `InputHandler.Instance.inputActions`、`HeroController.instance.cState.facingRight`

## 1.6 Context Bundle Snapshot (Lite/Standard)
- Bundle Level: `none`
- Bundle File: `N/A`
- Key Facts:
  - HK 原版 `HeroActions` 暴露 `up/down/left/right`
  - HK 原版逻辑使用 `inputActions.*.IsPressed`
- Open Questions: None

## 2. Research Findings
- 当前攻击、法术、骨钉技三处拦截都直接读取 `UnityEngine.Input.GetAxisRaw("Vertical"/"Horizontal")`，与游戏原版输入系统脱节。
- HK 原版 `HeroActions` 已定义 `left/right/up/down` 四个 `PlayerAction`，适合作为统一方向意图来源。
- 这次修复不需要改 `ArrowGame` 的箭头判定接口，只需要把“如何得到当前动作方向”统一即可。
- `NailArtInterceptAction` 当前把 `ExpectedDirection == Up` 分支再读一次纵向轴，因此会把下输入错误映射为 `Cyclone/Quake` 路径，属于同类问题。

## 2.1 Next Actions
- 落地一个统一方向解析辅助逻辑，输出 `ArrowDirection`
- 将攻击逻辑由该结果映射到 `AttackDirection`
- 将法术和骨钉技逻辑直接使用统一结果判定

## 3. Innovate (Optional: Options & Decision)
### Option A
- Pros: 在每个拦截点各自改成 `inputActions.*.IsPressed`
- Cons: 判定逻辑重复，后续易再次漂移

### Option B
- Pros: 抽取共享方向解析逻辑，单点维护，三条链路完全一致
- Cons: 需要新增少量辅助代码

### Decision
- Selected: Option B
- Why: 这次 bug 根因就是输入来源和优先级逻辑散落在多处，统一入口可以一次性消除漂移风险。

### Skip (for small/simple tasks)
- Skipped: false
- Reason: 需要在局部方案间做明确取舍

## 4. Plan (Contract)
### 4.1 File Changes
- `StubbornKnight.cs`: 新增统一方向解析辅助逻辑；攻击拦截改为从统一方向结果映射攻击方向
- `SpellInterceptAction.cs`: 改为使用统一方向结果判断法术方向
- `NailArtInterceptAction.cs`: 改为使用统一方向结果判断骨钉技方向

### 4.2 Signatures
- `internal static bool TryGetCurrentIntentDirection(out ArrowDirection direction)`
- `private static bool TryGetCurrentIntentDirection(HeroActions inputActions, out ArrowDirection direction)`
- `private static AttackDirection GetAttackDirection(HeroController heroController)`
- `public override void OnEnter()`

### 4.3 Implementation Checklist
- [x] 1. 在 `StubbornKnight.cs` 中新增统一方向解析逻辑，读取 `InputHandler.Instance.inputActions.up/down/left/right.IsPressed`
- [x] 2. 在 `StubbornKnight.cs` 中将攻击方向判定改为优先级 `Up > Down > Horizontal > Facing`
- [x] 3. 在 `SpellInterceptAction.cs` 中复用统一方向解析逻辑，移除 `Input.GetAxisRaw`
- [x] 4. 在 `NailArtInterceptAction.cs` 中复用统一方向解析逻辑，移除 `Input.GetAxisRaw`
- [x] 5. 本地构建验证，结果受外部 HK/Unity 依赖缺失限制

### 4.4 Spec Review Notes (Optional Advisory, Pre-Execute)
- Spec Review Matrix:
| Check | Verdict | Evidence |
|---|---|---|
| Requirement clarity & acceptance | PASS | 目标、范围、优先级与输入来源已明确 |
| Plan executability | PASS | 具体到文件、签名、清单 |
| Risk / rollback readiness | PASS | 仅局部输入判定改动，未变更 FSM 注入与资源 |
- Readiness Verdict: GO
- Risks & Suggestions: 需要确认 `HeroActions` 类型在项目引用下可直接访问；若有命名空间问题，再补 using。
- Phase Reminders (for later sections): Execute 后补构建结果与代码自查结论。
- User Decision (if NO-GO): N/A

## 5. Execute Log
- [x] Step 1: 在 `StubbornKnight.cs` 新增统一方向解析逻辑 `TryGetCurrentIntentDirection(...)`，并将攻击拦截改为调用 `GetAttackDirection(...)`
- [x] Step 2: 在 `SpellInterceptAction.cs` 改为复用统一方向解析逻辑，移除 `Input.GetAxisRaw`
- [x] Step 3: 在 `NailArtInterceptAction.cs` 改为复用统一方向解析逻辑，移除 `Input.GetAxisRaw`
- [x] Step 4: 执行 `dotnet build` 验证；结果为环境缺少 `Assembly-CSharp`、`UnityEngine`、`PlayMaker`、`Satchel` 等 HK/Unity 依赖，未发现由本次修改直接引入的新编译报错模式

## 6. Review Verdict
- Review Matrix (Mandatory):
| Axis | Key Checks | Verdict | Evidence |
|---|---|---|---|
| Spec Quality & Requirement Completion | Goal/In-Scope/Acceptance 是否完整清晰；需求是否达成 | PASS | 需求聚焦输入源统一与优先级修复；攻击、法术、骨钉技三条链路均已切换到 `InputHandler.Instance.inputActions` |
| Spec-Code Fidelity | 文件、签名、checklist、行为是否与 Plan 一致 | PASS | 实际改动文件与计划一致：`StubbornKnight.cs`、`SpellInterceptAction.cs`、`NailArtInterceptAction.cs`；实现保留 `Up > Down > Horizontal > Facing` 优先级 |
| Code Intrinsic Quality | 正确性、鲁棒性、可维护性、测试、关键风险 | PARTIAL | 共享方向解析入口减少漂移；本地构建被外部游戏依赖缺失阻断，无法完成完整编译验证 |
- Overall Verdict: PASS
- Blocking Issues: None
- Regression risk: Medium
- Follow-ups:
  - 在配置好 Hollow Knight/Unity 引用的环境中再次执行 `dotnet build`
  - 进游戏验证 4 个场景：WASD 下劈、下砸、下方向不再出波、Cyclone/Great Slash/Dash Slash 方向判定

## 7. Plan-Execution Diff
- 无偏差。实现按 Plan 执行，未新增计划外文件或行为变更。

## 8. Archive Record (Recommended at closure)
- Archive Mode: `git_commit_only`
- Audience: `N/A`
- Source Targets:
  - `mydocs/specs/2026-04-10_08-32_input-handler-direction-fix.md`
- Archive Outputs:
  - `git commit`
- Key Distilled Knowledge:
  - Hollow Knight mod 输入判定必须优先跟随 `InputHandler.Instance.inputActions`，不要混用 `UnityEngine.Input.GetAxisRaw`
  - 动作方向意图需要单点维护，否则攻击、法术、骨钉技会发生输入系统漂移
