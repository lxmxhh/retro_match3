# M1增强匹配系统文档

## 概述

M1里程碑中实现了增强的匹配检测系统，支持更多种类的匹配模式，包括L型、T型、十字型以及4连、5连的识别。系统还引入了匹配优先级机制，确保更有价值的匹配被优先处理。

## 新增匹配类型

### 1. 基础线性匹配

| 类型 | 描述 | 优先级加成 |
|------|------|-----------|
| **Horizontal3** | 水平3连 | +30 (基础) |
| **Vertical3** | 垂直3连 | +30 (基础) |
| **Horizontal4** | 水平4连 | +30 + 40 (宝石数) |
| **Vertical4** | 垂直4连 | +30 + 40 (宝石数) |
| **Horizontal5** | 水平5连或更多 | +50 + 50+ (宝石数) |
| **Vertical5** | 垂直5连或更多 | +50 + 50+ (宝石数) |

### 2. L型匹配 (LShape)

**定义**: 一个3连在水平/垂直方向，在其端点有另一个2连在垂直/水平方向

**配置示例**:
```
┘ 形态: 水平右3连 + 垂直上2连
┐ 形态: 水平右3连 + 垂直下2连
└ 形态: 水平左3连 + 垂直上2连
┌ 形态: 水平左3连 + 垂直下2连
```

**优先级**: 宝石数 × 10 + 60

**最小宝石数**: 5个 (3+2配置)

### 3. T型匹配 (TShape)

**定义**: 一个3连在一个方向（主线），中间点有另一个2连在垂直方向（支线）

**配置示例**:
```
⊥ 形态: 水平主线 + 垂直向上支线
⊤ 形态: 水平主线 + 垂直向下支线
⊢ 形态: 垂直主线 + 水平向右支线
⊣ 形态: 垂直主线 + 水平向左支线
```

**优先级**: 宝石数 × 10 + 80

**最小宝石数**: 5个 (3+2配置)

### 4. 十字型匹配 (Cross)

**定义**: 水平和垂直都是3连，在中心点交叉

**形态**:
```
  ╋
```

**优先级**: 宝石数 × 10 + 100 (最高优先级)

**最小宝石数**: 5个 (水平3 + 垂直3 - 中心点1)

## 核心数据结构

### MatchType 枚举

```csharp
public enum MatchType
{
    None = 0,           // 无匹配
    Horizontal3 = 1,    // 水平3连
    Vertical3 = 2,      // 垂直3连
    Horizontal4 = 3,    // 水平4连
    Vertical4 = 4,      // 垂直4连
    Horizontal5 = 5,    // 水平5连或更多
    Vertical5 = 6,      // 垂直5连或更多
    LShape = 7,         // L型（3+2）
    TShape = 8,         // T型（3+2+2或交叉）
    Cross = 9           // 十字型（垂直3+水平3交叉）
}
```

### MatchData 类

```csharp
public class MatchData
{
    public List<Gem> Gems { get; set; }      // 匹配的宝石列表
    public MatchType Type { get; set; }       // 匹配类型
    public int Priority { get; set; }         // 匹配优先级
    public GemType GemType { get; set; }      // 匹配的宝石类型
}
```

## API使用方法

### 基础用法

```csharp
using RetroMatch2D.Core;

// 使用增强匹配检测
List<MatchData> matches = MatchDetector.FindMatchesEnhanced(board);

// 遍历匹配结果
foreach (var match in matches)
{
    Debug.Log($"匹配类型: {match.Type}");
    Debug.Log($"优先级: {match.Priority}");
    Debug.Log($"宝石数量: {match.Gems.Count}");
    Debug.Log($"宝石类型: {match.GemType}");
}
```

### 调试输出

```csharp
// 打印详细匹配信息
MatchDetector.DebugPrintMatchesEnhanced(matches);

// 输出示例：
// Match 1: Type=Cross, Priority=160, Gems=5, GemType=Red, Positions=[(2,1), (2,2), (2,3), (1,2), (3,2)]
// Match 2: Type=LShape, Priority=110, Gems=5, GemType=Blue, Positions=[(4,4), (5,4), (6,4), (4,5), (4,6)]
```

### 根据匹配类型生成特殊宝石

```csharp
foreach (var match in matches)
{
    switch (match.Type)
    {
        case MatchType.Horizontal4:
        case MatchType.Vertical4:
            // 生成炸弹宝石
            CreateBombGem(match.Gems[0].GridPosition);
            break;

        case MatchType.Horizontal5:
        case MatchType.Vertical5:
            // 生成彩虹宝石
            CreateRainbowGem(match.Gems[0].GridPosition);
            break;

        case MatchType.LShape:
            // 生成L型特殊宝石
            CreateLShapeGem(match.Gems[0].GridPosition);
            break;

        case MatchType.TShape:
            // 生成T型特殊宝石
            CreateTShapeGem(match.Gems[0].GridPosition);
            break;

        case MatchType.Cross:
            // 生成十字炸弹
            CreateCrossBombGem(match.Gems[0].GridPosition);
            break;

        default:
            // 普通3连
            break;
    }
}
```

