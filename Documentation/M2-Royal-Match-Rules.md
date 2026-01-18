# M2 - Royal Match规则匹配系统

## ⚠️ 重要说明

**本文档包含历史实现细节，部分内容已过时。**

### 当前最终实现（2026-01-18）

**核心规则**：
1. ✅ **精确形状匹配**：所有匹配类型都有明确定义，**不使用动态扩展算法**
2. ✅ **3阶段检测**：收集所有候选 → 按优先级排序 → 贪心选择不冲突的匹配
3. ✅ **移除Cross类型**：十字型归类为T型TNT
4. ✅ **新增SquarePlus类型**：2x2+1明确检测（5个宝石）

**~~废弃~~**：`ExpandContinuousGems` BFS扩展算法（文档第二部分描述的核心算法已不再使用）

**参考最新规则**：请查看 `Documentation/MatchRule.md` 了解当前准确的匹配规则。

---

## 概述

本文档说明M2里程碑的核心改进：完全重写匹配检测系统，基于Royal Match官方规则实现精确的形状匹配算法。

**规则演变**：
1. **初始**：固定形状检测
2. **中期**：实现"连续判定"扩展算法（BFS）
3. **最终**：废弃扩展，改为精确形状匹配

**实现日期**: 2026-01-18
**版本**: M2 (最后更新: 2026-01-18)

---

## 一、问题分析

### 旧系统的关键问题

在M1实现后，用户发现匹配检测逻辑与Royal Match官方规则不符，存在以下严重问题：

#### 1. **匹配范围规则的变更** ⚠️

**规则演变历史**：
1. **初始实现**：固定检测形状，只消除匹配到的宝石（如L型只消除5个）
2. **中期调整**：实现"连续判定"扩展算法，消除所有连续同色宝石
3. **最终方案**（当前）：明确每个形状的精确定义，不使用动态扩展

**最终需求**（来自用户反馈）:
> "所有匹配类型都不需要扩展了。每个匹配模式都应该有明确的定义，只消除匹配到的具体宝石。"

**当前实现**：
```csharp
// 当前的TryFindLOrTShape() - MatchDetector.cs
// L型：两个方向各3+个宝石，总共至少5个
if (right.Count >= 3 && up.Count >= 3)
{
    var seeds = new HashSet<Gem>(right);
    foreach (var gem in up) seeds.Add(gem);
    if (seeds.Count >= 5)
    {
        // ✅ 只返回匹配到的L型宝石，不扩展
        return new MatchData(seeds.ToList(), MatchType.LShape, gemType);
    }
}
```

**示例**：
```
棋盘：
R R R R  (4个横向)
      R  (2个纵向)
      R

当前系统：检测到L型，消除6个R（恰好是L型的宝石） ✅
```

#### 2. **2x2方块检测的改进** ✅

**问题**：
- 初始版本完全没有实现Propeller（螺旋桨）的2x2田字格匹配

**解决方案**（当前实现）：
- ✅ 标准2x2方块：恰好4个宝石组成正方形
- ✅ 增强2x2+1方块：2x2核心外加周围1个同色宝石，共5个宝石
- ✅ 新增 `MatchType.SquarePlus` 枚举，明确区分两种情况
- ✅ 不使用扩展算法，而是精确检测2x2核心+周围8个位置

#### 3. **检测优先级和匹配冲突** ✅

**问题**：
- 旧系统按顺序检测并立即标记，导致低优先级匹配（如3连）抢先被检测，阻止高优先级匹配（如L型）

**解决方案**（当前实现）：
- ✅ 使用**3阶段检测策略**：
  1. **收集阶段**：收集所有可能的匹配候选，不立即标记
  2. **排序阶段**：按优先级降序排序
  3. **选择阶段**：贪心选择不冲突的匹配

**当前的Royal Match优先级**（从高到低）：
1. **Light Ball**（5+连） - 优先级1000
2. **T型TNT**（包括十字型） - 优先级800
3. **L型TNT** - 优先级700
4. **2x2+1 增强Propeller** - 优先级650
5. **2x2 Propeller** - 优先级600
6. **Rocket**（4连） - 优先级400
7. **Basic 3连** - 优先级0-30

**注意**：移除了 `MatchType.Cross`，十字型现在归类为T型TNT。

