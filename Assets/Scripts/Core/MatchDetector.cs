using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace RetroMatch2D.Core
{
    /// <summary>
    /// 匹配类型枚举
    /// 定义了游戏中所有可能的匹配形状类型
    /// </summary>
    public enum MatchType
    {
        None = 0,           // 无匹配
        Horizontal3 = 1,    // 水平3连
        Vertical3 = 2,      // 垂直3连
        Horizontal4 = 3,    // 水平4连 (Rocket)
        Vertical4 = 4,      // 垂直4连 (Rocket)
        Horizontal5 = 5,    // 水平5连或更多 (Light Ball)
        Vertical5 = 6,      // 垂直5连或更多 (Light Ball)
        Square = 7,         // 田字格 2x2 (Propeller)
        SquarePlus = 11,    // 田字格+1 2x2+1 (增强Propeller, 5个宝石)
        LShape = 8,         // L型 (TNT)
        TShape = 9          // T型 (TNT) - 包括十字型
    }

    /// <summary>
    /// 匹配数据结构
    /// 包含匹配的宝石列表、类型和优先级信息
    /// </summary>
    public class MatchData
    {
        /// <summary>匹配的宝石列表</summary>
        public List<Gem> Gems { get; set; }

        /// <summary>匹配类型</summary>
        public MatchType Type { get; set; }

        /// <summary>匹配优先级（数值越高优先级越高）</summary>
        public int Priority { get; set; }

        /// <summary>匹配的宝石类型</summary>
        public GemType GemType { get; set; }

        public MatchData(List<Gem> gems, MatchType type, GemType gemType)
        {
            Gems = gems;
            Type = type;
            GemType = gemType;
            Priority = CalculatePriority(type, gems.Count);
        }

        /// <summary>
        /// 根据Royal Match规则计算优先级
        /// 优先级顺序：Light Ball > T型TNT > L型TNT > 2x2+1 > 2x2 Propeller > Rocket > Basic 3
        /// </summary>
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
                case MatchType.TShape:
                    priority += 800; // T型TNT（包括十字型）
                    break;
                case MatchType.LShape:
                    priority += 700; // L型TNT
                    break;
                case MatchType.SquarePlus:
                    priority += 650; // 2x2+1 增强Propeller
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
    }

    /// <summary>
    /// 匹配检测系统 - 基于Royal Match规则重写
    /// 实现"连续判定"规则：匹配形状判定时，往外探查连续同色物品，尽可能多的消除连续的同色物品
    /// </summary>
    public class MatchDetector
    {
        /// <summary>
        /// 最小匹配数（3个相同宝石为一个匹配）
        /// </summary>
        private const int MINIMUM_MATCH_LENGTH = 3;

        // ==================== 新的Royal Match规则匹配系统 ====================

        /// <summary>
        /// 增强的匹配检测 - 基于Royal Match规则
        /// 检测顺序：Light Ball > L/T TNT > 2x2+1 > 2x2 Propeller > Rocket > Basic 3
        /// 所有匹配都明确定义，不使用扩展算法
        /// </summary>
        public static List<MatchData> FindMatchesEnhanced(Board board)
        {
            if (board == null)
            {
                Debug.LogError("MatchDetector.FindMatchesEnhanced: Board is null!");
                return new List<MatchData>();
            }

            // 阶段1：收集所有可能的匹配（不立即标记）
            var allCandidates = new List<MatchData>();

            // 遍历整个棋盘，收集所有匹配
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    Gem currentGem = board.GetGem(x, y);
                    if (currentGem == null)
                        continue;

                    GemType gemType = currentGem.Type;

                    // 检测所有类型的匹配（从高优先级到低优先级）
                    var emptyProcessed = new HashSet<Gem>(); // 临时空集，不影响后续检测

                    // 1. Light Ball (5+连)
                    var lightBallMatch = TryFindLightBall(board, x, y, gemType, emptyProcessed);
                    if (lightBallMatch != null)
                        allCandidates.Add(lightBallMatch);

                    // 2. L型和T型 (TNT)
                    var lOrTMatch = TryFindLOrTShape(board, x, y, gemType, emptyProcessed);
                    if (lOrTMatch != null)
                        allCandidates.Add(lOrTMatch);

                    // 3. 2x2+1方块 (增强Propeller)
                    var squarePlusMatch = TryFindSquarePlus(board, x, y, gemType, emptyProcessed);
                    if (squarePlusMatch != null)
                        allCandidates.Add(squarePlusMatch);

                    // 4. 2x2方块 (Propeller)
                    var squareMatch = TryFindSquare(board, x, y, gemType, emptyProcessed);
                    if (squareMatch != null)
                        allCandidates.Add(squareMatch);

                    // 5. Rocket (4连)
                    var rocketMatch = TryFindRocket(board, x, y, gemType, emptyProcessed);
                    if (rocketMatch != null)
                        allCandidates.Add(rocketMatch);

                    // 6. 基础3连
                    var basicMatch = TryFindBasicMatch(board, x, y, gemType, emptyProcessed);
                    if (basicMatch != null)
                        allCandidates.Add(basicMatch);
                }
            }

            // 阶段2：按优先级排序
            allCandidates = allCandidates.OrderByDescending(m => m.Priority).ToList();

            // 阶段3：去重，选择不冲突的匹配
            var finalMatches = new List<MatchData>();
            var usedGems = new HashSet<Gem>();

            foreach (var candidate in allCandidates)
            {
                // 检查这个匹配的宝石是否已被使用
                bool hasConflict = candidate.Gems.Any(g => usedGems.Contains(g));

                if (!hasConflict)
                {
                    finalMatches.Add(candidate);
                    // 标记这些宝石为已使用
                    foreach (var gem in candidate.Gems)
                    {
                        usedGems.Add(gem);
                    }
                }
            }

            Debug.Log($"MatchDetector Enhanced: Found {finalMatches.Count} matches (from {allCandidates.Count} candidates)");

            return finalMatches;
        }

        /// <summary>
        /// 核心算法：连续扩展 - 从种子宝石开始，找到所有连续同色宝石
        /// 实现Royal Match的"连续判定"规则
        /// </summary>
        /// <param name="board">棋盘</param>
        /// <param name="seeds">种子宝石列表</param>
        /// <param name="gemType">目标宝石类型</param>
        /// <returns>所有连续同色的宝石（包括种子）</returns>
        private static List<Gem> ExpandContinuousGems(Board board, List<Gem> seeds, GemType gemType)
        {
            var result = new HashSet<Gem>(seeds);
            var queue = new Queue<Gem>(seeds);

            // BFS广度优先搜索
            while (queue.Count > 0)
            {
                Gem current = queue.Dequeue();
                Vector2Int pos = current.GridPosition;

                // 检查四个方向（上下左右）
                Vector2Int[] directions = new Vector2Int[]
                {
                    new Vector2Int(0, 1),  // 上
                    new Vector2Int(0, -1), // 下
                    new Vector2Int(-1, 0), // 左
                    new Vector2Int(1, 0)   // 右
                };

                foreach (var dir in directions)
                {
                    int newX = pos.x + dir.x;
                    int newY = pos.y + dir.y;

                    if (!board.IsValidPosition(newX, newY))
                        continue;

                    Gem neighbor = board.GetGem(newX, newY);
                    if (neighbor == null || neighbor.Type != gemType)
                        continue;

                    if (!result.Contains(neighbor))
                    {
                        result.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return result.ToList();
        }

        /// <summary>
        /// 尝试检测Light Ball (5+连)
        /// 直线型匹配不需要连续扩展，只消除这条直线上的宝石
        /// </summary>
        private static MatchData TryFindLightBall(Board board, int x, int y, GemType gemType, HashSet<Gem> processedGems)
        {
            // 检查水平5+连
            var horizLine = CheckDirectionContinuous(board, x, y, 1, 0, gemType);
            if (horizLine.Count >= 5)
            {
                if (horizLine.Any(g => processedGems.Contains(g)))
                    return null; // 已被处理
                return new MatchData(horizLine, MatchType.Horizontal5, gemType);
            }

            // 检查垂直5+连
            var vertLine = CheckDirectionContinuous(board, x, y, 0, 1, gemType);
            if (vertLine.Count >= 5)
            {
                if (vertLine.Any(g => processedGems.Contains(g)))
                    return null;
                return new MatchData(vertLine, MatchType.Vertical5, gemType);
            }

            return null;
        }

        /// <summary>
        /// 尝试检测L型或T型 (TNT)
        /// L型和T型不需要连续扩展，只消除匹配到的形状本身
        /// </summary>
        private static MatchData TryFindLOrTShape(Board board, int x, int y, GemType gemType, HashSet<Gem> processedGems)
        {
            Gem centerGem = board.GetGem(x, y);
            if (centerGem == null || centerGem.Type != gemType)
                return null;

            // 检查4个方向的连续宝石
            var up = CheckDirectionContinuous(board, x, y, 0, 1, gemType);
            var down = CheckDirectionContinuous(board, x, y, 0, -1, gemType);
            var left = CheckDirectionContinuous(board, x, y, -1, 0, gemType);
            var right = CheckDirectionContinuous(board, x, y, 1, 0, gemType);

            // 合并对向的连线（去重中心点）
            var vertical = MergeLines(up, down, centerGem);
            var horizontal = MergeLines(left, right, centerGem);

            // 检测十字型 - 水平3+ 且 垂直3+，归类为T型TNT
            if (horizontal.Count >= 3 && vertical.Count >= 3)
            {
                var seeds = new HashSet<Gem>(horizontal);
                foreach (var gem in vertical) seeds.Add(gem);
                if (seeds.Any(g => processedGems.Contains(g)))
                    return null;
                return new MatchData(seeds.ToList(), MatchType.TShape, gemType);
            }

            // 检测T型 - 一个方向3+，另一个方向只有一侧延伸2+
            // T型情况1：水平主线3+，垂直只有一侧2+（上或下）
            if (horizontal.Count >= 3)
            {
                // 上侧有2+，下侧不足2（或没有）
                if (up.Count >= 2 && down.Count < 2)
                {
                    var seeds = new HashSet<Gem>(horizontal);
                    foreach (var gem in up) seeds.Add(gem);
                    if (seeds.Count >= 5)
                    {
                        if (seeds.Any(g => processedGems.Contains(g)))
                            return null;
                        return new MatchData(seeds.ToList(), MatchType.TShape, gemType);
                    }
                }
                // 下侧有2+，上侧不足2（或没有）
                if (down.Count >= 2 && up.Count < 2)
                {
                    var seeds = new HashSet<Gem>(horizontal);
                    foreach (var gem in down) seeds.Add(gem);
                    if (seeds.Count >= 5)
                    {
                        if (seeds.Any(g => processedGems.Contains(g)))
                            return null;
                        return new MatchData(seeds.ToList(), MatchType.TShape, gemType);
                    }
                }
            }

            // T型情况2：垂直主线3+，水平只有一侧2+（左或右）
            if (vertical.Count >= 3)
            {
                // 左侧有2+，右侧不足2（或没有）
                if (left.Count >= 2 && right.Count < 2)
                {
                    var seeds = new HashSet<Gem>(vertical);
                    foreach (var gem in left) seeds.Add(gem);
                    if (seeds.Count >= 5)
                    {
                        if (seeds.Any(g => processedGems.Contains(g)))
                            return null;
                        return new MatchData(seeds.ToList(), MatchType.TShape, gemType);
                    }
                }
                // 右侧有2+，左侧不足2（或没有）
                if (right.Count >= 2 && left.Count < 2)
                {
                    var seeds = new HashSet<Gem>(vertical);
                    foreach (var gem in right) seeds.Add(gem);
                    if (seeds.Count >= 5)
                    {
                        if (seeds.Any(g => processedGems.Contains(g)))
                            return null;
                        return new MatchData(seeds.ToList(), MatchType.TShape, gemType);
                    }
                }
            }

            // 检测L型 - 检查4种L型配置
            // L型定义：从角点出发，两个方向各有至少3个（包括中心），总共至少5个

            // 右上L (┘)
            if (right.Count >= 3 && up.Count >= 3)
            {
                var seeds = new HashSet<Gem>(right);
                foreach (var gem in up) seeds.Add(gem);
                // L型至少需要5个宝石
                if (seeds.Count >= 5)
                {
                    if (seeds.Any(g => processedGems.Contains(g)))
                        return null;
                    return new MatchData(seeds.ToList(), MatchType.LShape, gemType);
                }
            }

            // 右下L (┐)
            if (right.Count >= 3 && down.Count >= 3)
            {
                var seeds = new HashSet<Gem>(right);
                foreach (var gem in down) seeds.Add(gem);
                if (seeds.Count >= 5)
                {
                    if (seeds.Any(g => processedGems.Contains(g)))
                        return null;
                    return new MatchData(seeds.ToList(), MatchType.LShape, gemType);
                }
            }

            // 左上L (└)
            if (left.Count >= 3 && up.Count >= 3)
            {
                var seeds = new HashSet<Gem>(left);
                foreach (var gem in up) seeds.Add(gem);
                if (seeds.Count >= 5)
                {
                    if (seeds.Any(g => processedGems.Contains(g)))
                        return null;
                    return new MatchData(seeds.ToList(), MatchType.LShape, gemType);
                }
            }

            // 左下L (┌)
            if (left.Count >= 3 && down.Count >= 3)
            {
                var seeds = new HashSet<Gem>(left);
                foreach (var gem in down) seeds.Add(gem);
                if (seeds.Count >= 5)
                {
                    if (seeds.Any(g => processedGems.Contains(g)))
                        return null;
                    return new MatchData(seeds.ToList(), MatchType.LShape, gemType);
                }
            }

            return null;
        }

        /// <summary>
        /// 尝试检测2x2+1方块 (增强Propeller)
        /// 2x2核心 + 周围1个同色宝石，共5个宝石
        /// </summary>
        private static MatchData TryFindSquarePlus(Board board, int x, int y, GemType gemType, HashSet<Gem> processedGems)
        {
            // 检查以(x,y)为左下角的2x2方块
            if (!board.IsValidPosition(x + 1, y) || !board.IsValidPosition(x, y + 1) || !board.IsValidPosition(x + 1, y + 1))
                return null;

            Gem gem1 = board.GetGem(x, y);
            Gem gem2 = board.GetGem(x + 1, y);
            Gem gem3 = board.GetGem(x, y + 1);
            Gem gem4 = board.GetGem(x + 1, y + 1);

            if (gem1 == null || gem2 == null || gem3 == null || gem4 == null)
                return null;

            if (gem1.Type != gemType || gem2.Type != gemType || gem3.Type != gemType || gem4.Type != gemType)
                return null;

            // 找到2x2核心，现在检查周围8个位置是否有第5个同色宝石
            // 周围8个位置：上2、下2、左2、右2
            var adjacentPositions = new List<(int, int)>
            {
                (x, y + 2),     // 左上方
                (x + 1, y + 2), // 右上方
                (x, y - 1),     // 左下方
                (x + 1, y - 1), // 右下方
                (x - 1, y),     // 左下侧
                (x - 1, y + 1), // 左上侧
                (x + 2, y),     // 右下侧
                (x + 2, y + 1)  // 右上侧
            };

            // 检查是否有恰好1个相邻的同色宝石
            Gem fifthGem = null;
            foreach (var (px, py) in adjacentPositions)
            {
                if (board.IsValidPosition(px, py))
                {
                    Gem adjacentGem = board.GetGem(px, py);
                    if (adjacentGem != null && adjacentGem.Type == gemType)
                    {
                        fifthGem = adjacentGem;
                        break; // 找到第5个宝石
                    }
                }
            }

            // 如果没有找到第5个宝石，不是2x2+1模式
            if (fifthGem == null)
                return null;

            // 组成2x2+1，共5个宝石
            var squarePlusGems = new List<Gem> { gem1, gem2, gem3, gem4, fifthGem };

            if (squarePlusGems.Any(g => processedGems.Contains(g)))
                return null;

            return new MatchData(squarePlusGems, MatchType.SquarePlus, gemType);
        }

        /// <summary>
        /// 尝试检测标准2x2方块 (Propeller)
        /// 恰好4个宝石，不扩展
        /// </summary>
        private static MatchData TryFindSquare(Board board, int x, int y, GemType gemType, HashSet<Gem> processedGems)
        {
            // 检查以(x,y)为左下角的2x2方块
            if (!board.IsValidPosition(x + 1, y) || !board.IsValidPosition(x, y + 1) || !board.IsValidPosition(x + 1, y + 1))
                return null;

            Gem gem1 = board.GetGem(x, y);
            Gem gem2 = board.GetGem(x + 1, y);
            Gem gem3 = board.GetGem(x, y + 1);
            Gem gem4 = board.GetGem(x + 1, y + 1);

            if (gem1 == null || gem2 == null || gem3 == null || gem4 == null)
                return null;

            if (gem1.Type != gemType || gem2.Type != gemType || gem3.Type != gemType || gem4.Type != gemType)
                return null;

            // 标准2x2方块，恰好4个宝石
            var squareGems = new List<Gem> { gem1, gem2, gem3, gem4 };

            if (squareGems.Any(g => processedGems.Contains(g)))
                return null;

            return new MatchData(squareGems, MatchType.Square, gemType);
        }

        /// <summary>
        /// 尝试检测Rocket (4连)
        /// 直线型匹配不需要连续扩展，只消除这条直线上的宝石
        /// </summary>
        private static MatchData TryFindRocket(Board board, int x, int y, GemType gemType, HashSet<Gem> processedGems)
        {
            // 检查水平4连
            var horizLine = CheckDirectionContinuous(board, x, y, 1, 0, gemType);
            if (horizLine.Count == 4)
            {
                if (horizLine.Any(g => processedGems.Contains(g)))
                    return null;
                return new MatchData(horizLine, MatchType.Horizontal4, gemType);
            }

            // 检查垂直4连
            var vertLine = CheckDirectionContinuous(board, x, y, 0, 1, gemType);
            if (vertLine.Count == 4)
            {
                if (vertLine.Any(g => processedGems.Contains(g)))
                    return null;
                return new MatchData(vertLine, MatchType.Vertical4, gemType);
            }

            return null;
        }

        /// <summary>
        /// 尝试检测基础3连
        /// 直线型匹配不需要连续扩展，只消除这条直线上的宝石
        /// </summary>
        private static MatchData TryFindBasicMatch(Board board, int x, int y, GemType gemType, HashSet<Gem> processedGems)
        {
            // 检查水平3连
            var horizLine = CheckDirectionContinuous(board, x, y, 1, 0, gemType);
            if (horizLine.Count >= 3)
            {
                if (horizLine.Any(g => processedGems.Contains(g)))
                    return null;
                return new MatchData(horizLine, MatchType.Horizontal3, gemType);
            }

            // 检查垂直3连
            var vertLine = CheckDirectionContinuous(board, x, y, 0, 1, gemType);
            if (vertLine.Count >= 3)
            {
                if (vertLine.Any(g => processedGems.Contains(g)))
                    return null;
                return new MatchData(vertLine, MatchType.Vertical3, gemType);
            }

            return null;
        }

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

        // ==================== 向后兼容的旧API ====================

        /// <summary>
        /// 查找所有匹配项（向后兼容）
        /// 内部调用新的Enhanced系统
        /// </summary>
        public static List<List<Gem>> FindMatches(Board board)
        {
            var enhancedMatches = FindMatchesEnhanced(board);
            return enhancedMatches.Select(m => m.Gems).ToList();
        }

        /// <summary>
        /// 检查是否存在任何匹配
        /// </summary>
        public static bool HasMatches(Board board)
        {
            var matches = FindMatchesEnhanced(board);
            return matches.Count > 0;
        }

        /// <summary>
        /// 获取总匹配数
        /// </summary>
        public static int GetTotalMatchCount(Board board)
        {
            var matches = FindMatchesEnhanced(board);
            int totalCount = 0;

            foreach (var match in matches)
            {
                totalCount += match.Gems.Count;
            }

            return totalCount;
        }

        /// <summary>
        /// 获取指定位置是否有宝石被匹配
        /// </summary>
        public static bool IsGemMatched(Board board, int x, int y)
        {
            var matches = FindMatchesEnhanced(board);
            Gem targetGem = board.GetGem(x, y);

            if (targetGem == null)
            {
                return false;
            }

            foreach (var match in matches)
            {
                if (match.Gems.Contains(targetGem))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 调试用：打印匹配信息（向后兼容）
        /// </summary>
        public static void DebugPrintMatches(List<List<Gem>> matches)
        {
            if (matches.Count == 0)
            {
                Debug.Log("MatchDetector: No matches found");
                return;
            }

            Debug.Log($"MatchDetector: Found {matches.Count} match groups:");

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var positions = string.Join(", ", match.Select(g => $"({g.GridPosition.x},{g.GridPosition.y})"));
                Debug.Log($"  Match {i + 1}: {match.Count} gems of type {match[0].Type} at [{positions}]");
            }
        }

        /// <summary>
        /// 调试用：打印增强匹配信息
        /// </summary>
        public static void DebugPrintMatchesEnhanced(List<MatchData> matches)
        {
            if (matches.Count == 0)
            {
                Debug.Log("MatchDetector Enhanced: No matches found");
                return;
            }

            Debug.Log($"MatchDetector Enhanced: Found {matches.Count} match groups:");

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var positions = string.Join(", ", match.Gems.Select(g => $"({g.GridPosition.x},{g.GridPosition.y})"));
                Debug.Log($"  Match {i + 1}: Type={match.Type}, Priority={match.Priority}, " +
                         $"Gems={match.Gems.Count}, GemType={match.GemType}, Positions=[{positions}]");
            }
        }
    }
}