## 优先级系统

### 优先级计算公式

```
Priority = (宝石数量 × 10) + 形状加成
```

### 形状加成表

| 匹配类型 | 形状加成 |
|---------|---------|
| Cross (十字型) | +100 |
| TShape (T型) | +80 |
| LShape (L型) | +60 |
| 5连 | +50 |
| 4连 | +30 |
| 3连 | +0 |

### 优先级排序

匹配结果会自动按优先级从高到低排序：

```csharp
// 内部自动排序
allMatches = allMatches.OrderByDescending(m => m.Priority).ToList();
```

**示例**:
- 十字型 (5宝石): Priority = 50 + 100 = **150**
- T型 (5宝石): Priority = 50 + 80 = **130**
- L型 (5宝石): Priority = 50 + 60 = **110**
- 5连: Priority = 50 + 50 = **100**
- 4连: Priority = 40 + 30 = **70**
- 3连: Priority = 30 + 0 = **30**

## 去重机制

系统会自动去除重复的宝石匹配：

1. 按优先级从高到低排序
2. 优先保留高优先级的匹配
3. 已使用的宝石不会出现在后续匹配中

```csharp
// 自动去重
allMatches = RemoveDuplicateMatchData(allMatches);
```

## 向后兼容性

为了保持向后兼容，原有的 `FindMatches()` 方法仍然保留：

```csharp
// 旧版API - 仍然可用
List<List<Gem>> basicMatches = MatchDetector.FindMatches(board);

// 新版API - 增强功能
List<MatchData> enhancedMatches = MatchDetector.FindMatchesEnhanced(board);
```

## 性能考虑

### 时间复杂度

| 操作 | 复杂度 | 说明 |
|------|--------|------|
| 线性匹配检测 | O(W×H) | 扫描整个棋盘 |
| L型匹配检测 | O(W×H×C) | C为每个点的检查方向数(4) |
| T型匹配检测 | O(W×H×D) | D为每个点的T型检查数(2) |
| 去重排序 | O(M log M) | M为匹配数量 |
| **总体** | **O(W×H)** | 线性时间复杂度 |

### 优化建议

1. **缓存匹配结果**: 如果棋盘状态未改变，不需要重新检测
2. **延迟检测**: 只在需要时调用增强匹配（如特殊宝石生成）
3. **增量检测**: 只检测交换附近的区域（未来优化）

## 使用场景

### 1. 基础匹配消除

使用原有的 `FindMatches()` 即可：

```csharp
var matches = MatchDetector.FindMatches(board);
```

### 2. 特殊宝石生成

使用 `FindMatchesEnhanced()` 识别特殊形状：

```csharp
var matches = MatchDetector.FindMatchesEnhanced(board);
foreach (var match in matches)
{
    if (match.Type == MatchType.LShape)
    {
        GenerateSpecialGem(match);
    }
}
```

### 3. 分数计算

使用优先级加权分数：

```csharp
int totalScore = 0;
foreach (var match in matches)
{
    int baseScore = match.Gems.Count * 100;
    int bonusScore = match.Priority;
    totalScore += baseScore + bonusScore;
}
```

## 测试建议

### 创建测试场景

建议创建以下测试用例：

1. **L型测试**: 手动放置3+2宝石形成L型
2. **T型测试**: 放置3+2+2宝石形成T型
3. **十字型测试**: 放置水平3+垂直3形成十字
4. **4连测试**: 放置4个连续宝石
5. **5连测试**: 放置5个连续宝石
6. **复杂组合**: 同时存在多种匹配类型

### 调试工具

```csharp
[MenuItem("Tools/Test Enhanced Matching")]
public static void TestEnhancedMatching()
{
    Board board = FindObjectOfType<Board>();
    var matches = MatchDetector.FindMatchesEnhanced(board);
    MatchDetector.DebugPrintMatchesEnhanced(matches);
}
```

## 未来扩展

### 计划中的功能

1. **匹配预测**: 提前计算可能的匹配组合
2. **连锁检测**: 检测消除后可能产生的连锁反应
3. **最佳匹配推荐**: AI提示系统，推荐最优匹配
4. **自定义形状**: 支持更多自定义匹配形状

### 扩展示例

```csharp
// 未来API示例
public static List<MatchData> PredictMatchesAfterSwap(Board board, Vector2Int pos1, Vector2Int pos2);
public static List<MatchData> FindChainMatches(Board board, int maxDepth);
public static MatchData GetBestMatch(List<MatchData> matches, MatchStrategy strategy);
```

## 总结

M1增强匹配系统提供了：

✅ **9种匹配类型** - 从基础3连到复杂十字型
✅ **智能优先级系统** - 自动识别最有价值的匹配
✅ **自动去重机制** - 避免宝石重复消除
✅ **向后兼容** - 保留旧API，平滑迁移
✅ **性能优化** - 线性时间复杂度
✅ **易于扩展** - 清晰的架构设计

**文件位置**: `Assets/Scripts/Core/MatchDetector.cs`

**版本**: M1 (2026-01-17)

**作者**: Claude AI + lxmxhh
