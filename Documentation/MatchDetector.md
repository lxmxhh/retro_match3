# 匹配检测系统 (Match Detection System)

## 概述

`MatchDetector` 是一个完整的匹配检测系统，用于在消消乐游戏中检测棋盘上的匹配宝石（3个或更多相同类型的宝石连成一线）。

## 核心特性

- ✓ **静态API设计**：无需实例化，直接调用静态方法
- ✓ **水平和垂直检测**：支持双方向匹配检测
- ✓ **智能去重**：自动移除重复匹配，优先保留更长的匹配
- ✓ **空值处理**：自动跳过空宝石位置
- ✓ **完整的辅助方法**：提供多种便利方法进行查询
- ✓ **调试支持**：内置调试日志打印功能

## 主要方法

### 1. FindMatches(Board board) - 查找所有匹配
```csharp
// 查找棋盘上的所有匹配
List<List<Gem>> matches = MatchDetector.FindMatches(board);

// 结果说明：
// - 返回一个包含多个匹配组的列表
// - 每个匹配组是一个 List<Gem>，包含3个或更多相同类型的宝石
// - 返回的匹配已自动去重，不存在重复的宝石
```

### 2. HasMatches(Board board) - 检查是否有匹配
```csharp
// 快速检查是否存在匹配
if (MatchDetector.HasMatches(board))
{
    Debug.Log("存在匹配的宝石");
}
```

### 3. GetTotalMatchCount(Board board) - 获取总匹配数
```csharp
// 获取所有匹配宝石的总数
int totalMatchedGems = MatchDetector.GetTotalMatchCount(board);
Debug.Log($"总共有 {totalMatchedGems} 个宝石被匹配");
```

### 4. IsGemMatched(Board board, int x, int y) - 检查特定宝石是否被匹配
```csharp
// 检查位置 (3, 4) 的宝石是否被匹配
bool isMatched = MatchDetector.IsGemMatched(board, 3, 4);
if (isMatched)
{
    Debug.Log("该宝石将被消除");
}
```

### 5. DebugPrintMatches(List<List<Gem>> matches) - 打印匹配信息
```csharp
// 打印所有匹配的详细信息（调试用）
var matches = MatchDetector.FindMatches(board);
MatchDetector.DebugPrintMatches(matches);

// 输出示例：
// MatchDetector: Found 2 match groups:
//   Match 1: 3 gems of type Red at [(1,2), (2,2), (3,2)]
//   Match 2: 4 gems of type Blue at [(2,0), (2,1), (2,2), (2,3)]
```

## 内部方法（私有）

### FindHorizontalMatches(Board board)
- 逐行扫描棋盘
- 查找连续的相同类型宝石
- 返回所有水平匹配组

### FindVerticalMatches(Board board)
- 逐列扫描棋盘
- 查找连续的相同类型宝石
- 返回所有垂直匹配组

### CheckDirection(Board board, int startX, int startY, int dirX, int dirY)
- 从指定位置沿指定方向检查
- 参数：
  - `dirX, dirY`：方向向量
  - 水平向右：`(1, 0)`
  - 垂直向上：`(0, 1)`
- 返回所有连续的相同类型宝石

### RemoveDuplicateMatches(List<List<Gem>> matches)
- 移除包含重复宝石的匹配
- 优先保留更长的匹配组
- 确保每个宝石最多只在一个匹配组中

## 使用场景

### 场景1：游戏每帧检查匹配
```csharp
public class GameManager : MonoBehaviour
{
    private Board board;

    private void Update()
    {
        // 检查是否有新的匹配
        if (MatchDetector.HasMatches(board))
        {
            var matches = MatchDetector.FindMatches(board);
            HandleMatches(matches);
        }
    }

    private void HandleMatches(List<List<Gem>> matches)
    {
        foreach (var match in matches)
        {
            // 处理每个匹配组
            foreach (var gem in match)
            {
                // 销毁宝石、添加分数等
            }
        }
    }
}
```

### 场景2：玩家交换后检查匹配
```csharp
public void OnSwapGems(Gem gem1, Gem gem2)
{
    // 交换宝石
    SwapGemsOnBoard(gem1, gem2);

    // 检查是否产生匹配
    if (MatchDetector.HasMatches(board))
    {
        var matches = MatchDetector.FindMatches(board);
        // 处理匹配，如消除宝石、添加分数等
    }
    else
    {
        // 撤销交换
        SwapGemsOnBoard(gem1, gem2);
        Debug.Log("无法产生匹配，交换撤销");
    }
}
```

### 场景3：计算分数
```csharp
public int CalculateScore()
{
    var matches = MatchDetector.FindMatches(board);
    int score = 0;

    foreach (var match in matches)
    {
        // 3个宝石：100分
        // 4个宝石：200分
        // 5个宝石：500分
        int baseScore = 100;
        int bonus = (match.Count - 3) * 100;
        score += baseScore + bonus;
    }

    return score;
}
```

