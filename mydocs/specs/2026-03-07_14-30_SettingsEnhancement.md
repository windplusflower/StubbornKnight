# SDD Spec: 箭头队列设置增强

## 0. 🚨 Open Questions (MUST BE CLEAR BEFORE CODING)
- [x] 箭头队列长度范围：1~10，默认3
- [x] 箭头不透明度范围：0.1~1.0，默认1.0

## 1. Requirements (Context)
- **Goal**: 允许玩家在mod设置页面自定义箭头队列长度和箭头不透明度
- **In-Scope**: 
  - Settings 类新增两个字段
  - GetMenuData 新增两个菜单项
  - ArrowGame 使用设置值替代硬编码常量
- **Out-of-Scope**: 修改箭头动画逻辑、添加新功能

## 1.5 Code Map (Project Topology)
- **Core Logic**:
  - `StubbornKnight.cs`: Mod主类，包含Settings定义和菜单配置
    - `Settings` (L17-21): 设置数据类，当前仅有 `bool on`
    - `GetMenuData()` (L201-221): IMenuMod接口实现，返回菜单项列表
  - `ArrowGame.cs`: 箭头游戏逻辑组件
    - `ArrowCount` (L23): 常量，当前硬编码为4
    - `CreateArrowDisplay()` (L339-362): 创建箭头显示，使用 `sr.color = Color.white`
    - `SetTopArrowTransparent()` (L393-401): 设置顶部箭头透明度
- **Entry Points**:
  - `HeroController_Start` (L51-57): 将设置传递给 ArrowGame
- **Dependencies**:
  - Satchel: 用于菜单UI扩展

## 2. Architecture (Optional - Populated in INNOVATE)
- 策略：菜单使用滑块控件（Slider）实现数值调节
- 箭头队列长度用整数滑块，不透明度用浮点滑块

## 3. Detailed Design & Implementation (Populated in PLAN)

### 3.1 Data Structures & Interfaces

#### File: StubbornKnight.cs
- **Settings 类** (L17-21):
  - 新增 `public int arrowCount = 3;` (范围 1-10)
  - 新增 `public float arrowOpacity = 1.0f;` (范围 0.1-1.0)
  
- **GetMenuData 方法** (L201-221):
  - 新增菜单项1：
    - Name: "Arrow Count" (或本地化键值)
    - Values: 通过循环生成 "1", "2", ..., "10"
    - Saver: `i => mySettings.arrowCount = i + 1`
    - Loader: `() => mySettings.arrowCount - 1`
  - 新增菜单项2：
    - Name: "Arrow Opacity" (或本地化键值)
    - Values: 生成 0.1 到 1.0 的数组（步长 0.1）
    - Saver: `i => mySettings.arrowOpacity = (i + 1) * 0.1f`
    - Loader: `() => (int)(mySettings.arrowOpacity * 10) - 1`

#### File: ArrowGame.cs
- **修改 CreateArrowDisplay 方法** (L339-362):
  - 使用 `mySettings.arrowCount` 替代硬编码 `ArrowCount` 常量
  - 动态创建对应数量的 SpriteRenderer
  - 使用 `mySettings.arrowOpacity` 初始化 `sr.color`
  
- **新增 ApplyOpacity 方法**:
  - `public void ApplyOpacity(float opacity)`：设置所有箭头的不透明度

- **修改相关数组大小逻辑**:
  - `_arrowRenderers` 和 `_currentArrows` 数组大小改为动态
  - `CurrentTargetArrow` 返回最后一个有效元素

### 3.2 Implementation Checklist
- [x] 1. 更新 `Settings` 类：添加 `arrowCount` 和 `arrowOpacity` 字段
- [x] 2. 更新 `GetMenuData` 方法：添加两个新菜单项
- [x] 3. 修改 `ArrowGame` 类：移除 `ArrowCount` 常量，改用实例变量
- [x] 4. 修改 `CreateArrowDisplay` 方法：支持动态箭头数量和透明度
- [x] 5. 修改 `UpdateArrowDisplay` 方法：支持动态数组大小
- [x] 6. 修改 `RollArrowsWithAnimation` 方法：支持动态数组大小
- [x] 7. 添加 `SetConfig` 和 `ApplyOpacity` 公开方法供外部调用
- [x] 8. 在 `HeroController_Start` 中传递设置值给 ArrowGame
- [x] 9. 编译验证 ✓