---

## 二、解决方案

### 核心策略：精确形状匹配 + 3阶段检测

**设计理念**：每个匹配类型都有明确的形状定义，只消除匹配到的宝石，不进行动态扩展。

#### 关键改进

1. **废弃连续扩展算法**
   - ~~ExpandContinuousGems~~（已删除）：之前使用BFS扩展到所有连续同色宝石
   - **当前**：每个匹配函数直接返回匹配到的宝石列表

2. **明确的形状定义**
   - **L型**：两个方向各3+个宝石，总共至少5个
   - **T型**：一个方向3+主线，另一方向单侧2+分支，总共至少5个
   - **十字型**：水平和垂直都3+，归类为T型TNT
   - **2x2**：恰好4个宝石
   - **2x2+1**：2x2核心+周围1个同色宝石，共5个

3. **3阶段检测策略**
   ```csharp
   // 阶段1：收集所有候选（不标记）
   for each position:
       检测所有匹配类型
       加入候选列表

   // 阶段2：按优先级排序
   candidates.OrderByDescending(m => m.Priority)

   // 阶段3：贪心选择不冲突的匹配
   for each candidate (按优先级):
       if 没有宝石冲突:
           选择此匹配
           标记宝石为已使用
   ```

---

### 重写后的检测系统

#### 1. 新的3阶段检测流程

```csharp
public static List<MatchData> FindMatchesEnhanced(Board board)
{
    // ===== 阶段1：收集所有候选 =====
    var allCandidates = new List<MatchData>();

    for (int x = 0; x < board.Width; x++)
    {
        for (int y = 0; y < board.Height; y++)
        {
            Gem currentGem = board.GetGem(x, y);
            if (currentGem == null) continue;

            GemType gemType = currentGem.Type;
            var emptyProcessed = new HashSet<Gem>(); // 不立即标记

            // 检测所有匹配类型
            var lightBallMatch = TryFindLightBall(board, x, y, gemType, emptyProcessed);
            if (lightBallMatch != null) allCandidates.Add(lightBallMatch);

            var lOrTMatch = TryFindLOrTShape(board, x, y, gemType, emptyProcessed);
            if (lOrTMatch != null) allCandidates.Add(lOrTMatch);

            var squarePlusMatch = TryFindSquarePlus(board, x, y, gemType, emptyProcessed);
            if (squarePlusMatch != null) allCandidates.Add(squarePlusMatch);

            var squareMatch = TryFindSquare(board, x, y, gemType, emptyProcessed);
            if (squareMatch != null) allCandidates.Add(squareMatch);

            var rocketMatch = TryFindRocket(board, x, y, gemType, emptyProcessed);
            if (rocketMatch != null) allCandidates.Add(rocketMatch);

            var basicMatch = TryFindBasicMatch(board, x, y, gemType, emptyProcessed);
            if (basicMatch != null) allCandidates.Add(basicMatch);
        }
    }

    // ===== 阶段2：按优先级排序 =====
    allCandidates = allCandidates.OrderByDescending(m => m.Priority).ToList();

    // ===== 阶段3：贪心选择不冲突的匹配 =====
    var finalMatches = new List<MatchData>();
    var usedGems = new HashSet<Gem>();

    foreach (var candidate in allCandidates)
    {
        bool hasConflict = candidate.Gems.Any(g => usedGems.Contains(g));
        if (!hasConflict)
        {
            finalMatches.Add(candidate);
            foreach (var gem in candidate.Gems)
                usedGems.Add(gem);
        }
    }

    return finalMatches;
}
```

**关键改进**：
- ✅ 3阶段设计：收集→排序→选择
- ✅ 不立即标记宝石，避免低优先级匹配阻挡高优先级匹配
- ✅ 每个检测函数直接返回匹配到的宝石，不扩展

#### 2. Light Ball检测（5+连）