### 场景4：检测个别宝石
```csharp
public void HighlightMatchedGems()
{
    Gem[,] allGems = board.GetAllGems();

    for (int x = 0; x < board.Width; x++)
    {
        for (int y = 0; y < board.Height; y++)
        {
            if (MatchDetector.IsGemMatched(board, x, y))
            {
                // 高亮显示该宝石
                HighlightGem(allGems[x, y]);
            }
        }
    }
}
```

## 算法说明

### 匹配检测流程

1. **初始化**
   - 检查棋盘是否为null
   - 创建结果列表

2. **水平扫描**
   - 从上到下逐行扫描
   - 从左到右检查每个宝石
   - 跳过已检查的匹配组

3. **垂直扫描**
   - 从左到右逐列扫描
   - 从下到上检查每个宝石
   - 跳过已检查的匹配组

4. **去重处理**
   - 按匹配长度从长到短排序
   - 保留最长的匹配
   - 移除包含重复宝石的匹配

### 性能特性

- **时间复杂度**：O(W × H)，其中W是宽度，H是高度
- **空间复杂度**：O(M)，其中M是匹配宝石的总数
- **最坏情况**：整个棋盘都是匹配宝石（O(W × H)）
- **最优情况**：没有匹配宝石（O(W × H)）

## 常数配置

```csharp
// 最小匹配数
private const int MINIMUM_MATCH_LENGTH = 3;
```

如需修改最小匹配数，只需更改该常数即可。

## 测试

项目包含 `MatchDetectorTest.cs` 测试脚本，可进行以下测试：

1. **基本匹配查找** - 验证FindMatches方法
2. **存在性检查** - 验证HasMatches方法
3. **总数统计** - 验证GetTotalMatchCount方法
4. **单个宝石检查** - 验证IsGemMatched方法

### 运行测试

```csharp
// 方式1：在编辑器中为MatchDetectorTest分配Board，勾选runTestOnStart
// 游戏启动时自动运行测试

// 方式2：直接调用
MatchDetectorTest test = GetComponent<MatchDetectorTest>();
test.RunTests();

// 方式3：通过编辑器按钮调用
// 在Inspector中找到MatchDetectorTest，点击OnTestButtonClick按钮
```

## 注意事项

1. **空宝石处理**
   - 自动跳过null宝石，不会产生异常
   - 空宝石会中断匹配序列

2. **宝石类型比较**
   - 使用 `GemType` 枚举比较
   - 确保所有宝石都有有效的类型

3. **网格位置**
   - 使用 `GridPosition` 属性存储宝石的棋盘坐标
   - 坐标范围：x [0, Width-1]，y [0, Height-1]

4. **性能优化建议**
   - 避免在每帧都调用FindMatches（仅在必要时调用）
   - 使用HasMatches进行快速检查
   - 对于大型棋盘，可考虑缓存结果

## 扩展可能性

### 1. 支持L形匹配
```csharp
private static List<List<Gem>> FindLShapeMatches(Board board)
{
    // 检测L形和T形匹配
}
```

### 2. 支持特殊宝石
```csharp
public class SpecialGem : Gem
{
    // 炸弹、骨牌等特殊宝石
}
```

### 3. 支持组合匹配
```csharp
public static int GetComboMultiplier(Board board)
{
    // 计算连续匹配的组合倍数
}
```

### 4. 预测匹配
```csharp
public static List<List<Gem>> PredictMatches(Board board, Gem gem1, Gem gem2)
{
    // 交换前预测会产生的匹配
}
```

## 调试技巧

### 打印所有匹配
```csharp
var matches = MatchDetector.FindMatches(board);
MatchDetector.DebugPrintMatches(matches);
```

### 在Scene视图中高亮匹配
```csharp
var matches = MatchDetector.FindMatches(board);
foreach (var match in matches)
{
    foreach (var gem in match)
    {
        // 绘制调试线框
        Debug.DrawLine(gem.GetWorldPosition(), gem.GetWorldPosition() + Vector3.up, Color.red, 1f);
    }
}
```

### 记录详细日志
```csharp
Debug.Log($"Total matches: {matches.Count}");
Debug.Log($"Total matched gems: {MatchDetector.GetTotalMatchCount(board)}");
for (int i = 0; i < matches.Count; i++)
{
    Debug.Log($"Match {i}: {matches[i].Count} gems of type {matches[i][0].Type}");
}
```

## 许可证

该代码是Retro Match 2D游戏项目的一部分。

---

**最后更新**：2026年1月15日
**作者**：Claude Code
**版本**：1.0
