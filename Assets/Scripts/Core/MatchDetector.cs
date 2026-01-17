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
        Horizontal4 = 3,    // 水平4连
        Vertical4 = 4,      // 垂直4连
        Horizontal5 = 5,    // 水平5连或更多
        Vertical5 = 6,      // 垂直5连或更多
        LShape = 7,         // L型（3+2）
        TShape = 8,         // T型（3+2+2或交叉）
        Cross = 9           // 十字型（垂直3+水平3交叉）
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
        /// 根据匹配类型和数量计算优先级
        /// </summary>
        private int CalculatePriority(MatchType type, int gemCount)
        {
            // 基础优先级：宝石数量
            int priority = gemCount * 10;

            // 特殊形状加成
            switch (type)
            {
                case MatchType.Cross:
                    priority += 100; // 十字型最高优先级
                    break;
                case MatchType.TShape:
                    priority += 80; // T型次高
                    break;
                case MatchType.LShape:
                    priority += 60; // L型中等
                    break;
                case MatchType.Horizontal5:
                case MatchType.Vertical5:
                    priority += 50; // 5连较高
                    break;
                case MatchType.Horizontal4:
                case MatchType.Vertical4:
                    priority += 30; // 4连中等
                    break;
                default:
                    break;
            }

            return priority;
        }
    }

    /// <summary>
    /// 匹配检测系统
    /// 负责检测棋盘上的匹配宝石（3个或更多相同类型的宝石连成一线）
    /// 支持水平、垂直、L型、T型等多种匹配模式
    /// </summary>
    public class MatchDetector
    {
        /// <summary>
        /// 最小匹配数（3个相同宝石为一个匹配）
        /// </summary>
        private const int MINIMUM_MATCH_LENGTH = 3;

        /// <summary>
        /// 查找所有匹配项
        /// 返回一个包含所有匹配组的列表，每个匹配组是一个宝石列表
        /// </summary>
        /// <param name="board">棋盘对象</param>
        /// <returns>所有匹配的宝石组列表</returns>
        public static List<List<Gem>> FindMatches(Board board)
        {
            if (board == null)
            {
                Debug.LogError("MatchDetector.FindMatches: Board is null!");
                return new List<List<Gem>>();
            }

            var allMatches = new List<List<Gem>>();

            // 查找水平匹配
            var horizontalMatches = FindHorizontalMatches(board);
            allMatches.AddRange(horizontalMatches);

            // 查找垂直匹配
            var verticalMatches = FindVerticalMatches(board);
            allMatches.AddRange(verticalMatches);

            // 移除重复的匹配（同一个宝石不应该同时被计算为水平和垂直匹配）
            allMatches = RemoveDuplicateMatches(allMatches);

            Debug.Log($"MatchDetector: Found {allMatches.Count} matches");

            return allMatches;
        }

        /// <summary>
        /// 查找所有水平匹配
        /// 逐行扫描，寻找连续的相同类型宝石
        /// </summary>
        /// <param name="board">棋盘对象</param>
        /// <returns>水平匹配的宝石组列表</returns>
        private static List<List<Gem>> FindHorizontalMatches(Board board)
        {
            var horizontalMatches = new List<List<Gem>>();

            // 遍历每一行
            for (int y = 0; y < board.Height; y++)
            {
                // 从每一行的第一列开始
                for (int x = 0; x < board.Width; )
                {
                    Gem currentGem = board.GetGem(x, y);

                    // 跳过空宝石
                    if (currentGem == null)
                    {
                        x++;
                        continue;
                    }

                    // 沿水平方向检查匹配
                    var matchGroup = CheckDirection(board, x, y, 1, 0); // 向右检查

                    // 如果找到有效匹配，添加到列表
                    if (matchGroup.Count >= MINIMUM_MATCH_LENGTH)
                    {
                        horizontalMatches.Add(matchGroup);
                        // 跳过已经匹配的宝石
                        x += matchGroup.Count;
                    }
                    else
                    {
                        x++;
                    }
                }
            }

            return horizontalMatches;
        }

        /// <summary>
        /// 查找所有垂直匹配
        /// 逐列扫描，寻找连续的相同类型宝石
        /// </summary>
        /// <param name="board">棋盘对象</param>
        /// <returns>垂直匹配的宝石组列表</returns>
        private static List<List<Gem>> FindVerticalMatches(Board board)
        {
            var verticalMatches = new List<List<Gem>>();

            // 遍历每一列
            for (int x = 0; x < board.Width; x++)
            {
                // 从每一列的第一行开始
                for (int y = 0; y < board.Height; )
                {
                    Gem currentGem = board.GetGem(x, y);

                    // 跳过空宝石
                    if (currentGem == null)
                    {
                        y++;
                        continue;
                    }

                    // 沿垂直方向检查匹配
                    var matchGroup = CheckDirection(board, x, y, 0, 1); // 向上检查

                    // 如果找到有效匹配，添加到列表
                    if (matchGroup.Count >= MINIMUM_MATCH_LENGTH)
                    {
                        verticalMatches.Add(matchGroup);
                        // 跳过已经匹配的宝石
                        y += matchGroup.Count;
                    }
                    else
                    {
                        y++;
                    }
                }
            }

            return verticalMatches;
        }

        /// <summary>
        /// 辅助方法：沿指定方向检查连续的相同宝石
        /// </summary>
        /// <param name="board">棋盘对象</param>
        /// <param name="startX">起始X坐标</param>
        /// <param name="startY">起始Y坐标</param>
        /// <param name="dirX">检查方向的X分量（-1, 0, 或 1）</param>
        /// <param name="dirY">检查方向的Y分量（-1, 0, 或 1）</param>
        /// <returns>沿该方向连续相同类型的宝石列表</returns>
        private static List<Gem> CheckDirection(Board board, int startX, int startY, int dirX, int dirY)
        {
            var matchGroup = new List<Gem>();

            // 获取起始位置的宝石
            Gem startGem = board.GetGem(startX, startY);

            if (startGem == null)
            {
                return matchGroup; // 返回空列表
            }

            // 记录起始宝石的类型
            GemType targetType = startGem.Type;

            // 沿指定方向遍历
            int currentX = startX;
            int currentY = startY;

            while (board.IsValidPosition(currentX, currentY))
            {
                Gem currentGem = board.GetGem(currentX, currentY);

                // 如果宝石为空或类型不同，停止检查
                if (currentGem == null || currentGem.Type != targetType)
                {
                    break;
                }

                // 添加宝石到匹配组
                matchGroup.Add(currentGem);

                // 沿方向继续移动
                currentX += dirX;
                currentY += dirY;
            }

            return matchGroup;
        }

        /// <summary>
        /// 移除重复的匹配
        /// 如果一个宝石已经在一个匹配组中，就不应该再出现在另一个匹配组中
        /// </summary>
        /// <param name="matches">包含所有匹配的列表</param>
        /// <returns>移除重复后的匹配列表</returns>
        private static List<List<Gem>> RemoveDuplicateMatches(List<List<Gem>> matches)
        {
            if (matches.Count == 0)
            {
                return matches;
            }

            var uniqueMatches = new List<List<Gem>>();
            var usedGems = new HashSet<Gem>();

            // 按匹配长度从长到短排序，优先保留更长的匹配
            var sortedMatches = matches.OrderByDescending(m => m.Count).ToList();

            foreach (var match in sortedMatches)
            {
                // 检查这个匹配组中是否有已经被使用过的宝石
                bool hasUsedGem = false;
                foreach (var gem in match)
                {
                    if (usedGems.Contains(gem))
                    {
                        hasUsedGem = true;
                        break;
                    }
                }

                // 如果没有重复，添加到结果列表
                if (!hasUsedGem)
                {
                    uniqueMatches.Add(match);

                    // 标记这些宝石为已使用
                    foreach (var gem in match)
                    {
                        usedGems.Add(gem);
                    }
                }
            }

            return uniqueMatches;
        }

        /// <summary>
        /// 检查是否存在任何匹配
        /// 便利方法，用于快速判断棋盘是否有可消除的匹配
        /// </summary>
        /// <param name="board">棋盘对象</param>
        /// <returns>如果存在匹配返回true，否则返回false</returns>
        public static bool HasMatches(Board board)
        {
            var matches = FindMatches(board);
            return matches.Count > 0;
        }

        /// <summary>
        /// 获取总匹配数
        /// 便利方法，用于统计匹配宝石的总数
        /// </summary>
        /// <param name="board">棋盘对象</param>
        /// <returns>所有匹配宝石的总数</returns>
        public static int GetTotalMatchCount(Board board)
        {
            var matches = FindMatches(board);
            int totalCount = 0;

            foreach (var match in matches)
            {
                totalCount += match.Count;
            }

            return totalCount;
        }

        /// <summary>
        /// 获取指定位置是否有宝石被匹配
        /// </summary>
        /// <param name="board">棋盘对象</param>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>如果该位置的宝石被匹配返回true，否则返回false</returns>
        public static bool IsGemMatched(Board board, int x, int y)
        {
            var matches = FindMatches(board);
            Gem targetGem = board.GetGem(x, y);

            if (targetGem == null)
            {
                return false;
            }

            foreach (var match in matches)
            {
                if (match.Contains(targetGem))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 调试用：打印匹配信息
        /// </summary>
        /// <param name="matches">匹配列表</param>
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

        // ==================== 增强匹配检测系统 ====================

        /// <summary>
        /// 增强的匹配检测 - 支持L型、T型、4连、5连等特殊匹配
        /// 返回包含匹配类型和优先级的MatchData列表
        /// </summary>
        public static List<MatchData> FindMatchesEnhanced(Board board)
        {
            if (board == null)
            {
                Debug.LogError("MatchDetector.FindMatchesEnhanced: Board is null!");
                return new List<MatchData>();
            }

            var allMatches = new List<MatchData>();

            // 1. 查找所有基础线性匹配（水平和垂直）
            var linearMatches = FindLinearMatchesEnhanced(board);
            allMatches.AddRange(linearMatches);

            // 2. 查找L型匹配
            var lShapeMatches = FindLShapeMatches(board);
            allMatches.AddRange(lShapeMatches);

            // 3. 查找T型和十字型匹配
            var tShapeMatches = FindTShapeMatches(board);
            allMatches.AddRange(tShapeMatches);

            // 4. 去重并按优先级排序
            allMatches = RemoveDuplicateMatchData(allMatches);
            allMatches = allMatches.OrderByDescending(m => m.Priority).ToList();

            Debug.Log($"MatchDetector Enhanced: Found {allMatches.Count} matches");

            return allMatches;
        }

        /// <summary>
        /// 查找所有线性匹配（水平和垂直），并标识4连、5连
        /// </summary>
        private static List<MatchData> FindLinearMatchesEnhanced(Board board)
        {
            var matches = new List<MatchData>();

            // 水平匹配
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width;)
                {
                    Gem currentGem = board.GetGem(x, y);
                    if (currentGem == null)
                    {
                        x++;
                        continue;
                    }

                    var matchGroup = CheckDirection(board, x, y, 1, 0);
                    if (matchGroup.Count >= MINIMUM_MATCH_LENGTH)
                    {
                        MatchType matchType = DetermineLinearMatchType(matchGroup.Count, true);
                        matches.Add(new MatchData(matchGroup, matchType, currentGem.Type));
                        x += matchGroup.Count;
                    }
                    else
                    {
                        x++;
                    }
                }
            }

            // 垂直匹配
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height;)
                {
                    Gem currentGem = board.GetGem(x, y);
                    if (currentGem == null)
                    {
                        y++;
                        continue;
                    }

                    var matchGroup = CheckDirection(board, x, y, 0, 1);
                    if (matchGroup.Count >= MINIMUM_MATCH_LENGTH)
                    {
                        MatchType matchType = DetermineLinearMatchType(matchGroup.Count, false);
                        matches.Add(new MatchData(matchGroup, matchType, currentGem.Type));
                        y += matchGroup.Count;
                    }
                    else
                    {
                        y++;
                    }
                }
            }

            return matches;
        }

        /// <summary>
        /// 根据匹配长度和方向确定线性匹配类型
        /// </summary>
        private static MatchType DetermineLinearMatchType(int length, bool isHorizontal)
        {
            if (length >= 5)
            {
                return isHorizontal ? MatchType.Horizontal5 : MatchType.Vertical5;
            }
            else if (length == 4)
            {
                return isHorizontal ? MatchType.Horizontal4 : MatchType.Vertical4;
            }
            else
            {
                return isHorizontal ? MatchType.Horizontal3 : MatchType.Vertical3;
            }
        }

        /// <summary>
        /// 查找L型匹配
        /// L型定义：一个3连在水平/垂直方向，在其端点有另一个2连在垂直/水平方向
        /// </summary>
        private static List<MatchData> FindLShapeMatches(Board board)
        {
            var lShapeMatches = new List<MatchData>();

            // 遍历每个位置，寻找可能的L型中心点
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    Gem centerGem = board.GetGem(x, y);
                    if (centerGem == null) continue;

                    GemType gemType = centerGem.Type;

                    // 检查4种L型配置：
                    // 1. 水平右 + 垂直上 (┘)
                    var lShape1 = CheckLShape(board, x, y, gemType, 1, 0, 0, 1);
                    if (lShape1 != null) lShapeMatches.Add(lShape1);

                    // 2. 水平右 + 垂直下 (┐)
                    var lShape2 = CheckLShape(board, x, y, gemType, 1, 0, 0, -1);
                    if (lShape2 != null) lShapeMatches.Add(lShape2);

                    // 3. 水平左 + 垂直上 (└)
                    var lShape3 = CheckLShape(board, x, y, gemType, -1, 0, 0, 1);
                    if (lShape3 != null) lShapeMatches.Add(lShape3);

                    // 4. 水平左 + 垂直下 (┌)
                    var lShape4 = CheckLShape(board, x, y, gemType, -1, 0, 0, -1);
                    if (lShape4 != null) lShapeMatches.Add(lShape4);
                }
            }

            return lShapeMatches;
        }

        /// <summary>
        /// 检查指定位置是否构成L型匹配
        /// </summary>
        private static MatchData CheckLShape(Board board, int x, int y, GemType gemType,
            int dir1X, int dir1Y, int dir2X, int dir2Y)
        {
            var gems = new List<Gem>();
            gems.Add(board.GetGem(x, y));

            // 检查第一个方向（至少需要2个相同宝石）
            int count1 = 1;
            for (int i = 1; i < 3; i++)
            {
                int checkX = x + dir1X * i;
                int checkY = y + dir1Y * i;
                if (!board.IsValidPosition(checkX, checkY)) break;

                Gem gem = board.GetGem(checkX, checkY);
                if (gem == null || gem.Type != gemType) break;

                gems.Add(gem);
                count1++;
            }

            // 检查第二个方向（至少需要2个相同宝石）
            int count2 = 1;
            for (int i = 1; i < 3; i++)
            {
                int checkX = x + dir2X * i;
                int checkY = y + dir2Y * i;
                if (!board.IsValidPosition(checkX, checkY)) break;

                Gem gem = board.GetGem(checkX, checkY);
                if (gem == null || gem.Type != gemType) break;

                gems.Add(gem);
                count2++;
            }

            // L型需要至少3+2的配置
            if ((count1 >= 3 && count2 >= 2) || (count1 >= 2 && count2 >= 3))
            {
                return new MatchData(gems, MatchType.LShape, gemType);
            }

            return null;
        }

        /// <summary>
        /// 查找T型和十字型匹配
        /// T型：一个3连在一个方向，中间点有另一个2连在垂直方向
        /// 十字型：水平和垂直都是3连，在中心点交叉
        /// </summary>
        private static List<MatchData> FindTShapeMatches(Board board)
        {
            var tShapeMatches = new List<MatchData>();

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    Gem centerGem = board.GetGem(x, y);
                    if (centerGem == null) continue;

                    GemType gemType = centerGem.Type;

                    // 检查十字型（水平3连 + 垂直3连）
                    var crossMatch = CheckCrossShape(board, x, y, gemType);
                    if (crossMatch != null)
                    {
                        tShapeMatches.Add(crossMatch);
                        continue; // 十字型优先级最高，找到后跳过T型检查
                    }

                    // 检查T型（4种方向）
                    var tMatch = CheckTShape(board, x, y, gemType);
                    if (tMatch != null) tShapeMatches.Add(tMatch);
                }
            }

            return tShapeMatches;
        }

        /// <summary>
        /// 检查十字型匹配
        /// </summary>
        private static MatchData CheckCrossShape(Board board, int x, int y, GemType gemType)
        {
            // 检查水平方向
            int horizCount = 1;
            var horizGems = new List<Gem> { board.GetGem(x, y) };

            // 向左
            for (int i = 1; i <= 2; i++)
            {
                if (!board.IsValidPosition(x - i, y)) break;
                Gem gem = board.GetGem(x - i, y);
                if (gem == null || gem.Type != gemType) break;
                horizGems.Add(gem);
                horizCount++;
            }

            // 向右
            for (int i = 1; i <= 2; i++)
            {
                if (!board.IsValidPosition(x + i, y)) break;
                Gem gem = board.GetGem(x + i, y);
                if (gem == null || gem.Type != gemType) break;
                horizGems.Add(gem);
                horizCount++;
            }

            // 检查垂直方向
            int vertCount = 1;
            var vertGems = new List<Gem> { board.GetGem(x, y) };

            // 向下
            for (int i = 1; i <= 2; i++)
            {
                if (!board.IsValidPosition(x, y - i)) break;
                Gem gem = board.GetGem(x, y - i);
                if (gem == null || gem.Type != gemType) break;
                vertGems.Add(gem);
                vertCount++;
            }

            // 向上
            for (int i = 1; i <= 2; i++)
            {
                if (!board.IsValidPosition(x, y + i)) break;
                Gem gem = board.GetGem(x, y + i);
                if (gem == null || gem.Type != gemType) break;
                vertGems.Add(gem);
                vertCount++;
            }

            // 十字型：水平和垂直都至少3连
            if (horizCount >= 3 && vertCount >= 3)
            {
                // 合并所有宝石（去重中心点）
                var allGems = new HashSet<Gem>(horizGems);
                foreach (var gem in vertGems)
                {
                    allGems.Add(gem);
                }

                return new MatchData(allGems.ToList(), MatchType.Cross, gemType);
            }

            return null;
        }

        /// <summary>
        /// 检查T型匹配
        /// </summary>
        private static MatchData CheckTShape(Board board, int x, int y, GemType gemType)
        {
            // T型需要一条主线（3连）和一条支线（2连）

            // 检查水平主线 + 垂直支线
            var horizLine = CheckDirection(board, x - 1, y, 1, 0);
            if (horizLine.Count >= 3 && horizLine.Contains(board.GetGem(x, y)))
            {
                var vertBranch = CheckVerticalBranch(board, x, y, gemType);
                if (vertBranch >= 2)
                {
                    var allGems = new HashSet<Gem>(horizLine);

                    // 添加垂直支线
                    for (int i = 1; i < vertBranch; i++)
                    {
                        if (board.IsValidPosition(x, y + i))
                        {
                            var gem = board.GetGem(x, y + i);
                            if (gem != null && gem.Type == gemType) allGems.Add(gem);
                        }
                        if (board.IsValidPosition(x, y - i))
                        {
                            var gem = board.GetGem(x, y - i);
                            if (gem != null && gem.Type == gemType) allGems.Add(gem);
                        }
                    }

                    return new MatchData(allGems.ToList(), MatchType.TShape, gemType);
                }
            }

            // 检查垂直主线 + 水平支线
            var vertLine = CheckDirection(board, x, y - 1, 0, 1);
            if (vertLine.Count >= 3 && vertLine.Contains(board.GetGem(x, y)))
            {
                var horizBranch = CheckHorizontalBranch(board, x, y, gemType);
                if (horizBranch >= 2)
                {
                    var allGems = new HashSet<Gem>(vertLine);

                    // 添加水平支线
                    for (int i = 1; i < horizBranch; i++)
                    {
                        if (board.IsValidPosition(x + i, y))
                        {
                            var gem = board.GetGem(x + i, y);
                            if (gem != null && gem.Type == gemType) allGems.Add(gem);
                        }
                        if (board.IsValidPosition(x - i, y))
                        {
                            var gem = board.GetGem(x - i, y);
                            if (gem != null && gem.Type == gemType) allGems.Add(gem);
                        }
                    }

                    return new MatchData(allGems.ToList(), MatchType.TShape, gemType);
                }
            }

            return null;
        }

        /// <summary>
        /// 检查垂直支线长度
        /// </summary>
        private static int CheckVerticalBranch(Board board, int x, int y, GemType gemType)
        {
            int count = 1; // 中心点

            // 向上
            for (int i = 1; i <= 2; i++)
            {
                if (!board.IsValidPosition(x, y + i)) break;
                Gem gem = board.GetGem(x, y + i);
                if (gem == null || gem.Type != gemType) break;
                count++;
            }

            // 向下
            for (int i = 1; i <= 2; i++)
            {
                if (!board.IsValidPosition(x, y - i)) break;
                Gem gem = board.GetGem(x, y - i);
                if (gem == null || gem.Type != gemType) break;
                count++;
            }

            return count;
        }

        /// <summary>
        /// 检查水平支线长度
        /// </summary>
        private static int CheckHorizontalBranch(Board board, int x, int y, GemType gemType)
        {
            int count = 1; // 中心点

            // 向右
            for (int i = 1; i <= 2; i++)
            {
                if (!board.IsValidPosition(x + i, y)) break;
                Gem gem = board.GetGem(x + i, y);
                if (gem == null || gem.Type != gemType) break;
                count++;
            }

            // 向左
            for (int i = 1; i <= 2; i++)
            {
                if (!board.IsValidPosition(x - i, y)) break;
                Gem gem = board.GetGem(x - i, y);
                if (gem == null || gem.Type != gemType) break;
                count++;
            }

            return count;
        }

        /// <summary>
        /// 移除重复的MatchData（优先保留高优先级匹配）
        /// </summary>
        private static List<MatchData> RemoveDuplicateMatchData(List<MatchData> matches)
        {
            if (matches.Count == 0) return matches;

            var uniqueMatches = new List<MatchData>();
            var usedGems = new HashSet<Gem>();

            // 按优先级排序
            var sortedMatches = matches.OrderByDescending(m => m.Priority).ToList();

            foreach (var match in sortedMatches)
            {
                // 检查是否有重复宝石
                bool hasUsedGem = match.Gems.Any(gem => usedGems.Contains(gem));

                if (!hasUsedGem)
                {
                    uniqueMatches.Add(match);
                    foreach (var gem in match.Gems)
                    {
                        usedGems.Add(gem);
                    }
                }
            }

            return uniqueMatches;
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