```csharp
private static MatchData TryFindLightBall(Board board, int x, int y, GemType gemType, HashSet<Gem> processedGems)
{
    // 检查水平5+连
    var horizLine = CheckDirectionContinuous(board, x, y, 1, 0, gemType);
    if (horizLine.Count >= 5)
    {
        // 直接返回匹配到的直线宝石，不扩展
        if (horizLine.Any(g => processedGems.Contains(g)))
            return null;
        return new MatchData(horizLine, MatchType.Horizontal5, gemType);
    }

    // 检查垂直5+连
    var vertLine = CheckDirectionContinuous(board, x, y, 0, 1, gemType);
    if (vertLine.Count >= 5)
    {
        // 直接返回匹配到的直线宝石，不扩展
        if (vertLine.Any(g => processedGems.Contains(g)))
            return null;
        return new MatchData(vertLine, MatchType.Vertical5, gemType);
    }

    return null;
}
```

**流程**：
1. 沿单一方向检测是否有5+连续宝石
2. 如果找到，直接返回该直线上的宝石（**不扩展**）
3. 检查是否已被处理，避免重复

#### 3. L型和T型检测

```csharp
private static MatchData TryFindLOrTShape(Board board, int x, int y, GemType gemType, HashSet<Gem> processedGems)
{
    // 检查4个方向的连续宝石
    var up = CheckDirectionContinuous(board, x, y, 0, 1, gemType);
    var down = CheckDirectionContinuous(board, x, y, 0, -1, gemType);
    var left = CheckDirectionContinuous(board, x, y, -1, 0, gemType);
    var right = CheckDirectionContinuous(board, x, y, 1, 0, gemType);

    // 合并对向的连线（去重中心点）
    var vertical = MergeLines(up, down, centerGem);
    var horizontal = MergeLines(left, right, centerGem);

    // 检测十字型 (Cross) - 水平3+ 且 垂直3+
    if (horizontal.Count >= 3 && vertical.Count >= 3)
    {
        var seeds = /* 合并水平和垂直 */;
        var expanded = ExpandContinuousGems(board, seeds, gemType);
        return new MatchData(expanded, MatchType.Cross, gemType);
    }

    // 检测T型 - 一个方向3+，垂直方向2+
    if (horizontal.Count >= 3 && vertical.Count >= 2) { /* ... */ }
    if (vertical.Count >= 3 && horizontal.Count >= 2) { /* ... */ }

    // 检测L型 - 4种配置
    if (right.Count >= 2 && up.Count >= 2) { /* 右上L */ }
    if (right.Count >= 2 && down.Count >= 2) { /* 右下L */ }
    if (left.Count >= 2 && up.Count >= 2) { /* 左上L */ }
    if (left.Count >= 2 && down.Count >= 2) { /* 左下L */ }

    return null;
}
```

**流程**：
1. 从中心点向4个方向探测连续宝石
2. 合并对向的线（如上+下 = 垂直，左+右 = 水平）
3. 根据两条线的长度判断形状类型
4. 使用种子进行连续扩展

**示例 - 扩展L型**：
```
初始检测：
R R R R  (right.Count = 4)
      R  (down.Count = 2)
      R

判定：right >= 2 && down >= 2 → L型
种子：{右侧4个R + 下方2个R（去重角点）} = 5个种子
扩展：BFS探索周围，发现没有更多连续R
结果：消除5个R ✅

扩展L型（更复杂情况）：
R R R R  (right.Count = 4)
      R  (down.Count = 3)
      R
      R

判定：right >= 2 && down >= 2 → L型
种子：6个R
扩展：BFS探索，可能发现更多连续R
结果：消除所有连续的R ✅
```

#### 4. 2x2方块检测（Propeller）

```csharp
private static MatchData TryFindSquare(Board board, int x, int y, GemType gemType, HashSet<Gem> processedGems)
{
    // 检查以(x,y)为左下角的2x2方块
    if (!board.IsValidPosition(x + 1, y) ||
        !board.IsValidPosition(x, y + 1) ||
        !board.IsValidPosition(x + 1, y + 1))
        return null;

    Gem gem1 = board.GetGem(x, y);
    Gem gem2 = board.GetGem(x + 1, y);
    Gem gem3 = board.GetGem(x, y + 1);
    Gem gem4 = board.GetGem(x + 1, y + 1);

    if (gem1 == null || gem2 == null || gem3 == null || gem4 == null)
        return null;

    if (gem1.Type != gemType || gem2.Type != gemType ||
        gem3.Type != gemType || gem4.Type != gemType)
        return null;

    // 找到2x2核心，进行连续扩展
    var seeds = new List<Gem> { gem1, gem2, gem3, gem4 };
    var expanded = ExpandContinuousGems(board, seeds, gemType);

    if (expanded.Any(g => processedGems.Contains(g)))
        return null;

    return new MatchData(expanded, MatchType.Square, gemType);
}
```

