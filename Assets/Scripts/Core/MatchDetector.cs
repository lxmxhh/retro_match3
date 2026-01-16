using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace RetroMatch2D.Core
{
    /// <summary>
    /// 匹配检测系统
    /// 负责检测棋盘上的匹配宝石（3个或更多相同类型的宝石连成一线）
    /// 支持水平和垂直方向的匹配检测
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
    }
}
