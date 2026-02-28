# SDD Spec: 滚动箭头淡入淡出效果

## 0. 🚨 Open Questions (MUST BE CLEAR BEFORE CODING)
- [x] 淡入淡出时长：与滚动动画同步（0.2秒）
- [x] 离开箭头：向下移动时透明度从1降到0
- [x] 进入箭头：从上方进入时透明度从0升到1
- [x] 新箭头预生成：在动画开始时就准备好下一个箭头

## 1. Requirements (Context)
- **Goal**: 为滚动箭头添加淡入淡出效果，增强视觉流畅感
- **In-Scope**: 
  - 底部离开箭头淡出效果
  - 顶部新进入箭头淡入效果
- **Out-of-Scope**: 颜色变化、缩放效果

## 1.5 Code Map (Project Topology)
- **Core Logic**:
  - `RhythmKnight.cs:219-261`: `RollArrowsWithAnimation()` - 当前滚动动画协程
  - `RhythmKnight.cs:197-211`: `UpdateArrowDisplay()` - 更新箭头显示
  - `RhythmKnight.cs:34`: `_arrowRenderers` - SpriteRenderer数组

## 2. Architecture (Optional - Populated in INNOVATE)
- Strategy: 在滚动动画中同时处理位置和透明度插值

## 3. Detailed Design & Implementation (Populated in PLAN)
### 3.1 Data Structures & Interfaces
- `File: RhythmKnight.cs`
  - `class ArrowGame`:
    - 修改 `RollArrowsWithAnimation()` 协程：
      - 底部箭头（index=2）: 位置下移 + 透明度从1到0
      - 中间箭头（index=0,1）: 位置下移 + 保持透明度1
      - 动画开始时预生成新箭头，透明度设为0
      - 动画后半段：新箭头透明度从0到1

### 3.2 Implementation Checklist
- [x] 1. 修改 `RollArrowsWithAnimation()`：
    - 记录每个箭头的起始透明度
    - 底部箭头（index=2）: 动画期间透明度从1到0
    - 顶部新箭头: 动画后半段（t > 0.5f）透明度从0到1
- [x] 2. 确保 `UpdateArrowDisplay()` 中新箭头初始透明度为0
- [x] 3. 在动画结束时确保所有箭头透明度恢复到1