**流程**：
1. 检测以当前位置为左下角的2x2方块
2. 验证4个位置都是同色宝石
3. 使用4个宝石作为种子进行连续扩展
4. 支持"2x2+1"情况（扩展后可能是5个或更多宝石）

**示例 - 2x2+1情况**：
```
棋盘：
R R R
R R

2x2核心：左下4个R
种子：{4个R}
扩展：BFS发现右上角的R也连续
结果：消除5个R，仍判定为Propeller ✅
```

#### 5. Rocket和Basic检测

```csharp
// Rocket: 精确4连
private static MatchData TryFindRocket(...)
{
    var horizLine = CheckDirectionContinuous(board, x, y, 1, 0, gemType);
    if (horizLine.Count == 4)  // 精确等于4
    {
        var expanded = ExpandContinuousGems(board, horizLine, gemType);
        return new MatchData(expanded, MatchType.Horizontal4, gemType);
    }
    // 垂直同理
}

// Basic: 3+连
private static MatchData TryFindBasicMatch(...)
{
    var horizLine = CheckDirectionContinuous(board, x, y, 1, 0, gemType);
    if (horizLine.Count >= 3)  // 至少3个
    {
        var expanded = ExpandContinuousGems(board, horizLine, gemType);
        return new MatchData(expanded, MatchType.Horizontal3, gemType);
    }
    // 垂直同理
}
```

**注意**：
- Rocket检测要求精确4连（`Count == 4`），因为5+连应该被Light Ball优先识别
- Basic接受3+连（`Count >= 3`），因为在优先级系统中，如果没有被更高级的匹配识别，说明确实是基础3连

---

### 优先级系统

#### MatchData优先级计算

```csharp
private int CalculatePriority(MatchType type, int gemCount)
{
    // 基础优先级：宝石数量
    int priority = gemCount * 10;

    // Royal Match特殊形状优先级
    switch (type)
    {
        case MatchType.Horizontal5:
        case MatchType.Vertical5:
            priority += 1000; // Light Ball 最高优先级
            break;
        case MatchType.Cross:
            priority += 900; // 十字型TNT
            break;
        case MatchType.TShape:
            priority += 800; // T型TNT
            break;
        case MatchType.LShape:
            priority += 700; // L型TNT
            break;
        case MatchType.Square:
            priority += 600; // 2x2 Propeller
            break;
        case MatchType.Horizontal4:
        case MatchType.Vertical4:
            priority += 400; // Rocket
            break;
        default:
            break; // Basic 3-match 优先级最低
    }

    return priority;
}
```

#### MatchManager分数加成

```csharp
private int GetShapeBonus(MatchType matchType)
{
    switch (matchType)
    {
        case MatchType.Horizontal5:
        case MatchType.Vertical5:
            return 600; // Light Ball 最高奖励
        case MatchType.Cross:
            return 500; // 十字型TNT
        case MatchType.TShape:
            return 400; // T型TNT
        case MatchType.LShape:
            return 300; // L型TNT
        case MatchType.Square:
            return 250; // 2x2 Propeller
        case MatchType.Horizontal4:
        case MatchType.Vertical4:
            return 150; // Rocket
        default:
            return 0; // 普通3连无额外奖励
    }
}
```

---

## 三、技术细节

### 辅助函数

#### CheckDirectionContinuous
```csharp
/// <summary>
/// 沿指定方向检查连续的相同宝石（单向检测）
/// </summary>
private static List<Gem> CheckDirectionContinuous(Board board, int startX, int startY, int dirX, int dirY, GemType gemType)
{
    var result = new List<Gem>();
    int x = startX;
    int y = startY;

    while (board.IsValidPosition(x, y))
    {
        Gem gem = board.GetGem(x, y);
        if (gem == null || gem.Type != gemType)
            break;

        result.Add(gem);
        x += dirX;
        y += dirY;
    }

    return result;
}
```

**用途**：
- 从起点沿单一方向检测连续宝石
- 不进行扩展，只沿直线探测
- 用于判断是否满足某个形状的基本条件

