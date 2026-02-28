# SDD Spec: 滚动节奏按键动画增强

## 0. 🚨 Open Questions (MUST BE CLEAR BEFORE CODING)
- [x] 动画类型：使用平滑滚动动画
- [x] 动画时长：0.2秒
- [x] 缓动函数：使用 ease-in-out
- [x] 动画期间输入：禁止

## 1. Requirements (Context)
- **Goal**: 为滚动节奏按键添加滚动动画，使其不再生硬
- **In-Scope**: 
  - 添加滚动动画效果
  - 保持现有逻辑不变
- **Out-of-Scope**: 音效、粒子效果

## 1.5 Code Map (Project Topology)
- **Core Logic**:
  - `RhythmKnight.cs:28-223`: ArrowGame 类 - 节奏箭头游戏核心逻辑
    - `RollArrows()` (L211-222): 箭头滚动逻辑 - **需要添加动画**
    - `UpdateArrowDisplay()` (L197-209): 更新箭头显示
    - `_arrowRenderers` (L34): SpriteRenderer 数组，控制3个箭头
    - `_container` (L38): 箭头容器

## 2. Architecture (Optional - Populated in INNOVATE)
- Strategy: 使用协程实现平滑滚动动画

## 3. Detailed Design & Implementation (Populated in PLAN)
### 3.1 Data Structures & Interfaces
- `File: RhythmKnight.cs`
  - `class ArrowGame`:
    - 添加字段: `private bool _isAnimating = false;` (动画状态标志)
    - 添加字段: `private const float AnimationDuration = 0.2f;` (动画时长)
    - 添加方法: `private IEnumerator RollArrowsWithAnimation()` (滚动动画协程)
    - 修改方法: `private void RollArrows()` -> 启动协程，不直接更新

### 3.2 Implementation Checklist
- [x] 1. 在 ArrowGame 类添加 `_isAnimating` 字段和 `AnimationDuration` 常量
- [x] 2. 将 `RollArrows()` 改为启动协程 `RollArrowsWithAnimation()`
- [x] 3. 实现 `RollArrowsWithAnimation()` 协程方法：
    - 设置 `_isAnimating = true`
    - 记录起始位置和目标位置
    - 使用 Mathf.SmoothStep 实现 ease-in-out 插值
    - 动画完成后更新箭头数据并调用 `UpdateArrowDisplay()`
    - 设置 `_isAnimating = false`
- [x] 4. 在 Update() 中添加 `_isAnimating` 检查，动画期间禁止输入
