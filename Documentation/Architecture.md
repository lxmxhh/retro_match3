# 匹配检测系统架构文档

## 目录

1. [系统概览](#系统概览)
2. [组件详解](#组件详解)
3. [数据流](#数据流)
4. [算法流程](#算法流程)
5. [集成点](#集成点)
6. [扩展架构](#扩展架构)

---

## 系统概览

### 核心三层架构

```
┌─────────────────────────────────────────────┐
│          GamePlay Layer                      │
│      (GameManager, InputController)         │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────▼──────────────────────────┐
│        Management Layer                     │
│          (MatchManager)                     │
│  ✓ 事件系统                                 │
│  ✓ 分数计算                                 │
│  ✓ 流程协调                                 │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────▼──────────────────────────┐
│          Detection Layer                    │
│          (MatchDetector)                    │
│  ✓ 水平检测                                 │
│  ✓ 垂直检测                                 │
│  ✓ 智能去重                                 │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────▼──────────────────────────┐
│           Data Layer                        │
│        (Board, Gem, GemType)               │
│  ✓ 棋盘数据                                 │
│  ✓ 宝石对象                                 │
│  ✓ 类型枚举                                 │
└─────────────────────────────────────────────┘
```

---

## 组件详解

### 1. MatchDetector 类

**职责**：检测匹配的逻辑引擎

```csharp
┌─────────────────────────────────┐
│      MatchDetector              │
│        (静态类)                 │
├─────────────────────────────────┤
│ 公开静态方法:                   │
│                                 │
│ ▸ FindMatches()                │
│   └─ 返回所有匹配              │
│   └─ List<List<Gem>>           │
│                                 │
│ ▸ HasMatches()                 │
│   └─ 快速检查是否有匹配        │
│   └─ 返回 bool                 │
│                                 │
│ ▸ GetTotalMatchCount()         │
│   └─ 返回匹配宝石总数          │
│   └─ 返回 int                  │
│                                 │
│ ▸ IsGemMatched()               │
│   └─ 检查单点是否被匹配        │
│   └─ 返回 bool                 │
│                                 │
│ ▸ DebugPrintMatches()          │
│   └─ 打印调试信息              │
│   └─ 返回 void                 │
├─────────────────────────────────┤
│ 私有静态方法:                   │
│                                 │
│ ▹ FindHorizontalMatches()      │
│ ▹ FindVerticalMatches()        │
│ ▹ CheckDirection()             │
│ ▹ RemoveDuplicateMatches()     │
├─────────────────────────────────┤
│ 常数:                           │
│                                 │
│ MINIMUM_MATCH_LENGTH = 3        │
└─────────────────────────────────┘
```

**使用场景**：
- 直接的匹配检测
- 快速验证
- 调试输出

---

### 2. MatchManager 类

**职责**：高层的匹配管理和流程协调

```csharp
┌─────────────────────────────────────────┐
│         MatchManager                    │
│      (MonoBehaviour)                    │
├─────────────────────────────────────────┤
│ 字段:                                   │
│  - board: Board                        │
│  - baseScorePerGem: int                │
│  - bonusScorePerExtraGem: int         │
│  - matchAnimationDuration: float       │
│  - currentScore: int                   │
├─────────────────────────────────────────┤
│ 事件:                                   │
│  + OnMatchFound(List<List<Gem>>)      │
│  + OnScoreEarned(int)                 │
│  + OnNoMatches()                      │
├─────────────────────────────────────────┤
│ 公开方法:                               │
│                                         │
│ ▸ CheckAndHandleMatches()             │
│   └─ 检查→处理→返回是否有匹配        │
│   └─ 返回 bool                        │
│                                         │
│ ▸ CalculateScore()                    │
│   └─ 计算匹配带来的分数               │
│   └─ 返回 int                         │
│                                         │
│ ▸ GetAllMatchedGems()                 │
│   └─ 获取所有被匹配的宝石             │
│   └─ 返回 List<Gem>                   │
│                                         │
│ ▸ ClearMatchedGems()                  │
│   └─ 销毁所有匹配的宝石               │
│   └─ 返回 void                        │
│                                         │
│ ▸ ValidateSwap()                      │
│   └─ 验证交换是否有效                 │
│   └─ 返回 bool                        │
│                                         │
│ ▸ GetMatchForGem()                    │
│   └─ 获取宝石所在的匹配组             │
│   └─ 返回 List<Gem>                   │
│                                         │
│ ▸ CurrentScore { get }                │
│   └─ 当前累计分数                     │
├─────────────────────────────────────────┤
│ 私有方法:                               │
│                                         │
│ ▹ Start()                             │
│ ▹ 其他辅助方法                        │
└─────────────────────────────────────────┘
```

**特点**：
- 事件驱动架构
- 完整的游戏流程支持
- 自动化的分数管理

---

### 3. Board 类

**职责**：棋盘数据管理

```csharp
┌───────────────────────────────┐
│        Board                  │
│     (MonoBehaviour)           │
├───────────────────────────────┤
│ 字段:                         │
│  - width: int = 8            │
│  - height: int = 8           │
│  - boardOffset: Vector3       │
│  - gems: Gem[,]              │
├───────────────────────────────┤
│ 属性:                         │
│  + Width { get }             │
│  + Height { get }            │
│  + BoardOffset { get }       │
├───────────────────────────────┤
│ 公开方法:                     │
│                               │
│ ▸ InitializeBoard()          │
│   └─ 初始化棋盘             │
│                               │
│ ▸ GetGem(x, y)              │
│   └─ 获取位置的宝石         │
│                               │
│ ▸ SetGem(x, y, gem)         │
│   └─ 设置位置的宝石         │
│                               │
│ ▸ IsValidPosition(x, y)     │
│   └─ 检查位置是否有效       │
│                               │
│ ▸ ClearBoard()              │
│   └─ 清空所有宝石           │
│                               │
│ ▸ GetAllGems()              │
│   └─ 获取所有宝石数组       │
└───────────────────────────────┘
```

**特点**：
- 2D数组存储结构
- 边界检查
- 简单的CRUD操作

---

### 4. Gem 类

**职责**：单个宝石的数据和行为

```csharp
┌────────────────────────────────┐
│         Gem                    │
│      (MonoBehaviour)           │
├────────────────────────────────┤
│ 字段:                          │
│  - gemType: GemType           │
│  - gridPosition: Vector2Int    │
│  - spriteRenderer: Sprite...  │
├────────────────────────────────┤
│ 属性:                          │
│  + Type { get }               │
│  + GridPosition { get/set }   │
├────────────────────────────────┤
│ 公开方法:                      │
│                                │
│ ▸ SetGemType(GemType)        │
│   └─ 设置宝石类型            │
│                                │
│ ▸ Initialize(type, pos)      │
│   └─ 初始化宝石              │
│                                │
│ ▸ GetWorldPosition()          │
│   └─ 获取世界坐标            │
│                                │
│ ▸ SetWorldPosition(Vector3)  │
│   └─ 设置世界坐标            │
└────────────────────────────────┘
```

---

### 5. GemType 枚举

```csharp
┌──────────────────────┐
│     GemType          │
│     (enum)           │
├──────────────────────┤
│ Red                  │
│ Blue                 │
│ Green                │
│ Yellow               │
│ Purple               │
│ Orange               │
└──────────────────────┘
```

---

## 数据流

### 完整的游戏循环数据流

```
1. 输入阶段
   ┌──────────────────┐
   │ InputController  │
   │ 检测用户操作     │
   └────────┬─────────┘
            │
            ▼
   ┌──────────────────┐
   │ 选中两个宝石     │
   │ gem1, gem2       │
   └────────┬─────────┘
            │
            ▼
2. 验证阶段
   ┌──────────────────────────────┐
   │ MatchManager                 │
   │ .ValidateSwap(gem1, gem2)   │
   │                              │
   │ 内部:                        │
   │  1. 执行虚拟交换            │
   │  2. 调用 MatchDetector      │
   │     .HasMatches()           │
   │  3. 撤销虚拟交换            │
   │  4. 返回结果                │
   └────────┬─────────────────────┘
            │
            ├─ 无效 ─→ 交换不执行 ──┐
            │                        │
            ├─ 有效 ─→ 继续         │
            │                        │
            ▼                        │
3. 执行交换                          │
   ┌──────────────────┐             │
   │ Board            │             │
   │ .SetGem()        │             │
   │ 更新棋盘数据     │             │
   └────────┬─────────┘             │
            │                        │
            ▼                        │
4. 匹配检测                          │
   ┌─────────────────────────────┐  │
   │ MatchDetector               │  │
   │ .FindMatches(board)         │  │
   │                             │  │
   │ 返回: List<List<Gem>>       │  │
   │  ┌─ Match 1: [G1, G2, G3]   │  │
   │  ├─ Match 2: [G4, G5]       │  │
   │  └─ ...                     │  │
   └────────┬────────────────────┘  │
            │                        │
            ▼                        │
5. 分数计算                          │
   ┌──────────────────────────────┐ │
   │ MatchManager                 │ │
   │ .CalculateScore(matches)     │ │
   │                              │ │
   │ Match1: 3gems × 100 = 300   │ │
   │ Match2: 2gems × 100 = 200   │ │ (实际不计，小于3)
   │ Total: 300                   │ │
   └────────┬─────────────────────┘ │
            │                        │
            ▼                        │
6. 消除阶段                          │
   ┌──────────────────────────────┐ │
   │ MatchManager                 │ │
   │ .ClearMatchedGems()          │ │
   │                              │ │
   │ 销毁所有匹配的宝石对象       │ │
   │ 清空Board中的引用            │ │
   └────────┬─────────────────────┘ │
            │                        │
            ▼                        │
7. 重力应用                          │
   ┌──────────────────────────────┐ │
   │ GameManager                  │ │
   │ .ApplyGravity()              │ │
   │                              │ │
   │ 宝石下落                     │ │
   │ 填充空位                     │ │
   └────────┬─────────────────────┘ │
            │                        │
            ▼                        │
8. 级联检测                          │
   ┌──────────────────────────────┐ │
   │ MatchManager                 │ │
   │ .CheckAndHandleMatches()     │ │
   │                              │ │
   │ 是否产生新匹配？             │ │
   │  ├─ 是 ─→ 回到第5步         │ │
   │  └─ 否 ─→ 继续到第9步       │ │
   └────────┬─────────────────────┘ │
            │                        │
            ▼                        │
9. 游戏继续                          │
   ┌──────────────────┐             │
   │ 等待下一个操作   │             │
   └──────────────────┘             │
            ▲                        │
            └────────────────────────┘
```

---

## 算法流程

### FindMatches 的详细流程

```
输入: Board board
输出: List<List<Gem>>

START
 │
 ├─ 检查 board != null
 │   │
 │   └─ 如果为null: 返回空列表
 │
 ├─ 初始化 allMatches = []
 │
 ├─ 阶段1: 水平扫描
 │   │
 │   ├─ 对 y = 0 到 Height-1:
 │   │   │
 │   │   ├─ 对 x = 0 到 Width-1:
 │   │   │   │
 │   │   │   ├─ gem = GetGem(x, y)
 │   │   │   │
 │   │   │   ├─ 如果 gem == null: 跳过
 │   │   │   │
 │   │   │   ├─ match = CheckDirection(x, y, 1, 0)
 │   │   │   │   "向右检查连续宝石"
 │   │   │   │
 │   │   │   ├─ 如果 match.Count >= 3:
 │   │   │   │   │
 │   │   │   │   ├─ allMatches.Add(match)
 │   │   │   │   │
 │   │   │   │   └─ x += match.Count  "跳过已检查"
 │   │   │   │
 │   │   │   └─ 否则:
 │   │   │       x++
 │   │   │
 │   │   └─ 继续下一行
 │   │
 │   └─ 水平扫描完成
 │
 ├─ 阶段2: 垂直扫描
 │   │
 │   ├─ 对 x = 0 到 Width-1:
 │   │   │
 │   │   ├─ 对 y = 0 到 Height-1:
 │   │   │   │
 │   │   │   ├─ gem = GetGem(x, y)
 │   │   │   │
 │   │   │   ├─ 如果 gem == null: 跳过
 │   │   │   │
 │   │   │   ├─ match = CheckDirection(x, y, 0, 1)
 │   │   │   │   "向上检查连续宝石"
 │   │   │   │
 │   │   │   ├─ 如果 match.Count >= 3:
 │   │   │   │   │
 │   │   │   │   ├─ allMatches.Add(match)
 │   │   │   │   │
 │   │   │   │   └─ y += match.Count  "跳过已检查"
 │   │   │   │
 │   │   │   └─ 否则:
 │   │   │       y++
 │   │   │
 │   │   └─ 继续下一列
 │   │
 │   └─ 垂直扫描完成
 │
 ├─ 阶段3: 去重
 │   │
 │   ├─ allMatches 按长度从长到短排序
 │   │
 │   ├─ usedGems = empty set
 │   │
 │   ├─ 对每个 match 在 sortedMatches:
 │   │   │
 │   │   ├─ hasUsedGem = false
 │   │   │
 │   │   ├─ 对每个 gem 在 match:
 │   │   │   │
 │   │   │   ├─ 如果 gem 在 usedGems:
 │   │   │   │   hasUsedGem = true
 │   │   │   │
 │   │   │   └─ 中断循环
 │   │   │
 │   │   ├─ 如果 !hasUsedGem:
 │   │   │   │
 │   │   │   ├─ 将 match 添加到 uniqueMatches
 │   │   │   │
 │   │   │   └─ 将 match 中所有 gem 标记为已使用
 │   │   │
 │   │   └─ 继续下一个匹配
 │   │
 │   └─ 去重完成
 │
 ├─ Debug.Log 结果
 │
 └─ 返回 uniqueMatches
END
```

### CheckDirection 的流程

```
输入: startX, startY, dirX, dirY
输出: List<Gem>

START
 │
 ├─ startGem = GetGem(startX, startY)
 │
 ├─ 如果 startGem == null: 返回空列表
 │
 ├─ targetType = startGem.Type
 │
 ├─ matchGroup = []
 │
 ├─ currentX = startX, currentY = startY
 │
 ├─ 循环:
 │   │
 │   ├─ 如果 IsValidPosition(currentX, currentY):
 │   │   │
 │   │   ├─ currentGem = GetGem(currentX, currentY)
 │   │   │
 │   │   ├─ 如果 currentGem == null || currentGem.Type != targetType:
 │   │   │   │
 │   │   │   └─ 中断循环
 │   │   │
 │   │   ├─ 否则:
 │   │   │   │
 │   │   │   ├─ matchGroup.Add(currentGem)
 │   │   │   │
 │   │   │   ├─ currentX += dirX
 │   │   │   │
 │   │   │   └─ currentY += dirY
 │   │   │
 │   │   └─ 继续循环
 │   │
 │   └─ 否则:
 │       │
 │       └─ 中断循环 (超出边界)
 │
 └─ 返回 matchGroup
END
```

---

## 集成点

### 1. InputController 到 MatchManager

```
InputController
    │
    ├─ 事件: OnSwapRequested(gem1, gem2)
    │
    ▼
MatchManager
    │
    ├─ 订阅 OnSwapRequested
    │
    ├─ 调用 ValidateSwap(gem1, gem2)
    │
    │   内部逻辑:
    │   ├─ 虚拟交换
    │   ├─ 检查 HasMatches()
    │   ├─ 撤销交换
    │   └─ 返回结果
    │
    ├─ 如果有效:
    │   ├─ 执行真实交换
    │   ├─ 调用 CheckAndHandleMatches()
    │   └─ 触发事件
    │
    └─ 如果无效:
        └─ 提示无效交换
```

### 2. MatchDetector 到 MatchManager

```
MatchManager 作为 MatchDetector 的使用者

主要调用:
├─ MatchDetector.FindMatches()
│   └─ 获取所有匹配的宝石组
│
├─ MatchDetector.HasMatches()
│   └─ 快速检查是否有匹配
│
├─ MatchDetector.GetTotalMatchCount()
│   └─ 计算匹配宝石总数
│
├─ MatchDetector.IsGemMatched()
│   └─ 检查单个宝石是否被匹配
│
└─ MatchDetector.DebugPrintMatches()
    └─ 调试输出
```

### 3. Board 和 Gem 的交互

```
Board
 │
 ├─ 存储 Gem[,] 数组
 │
 ├─ 提供访问接口:
 │   ├─ GetGem(x, y) → Gem
 │   ├─ SetGem(x, y, gem) → void
 │   ├─ IsValidPosition(x, y) → bool
 │   └─ ...
 │
 ▼
Gem
 │
 ├─ 存储:
 │   ├─ Type: GemType
 │   ├─ GridPosition: Vector2Int
 │   └─ 其他属性
 │
 └─ MatchDetector 通过 Board 获取 Gem
    └─ board.GetGem(x, y)
```

---

## 扩展架构

### 扩展方案 1: 特殊宝石

```
当前结构:
┌─────────────────────┐
│      Gem            │
│  - type: GemType    │
│  - position: V2Int  │
└─────────────────────┘

扩展方案:
┌─────────────────────────────┐
│      Gem (基类)             │
│  - type: GemType            │
│  - position: Vector2Int     │
│  - virtual OnMatch()        │
└────────┬────────────────────┘
         │
    ┌────┴────┬────────────┬──────────────┐
    │         │            │              │
    ▼         ▼            ▼              ▼
┌────────┐┌──────┐    ┌──────────┐  ┌──────────┐
│NormalGem││BombGem   │RainbowGem  │ConnectGem│
│        ││        │  │            │         │
│基础行为││爆炸效果│  │任意匹配    │ 连接效果 │
└────────┘└──────┘    └──────────┘  └──────────┘
```

扩展后的MatchDetector:
```csharp
public static List<List<Gem>> FindMatches(Board board)
{
    var matches = FindMatches_Standard(board);
    matches.AddRange(FindMatches_Special(board));

    // 特殊宝石处理
    foreach (var match in matches)
    {
        foreach (var gem in match)
        {
            gem.OnMatch?.Invoke();
        }
    }

    return matches;
}
```

---

### 扩展方案 2: 预测系统

```
新增类: MatchPredictor

用途: 在交换前预测结果

┌──────────────────────────────────┐
│   MatchPredictor (新增)          │
├──────────────────────────────────┤
│ + PredictAfterSwap()            │
│   └─ 虚拟执行，返回预测结果     │
│                                 │
│ + GetAvailableMoves()           │
│   └─ 获取所有可能的有效交换     │
│                                 │
│ + EvaluateMove()                │
│   └─ 评估一个交换的价值         │
│                                 │
│ + FindBestMove()                │
│   └─ 找到最佳交换               │
└──────────────────────────────────┘

使用:
    MatchPredictor predictor = new MatchPredictor();
    var prediction = predictor.PredictAfterSwap(gem1, gem2);

    // 评估
    float score = predictor.EvaluateMove(gem1, gem2);
```

---

### 扩展方案 3: 组合系统

```
新增字段到MatchManager:

├─ comboCount: int
├─ comboMultiplier: float
└─ comboChain: List<List<Gem>>

新增方法:

├─ GetComboMultiplier()
│   └─ 返回当前组合倍数
│
├─ ResetCombo()
│   └─ 重置组合计数
│
└─ CalculateComboScore()
    └─ 计算带组合倍数的分数

流程:
    Match Found
        │
        ├─ comboCount++
        ├─ comboMultiplier = 1 + comboCount * 0.5
        │
        ├─ Score = BaseScore * comboMultiplier
        │
        └─ UI 显示 x2.5 Combo!
```

---

### 扩展方案 4: 时间限制模式

```
新增类: TimedMatchMode

┌──────────────────────────────┐
│  TimedMatchMode              │
├──────────────────────────────┤
│ - timeLimit: float           │
│ - timeRemaining: float       │
│ - isTimeUp: bool            │
│                              │
│ + Update()                   │
│   └─ 更新时间               │
│                              │
│ + GetTimeBonus()            │
│   └─ 剩余时间的分数加成     │
│                              │
│ + OnTimeUp()                │
│   └─ 时间到时的处理         │
└──────────────────────────────┘

集成:

    MatchManager
        │
        ├─ 依赖 TimedMatchMode
        │
        └─ CalculateScore() 中
            └─ score += GetTimeBonus()
```

---

### 扩展方案 5: 难度系统

```
新增枚举和类:

public enum Difficulty
{
    Easy,
    Normal,
    Hard,
    Expert
}

public class DifficultyConfig
{
    public int minMatchLength;      // 最小匹配数
    public float matchSpawnRate;    // 宝石生成速率
    public int scoreMultiplier;     // 分数倍数
    public float moveTime;          // 移动时间限制
}

集成:

    MatchManager
        │
        ├─ currentDifficulty: Difficulty
        │
        ├─ GetDifficultyConfig()
        │   └─ 根据难度返回配置
        │
        └─ CalculateScore() 中
            └─ score *= difficultyConfig.scoreMultiplier
```

---

## 性能优化架构

### 方案 1: 缓存机制

```
┌─────────────────────────────────┐
│   CachedMatchDetector           │
├─────────────────────────────────┤
│ - cachedMatches: List<List<Gem>>│
│ - isDirty: bool                 │
│ - lastBoardHash: int            │
├─────────────────────────────────┤
│ + FindMatches()                 │
│   └─ 返回缓存或重新计算        │
│                                 │
│ + InvalidateCache()             │
│   └─ 标记为过期                │
│                                 │
│ + IsCacheValid()                │
│   └─ 检查缓存是否有效          │
└─────────────────────────────────┘

使用场景:
    玩家操作
        │
        ├─ 第1次: 缓存为空 → 计算 → 缓存
        ├─ 第2次: 缓存有效 → 直接返回
        ├─ 棋盘改变 → InvalidateCache()
        ├─ 第3次: 缓存过期 → 重新计算
        └─ ...
```

---

## 总结

### 核心架构特点

1. **分层设计**
   - 清晰的职责划分
   - 易于扩展和维护

2. **静态API**
   - MatchDetector 使用静态方法
   - 无需实例化，高效便捷

3. **事件驱动**
   - MatchManager 提供事件系统
   - 松耦合的组件交互

4. **错误处理**
   - 完整的null检查
   - 边界验证
   - 详细的日志

5. **可扩展**
   - 支持特殊宝石
   - 支持新的匹配规则
   - 支持难度系统等

---

**文档版本**：1.0
**最后更新**：2026年1月15日