#### MergeLines
```csharp
/// <summary>
/// 合并两条对向的线（去重中心点）
/// </summary>
private static List<Gem> MergeLines(List<Gem> line1, List<Gem> line2, Gem centerGem)
{
    var result = new HashSet<Gem>();

    // line1和line2都包含中心点，需要去重
    foreach (var gem in line1)
        result.Add(gem);
    foreach (var gem in line2)
        result.Add(gem);

    return result.ToList();
}
```

**用途**：
- 合并上+下 → 垂直线
- 合并左+右 → 水平线
- 使用HashSet自动去重中心点

#### MarkGemsAsProcessed
```csharp
/// <summary>
/// 标记宝石为已处理
/// </summary>
private static void MarkGemsAsProcessed(List<Gem> gems, HashSet<Gem> processedGems)
{
    foreach (var gem in gems)
    {
        processedGems.Add(gem);
    }
}
```

**用途**：
- 避免同一个宝石被多次检测
- 确保一个匹配只被识别一次

---

## 四、测试验证

### 编译结果

✅ **编译成功**
- 无编译错误
- 无警告（仅有游戏运行时的状态提示）
- Unity版本：2022.3.23f1

### 测试场景

#### 场景1：扩展L型
```
输入棋盘：
B B B B  (4个蓝色水平)
      B  (2个蓝色垂直)
      B

预期：
- 检测为L型
- 消除全部6个B
- 分数：6个宝石 * 100 + 3个额外宝石 * 50 + L型加成300 = 1050分

旧系统：只消除5个 ❌
新系统：消除6个 ✅
```

#### 场景2：2x2+1 Propeller
```
输入棋盘：
R R R
R R

预期：
- 检测为2x2 Propeller（支持+1）
- 消除全部5个R
- 分数：5个宝石 * 100 + 2个额外宝石 * 50 + Propeller加成250 = 850分

旧系统：不检测2x2 ❌
新系统：正确检测并扩展 ✅
```

#### 场景3：Light Ball优先
```
输入棋盘：
G G G G G  (5个绿色水平)

预期：
- 优先检测为Light Ball（5+连）
- 而非3个Basic + 2个额外
- 分数：5个宝石 * 100 + 2个额外宝石 * 50 + Light Ball加成600 = 1200分

旧系统：可能被识别为多个3连 ❌
新系统：优先识别为Light Ball ✅
```

#### 场景4：复杂T型扩展
```
输入棋盘：
  Y Y Y Y
Y Y Y
  Y

预期：
- 检测为T型
- 扩展到全部8个Y
- 分数：8个宝石 * 100 + 5个额外宝石 * 50 + T型加成400 = 1450分

旧系统：只消除5个 ❌
新系统：消除8个 ✅
```

---

## 五、Royal Match规则对照表（最终版本）

| 匹配类型 | 最小宝石数 | 形状要求 | ~~连续扩展~~ | 优先级 | 分数加成 | 实现状态 |
|---------|-----------|---------|---------|--------|---------|---------|
| **Light Ball** | 5 | 直线5+连 | ❌ 不扩展 | 1000 | +600 | ✅ |
| **T-Shape TNT** | 5 | 主线3+支线2+<br/>或十字型（水平3+垂直3+） | ❌ 不扩展 | 800 | +400 | ✅ |
| **L-Shape TNT** | 5 | 两臂各3+ | ❌ 不扩展 | 700 | +300 | ✅ |
| **SquarePlus** | 5 | 2x2+周围1个 | ❌ 不扩展 | 650 | +280 | ✅ |
| **Propeller** | 4 | 2x2方块 | ❌ 不扩展 | 600 | +250 | ✅ |
| **Rocket** | 4 | 直线4连 | ❌ 不扩展 | 400 | +150 | ✅ |
| **Basic 3** | 3 | 直线3连 | ❌ 不扩展 | 0-30 | +0 | ✅ |

**核心规则（最终版本）**：
1. ❌ ~~**连续判定**~~：已废弃，不再使用`ExpandContinuousGems`扩展
2. ✅ **精确形状匹配**：每个匹配类型只消除匹配到的具体宝石
3. ✅ **优先级判定**：Light Ball > T型TNT > L型TNT > SquarePlus > Propeller > Rocket > Basic 3
4. ✅ **明确2x2+1**：新增SquarePlus类型，不依赖扩展
5. ✅ **移除Cross**：十字型归类为T型TNT

