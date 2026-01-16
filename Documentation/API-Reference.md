# API参考与使用示例

本文档提供MatchDetector API的快速参考和实用代码示例。

## 📖 目录

- [核心API](#核心api)
- [快速示例](#快速示例)
- [完整游戏循环](#完整游戏循环)
- [高级功能](#高级功能)
- [性能优化](#性能优化)
- [调试技巧](#调试技巧)
- [最佳实践](#最佳实践)

---

## 核心API

### FindMatches(Board board)
查找棋盘上的所有匹配项。

```csharp
List<List<Gem>> matches = MatchDetector.FindMatches(board);

// 返回：List<List<Gem>>
// - 匹配组列表，每组至少3个宝石
// - 已自动去重，无重复宝石
```

### HasMatches(Board board)
快速检查是否存在匹配。

```csharp
if (MatchDetector.HasMatches(board))
{
    // 存在匹配，处理逻辑
}

// 返回：bool
// - true: 存在匹配
// - false: 无匹配
```

### GetTotalMatchCount(Board board)
获取所有匹配宝石的总数。

```csharp
int count = MatchDetector.GetTotalMatchCount(board);
Debug.Log($"总共 {count} 个宝石被匹配");

// 返回：int
// - 所有匹配组中宝石的总数
```

### IsGemMatched(Board board, int x, int y)
检查特定位置的宝石是否被匹配。

```csharp
if (MatchDetector.IsGemMatched(board, 3, 4))
{
    Debug.Log("位置(3,4)的宝石将被消除");
}

// 返回：bool
// - true: 该位置宝石在匹配中
// - false: 该位置无匹配
```

### DebugPrintMatches(List<List<Gem>> matches)
打印匹配信息（调试用）。

```csharp
var matches = MatchDetector.FindMatches(board);
MatchDetector.DebugPrintMatches(matches);

// 输出示例：
// MatchDetector: Found 2 match groups:
//   Match 1: 3 gems of type Red at [(1,2), (2,2), (3,2)]
//   Match 2: 4 gems of type Blue at [(2,0), (2,1), (2,2), (2,3)]
```

---

## 快速示例

### 1. 基本匹配检测

```csharp
using RetroMatch2D.Core;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public Board board;

    void Update()
    {
        if (MatchDetector.HasMatches(board))
        {
            var matches = MatchDetector.FindMatches(board);

            foreach (var match in matches)
            {
                Debug.Log($"找到匹配：{match.Count} 个宝石");
                foreach (var gem in match)
                {
                    Destroy(gem.gameObject);
                }
            }
        }
    }
}
```

### 2. 验证交换

```csharp
// 交换宝石
SwapGemsOnBoard(gem1, gem2);

// 检查是否产生匹配
if (!MatchDetector.HasMatches(board))
{
    // 无效交换，撤销
    SwapGemsOnBoard(gem1, gem2);
    Debug.Log("无法产生匹配");
}
```

### 3. 计算分数

```csharp
var matches = MatchDetector.FindMatches(board);
int score = 0;

foreach (var match in matches)
{
    // 基础分：100分/宝石
    // 奖励分：超过3个的部分 +50分/宝石
    int baseScore = match.Count * 100;
    int bonus = (match.Count - 3) * 50;
    score += baseScore + bonus;
}

Debug.Log($"获得分数：{score}");
```

### 4. 级联消除

```csharp
do
{
    // 1. 消除匹配的宝石
    ClearMatchedGems();

    // 2. 应用重力下落
    ApplyGravity();

    // 3. 填充空位
    FillEmptySpaces();

    // 4. 检查是否产生新匹配
} while (MatchDetector.HasMatches(board));
```

---

## 完整游戏循环

### 玩家交换宝石

```csharp
public class SwapHandler : MonoBehaviour
{
    public Board board;
    public MatchManager matchManager;

    public void OnSwapGems(Gem gem1, Gem gem2)
    {
        // 验证交换是否有效
        if (!matchManager.ValidateSwap(gem1, gem2))
        {
            Debug.Log("无效交换 - 不产生匹配");
            return;
        }

        // 执行交换
        PerformSwap(gem1, gem2);

        // 检查并处理匹配
        matchManager.CheckAndHandleMatches();
    }

    private void PerformSwap(Gem gem1, Gem gem2)
    {
        Vector2Int pos1 = gem1.GridPosition;
        Vector2Int pos2 = gem2.GridPosition;

        board.SetGem(pos1.x, pos1.y, gem2);
        board.SetGem(pos2.x, pos2.y, gem1);

        gem1.SetGridPosition(pos2);
        gem2.SetGridPosition(pos1);
    }
}
```

### 消除和填充

```csharp
public class GemCascadeHandler : MonoBehaviour
{
    public Board board;
    public MatchManager matchManager;
    public float fallDuration = 0.3f;

    public void HandleCascade()
    {
        // 1. 消除匹配的宝石
        var matchedGems = matchManager.GetAllMatchedGems();
        foreach (var gem in matchedGems)
        {
            if (gem != null)
            {
                board.SetGem(gem.GridPosition.x, gem.GridPosition.y, null);
                Destroy(gem.gameObject);
            }
        }

        // 2. 应用重力
        ApplyGravity();

        // 3. 填充空位
        FillEmptySpaces();

        // 4. 检查级联
        if (matchManager.CheckAndHandleMatches())
        {
            StartCoroutine(CascadeCoroutine());
        }
    }

    private void ApplyGravity()
    {
        for (int x = 0; x < board.Width; x++)
        {
            int writeIndex = 0;
            for (int y = 0; y < board.Height; y++)
            {
                Gem gem = board.GetGem(x, y);
                if (gem != null)
                {
                    if (writeIndex != y)
                    {
                        board.SetGem(x, writeIndex, gem);
                        board.SetGem(x, y, null);
                        gem.SetGridPosition(new Vector2Int(x, writeIndex));
                    }
                    writeIndex++;
                }
            }
        }
    }

    private void FillEmptySpaces()
    {
        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                if (board.GetGem(x, y) == null)
                {
                    var newGem = CreateRandomGem(x, y);
                    board.SetGem(x, y, newGem);
                }
            }
        }
    }

    private IEnumerator CascadeCoroutine()
    {
        yield return new WaitForSeconds(fallDuration);
        HandleCascade();
    }
}
```

### 分数系统

```csharp
public class ScoreDisplay : MonoBehaviour
{
    public UnityEngine.UI.Text scoreText;
    public MatchManager matchManager;

    private void Start()
    {
        matchManager.OnScoreEarned += OnScoreEarned;
        UpdateScoreDisplay();
    }

    private void OnScoreEarned(int earnedScore)
    {
        UpdateScoreDisplay();
        Debug.Log($"获得 {earnedScore} 分！总分：{matchManager.CurrentScore}");
    }

    private void UpdateScoreDisplay()
    {
        scoreText.text = $"Score: {matchManager.CurrentScore}";
    }

    private void OnDestroy()
    {
        if (matchManager != null)
        {
            matchManager.OnScoreEarned -= OnScoreEarned;
        }
    }
}
```

---

## 高级功能

### 特殊宝石生成

```csharp
public class SpecialGemHandler
{
    public static void HandleSpecialGems(Board board, List<List<Gem>> matches)
    {
        foreach (var match in matches)
        {
            if (match.Count == 4)
            {
                // 4个匹配 → 炸弹宝石
                CreateBomb(match[0]);
            }
            else if (match.Count >= 5)
            {
                // 5个或更多 → 彩虹宝石
                CreateRainbowGem(match[0]);
            }
        }
    }

    private static void CreateBomb(Gem center)
    {
        Debug.Log($"炸弹创建于 {center.GridPosition}");
        // 实现炸弹效果
    }

    private static void CreateRainbowGem(Gem center)
    {
        Debug.Log($"彩虹宝石创建于 {center.GridPosition}");
        // 实现彩虹宝石
    }
}
```

### AI提示系统

```csharp
public class HintSystem : MonoBehaviour
{
    public Board board;

    /// <summary>
    /// 找到可以产生匹配的交换
    /// </summary>
    public (Gem, Gem) GetHint()
    {
        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                Gem gem = board.GetGem(x, y);
                if (gem == null) continue;

                // 检查四个方向
                Vector2Int[] directions = {
                    Vector2Int.right,
                    Vector2Int.up,
                    Vector2Int.left,
                    Vector2Int.down
                };

                foreach (var dir in directions)
                {
                    int nx = x + dir.x;
                    int ny = y + dir.y;

                    if (board.IsValidPosition(nx, ny))
                    {
                        Gem neighbor = board.GetGem(nx, ny);
                        if (neighbor != null && TrySwapAndCheck(gem, neighbor))
                        {
                            return (gem, neighbor);
                        }
                    }
                }
            }
        }

        return (null, null); // 无可用交换
    }

    private bool TrySwapAndCheck(Gem gem1, Gem gem2)
    {
        var pos1 = gem1.GridPosition;
        var pos2 = gem2.GridPosition;

        // 虚拟交换
        board.SetGem(pos1.x, pos1.y, gem2);
        board.SetGem(pos2.x, pos2.y, gem1);
        gem1.SetGridPosition(pos2);
        gem2.SetGridPosition(pos1);

        bool hasMatch = MatchDetector.HasMatches(board);

        // 撤销交换
        board.SetGem(pos1.x, pos1.y, gem1);
        board.SetGem(pos2.x, pos2.y, gem2);
        gem1.SetGridPosition(pos1);
        gem2.SetGridPosition(pos2);

        return hasMatch;
    }
}
```

### 死局检测

```csharp
public static bool IsGameDeadlock(Board board)
{
    // 检查是否无可用交换（死局）
    for (int x = 0; x < board.Width; x++)
    {
        for (int y = 0; y < board.Height; y++)
        {
            Gem gem = board.GetGem(x, y);
            if (gem == null) continue;

            if (CanSwapAndMatch(board, gem, Vector2Int.right) ||
                CanSwapAndMatch(board, gem, Vector2Int.up))
            {
                return false; // 有可用交换
            }
        }
    }

    return true; // 死局
}

private static bool CanSwapAndMatch(Board board, Gem gem, Vector2Int direction)
{
    int nx = gem.GridPosition.x + direction.x;
    int ny = gem.GridPosition.y + direction.y;

    if (!board.IsValidPosition(nx, ny))
        return false;

    Gem neighbor = board.GetGem(nx, ny);
    if (neighbor == null)
        return false;

    // 临时交换并检查
    var pos1 = gem.GridPosition;
    var pos2 = neighbor.GridPosition;

    board.SetGem(pos1.x, pos1.y, neighbor);
    board.SetGem(pos2.x, pos2.y, gem);
    gem.SetGridPosition(pos2);
    neighbor.SetGridPosition(pos1);

    bool hasMatch = MatchDetector.HasMatches(board);

    // 撤销
    board.SetGem(pos1.x, pos1.y, gem);
    board.SetGem(pos2.x, pos2.y, neighbor);
    gem.SetGridPosition(pos1);
    neighbor.SetGridPosition(pos2);

    return hasMatch;
}
```

---

## 性能优化

### 缓存匹配结果

```csharp
public class OptimizedMatchDetector
{
    private Board board;
    private List<List<Gem>> cachedMatches;
    private bool isDirty = true;

    public void InvalidateCache()
    {
        isDirty = true;
    }

    public List<List<Gem>> FindMatches()
    {
        if (isDirty)
        {
            cachedMatches = MatchDetector.FindMatches(board);
            isDirty = false;
        }
        return cachedMatches;
    }

    public bool HasMatches()
    {
        return FindMatches().Count > 0;
    }
}
```

---

## 调试技巧

### 可视化匹配

```csharp
public class MatchVisualizer : MonoBehaviour
{
    public Board board;
    public Color matchHighlightColor = Color.yellow;

    private void OnDrawGizmosSelected()
    {
        if (board == null) return;

        var matches = MatchDetector.FindMatches(board);
        Gizmos.color = matchHighlightColor;

        foreach (var match in matches)
        {
            foreach (var gem in match)
            {
                Gizmos.DrawWireCube(gem.transform.position, Vector3.one * 0.8f);
            }
        }
    }
}
```

### 调试快捷键

```csharp
public class DebugConsole : MonoBehaviour
{
    public Board board;
    public MatchManager matchManager;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            // M键：打印匹配信息
            matchManager.DebugPrintMatches();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            // C键：清除匹配
            matchManager.ClearMatchedGems();
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            // H键：显示提示
            var hint = GetComponent<HintSystem>();
            var (gem1, gem2) = hint.GetHint();
            if (gem1 != null)
            {
                Debug.Log($"提示：交换 {gem1.GridPosition} 和 {gem2.GridPosition}");
            }
            else
            {
                Debug.Log("无可用交换！");
            }
        }
    }
}
```

---

## 最佳实践

### 1. 避免频繁调用FindMatches
```csharp
// ❌ 不好
void Update()
{
    var matches = MatchDetector.FindMatches(board); // 每帧调用
}

// ✅ 好
void OnGemsSwapped()
{
    var matches = MatchDetector.FindMatches(board); // 仅在需要时调用
}
```

### 2. 先用HasMatches快速检查
```csharp
// ✅ 高效
if (MatchDetector.HasMatches(board))
{
    var matches = MatchDetector.FindMatches(board);
    HandleMatches(matches);
}
```

### 3. 正确处理null
```csharp
// ✅ 安全
if (board != null && MatchDetector.HasMatches(board))
{
    // 处理匹配
}
```

### 4. 使用事件解耦
```csharp
// ✅ 松耦合
matchManager.OnMatchFound += HandleMatchFound;
matchManager.OnScoreEarned += UpdateScore;
```

---

## 性能参考

| 操作 | 复杂度 | 建议调用时机 |
|------|--------|-------------|
| FindMatches | O(W×H) | 交换后、消除后 |
| HasMatches | O(W×H) | 快速检查 |
| GetTotalMatchCount | O(M) | 计分时 |
| IsGemMatched | O(M) | 检查单个宝石 |

**W** = 宽度，**H** = 高度，**M** = 匹配宝石数

---

## 常见错误

| 错误 | 原因 | 解决方案 |
|------|------|---------|
| Board为null | 未分配 | 检查Inspector中的Board引用 |
| 返回空列表 | 无匹配 | 先用HasMatches检查 |
| 宝石重复消除 | 去重失败 | 确保GemType正确赋值 |
| 交换后无反应 | 事件未订阅 | 检查事件订阅 |

---

## 配置

```csharp
// 修改最小匹配数（在MatchDetector.cs中）
private const int MINIMUM_MATCH_LENGTH = 3; // 改为4可要求4连消
```

---

**最后更新**：2026年1月16日
**版本**：1.0