---

## 六、性能考虑

### 时间复杂度

#### 旧系统
- 线性匹配：O(W * H)
- L/T检测：O(W * H)
- 总计：O(W * H)

#### 新系统
- 每个位置最多检测一次：O(W * H)
- BFS扩展最坏情况：O(W * H)（整个棋盘同色）
- 总计：O(W * H) 单次扫描 + O(匹配数 * 平均匹配大小) BFS

**实际性能**：
- 8x8棋盘：64个位置
- 典型匹配：3-10个宝石
- 预期：每帧 < 1ms

### 空间复杂度

- `processedGems` HashSet：O(W * H) 最坏情况
- BFS Queue：O(平均匹配大小) ≈ O(1)
- 总计：O(W * H)

### 优化措施

1. **避免重复检测**：使用`processedGems`标记
2. **HashSet去重**：O(1)查找和插入
3. **提前返回**：检测到匹配立即标记并跳过
4. **单次遍历**：按优先级顺序，一次扫描完成

---

## 七、向后兼容

### 保留的旧API

```csharp
// 旧的FindMatches API仍然可用
public static List<List<Gem>> FindMatches(Board board)
{
    var enhancedMatches = FindMatchesEnhanced(board);
    return enhancedMatches.Select(m => m.Gems).ToList();
}
```

**兼容性**：
- ✅ `FindMatches()` 仍然可用，内部调用`FindMatchesEnhanced()`
- ✅ `MatchManager`同时触发新旧事件
- ✅ `DebugPrintMatches()` 和 `DebugPrintMatchesEnhanced()` 都可用

---

## 八、文件清单

| 文件 | 修改类型 | 说明 |
|-----|---------|------|
| `Assets/Scripts/Core/MatchDetector.cs` | 完全重写 | 实现Royal Match规则匹配系统 |
| `Assets/Scripts/Managers/MatchManager.cs` | 修改 | 添加Square类型分数加成，调整优先级 |
| `Documentation/M2-Royal-Match-Rules.md` | 新增 | 本文档 |

---

## 九、总结

### 当前最终实现（2026-01-18更新）

✅ **精确形状匹配**：废弃连续扩展算法，每个匹配类型都有明确定义
✅ **L型/T型检测**：只消除匹配到的形状，不扩展（如L型消除5-6个匹配到的宝石）
✅ **移除Cross类型**：十字型归类为T型TNT
✅ **新增SquarePlus类型**：明确检测2x2+1（5个宝石）
✅ **3阶段检测策略**：收集所有候选 → 排序 → 贪心选择，避免低优先级匹配阻挡高优先级匹配
✅ **优先级系统**：Light Ball > T型TNT > L型TNT > SquarePlus > Square > Rocket > Basic 3
✅ **分数系统**：SquarePlus (+280), Square (+250), 其他类型分数加成保持不变

### 关键改进（最终版本）

1. **~~算法核心~~**：~~`ExpandContinuousGems` BFS算法~~（已废弃） → **精确形状定义**
2. **检测策略**：3阶段检测（收集→排序→选择），避免优先级冲突
3. **形状类型**：移除Cross，新增SquarePlus，共7种匹配类型
4. **消除范围**：只消除匹配到的宝石，不扩展到其他连续同色宝石
5. **向后兼容**：保留旧API，内部使用新系统

### 文档状态

⚠️ **本文档的第二至第七部分**包含大量关于 `ExpandContinuousGems` 扩展算法的详细实现，这些内容已**过时**。

✅ **请参考**：
- `Documentation/MatchRule.md` - 当前准确的匹配规则文档
- `Assets/Scripts/Core/MatchDetector.cs` - 当前实际代码实现

### 下一步工作

1. ✅ **匹配系统完成**：所有匹配类型已实现并测试
2. **特殊道具效果**：实现Light Ball、TNT、Rocket、Propeller的特殊消除效果
3. **连锁反应**：优化连续消除的动画表现
4. **性能优化**：如需要，可以添加性能监控

---

**版本**: M2 (2026-01-18)
**作者**: Claude AI + lxmxhh
