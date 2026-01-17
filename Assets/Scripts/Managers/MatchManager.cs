using UnityEngine;
using System.Collections.Generic;
using RetroMatch2D.Core;

namespace RetroMatch2D.Managers
{
    /// <summary>
    /// 匹配管理器
    /// 管理游戏中的匹配检测、消除和分数计算
    /// </summary>
    public class MatchManager : MonoBehaviour
    {
        [SerializeField]
        private Board board;

        [SerializeField]
        private int baseScorePerGem = 100;

        [SerializeField]
        private int bonusScorePerExtraGem = 50;

        [SerializeField]
        private float matchAnimationDuration = 0.5f;

        // 事件声明
        public event System.Action<List<List<Gem>>> OnMatchFound;
        public event System.Action<List<MatchData>> OnMatchFoundEnhanced; // 新增：增强匹配事件
        public event System.Action<int> OnScoreEarned;
        public event System.Action OnNoMatches;

        private int currentScore = 0;
        private List<MatchData> lastMatchResults = new List<MatchData>(); // 缓存最近的匹配结果

        public int CurrentScore => currentScore;
        public List<MatchData> LastMatchResults => lastMatchResults;

        private void Start()
        {
            if (board == null)
            {
                Debug.LogError("MatchManager: Board not assigned!");
            }
        }

        /// <summary>
        /// 检查并处理棋盘上的所有匹配（使用增强匹配系统）
        /// </summary>
        /// <returns>是否找到了匹配</returns>
        public bool CheckAndHandleMatches()
        {
            if (board == null)
            {
                return false;
            }

            // 使用增强匹配系统
            var enhancedMatches = MatchDetector.FindMatchesEnhanced(board);

            if (enhancedMatches.Count == 0)
            {
                OnNoMatches?.Invoke();
                lastMatchResults.Clear();
                return false;
            }

            // 缓存匹配结果
            lastMatchResults = enhancedMatches;

            // 触发增强匹配事件
            OnMatchFoundEnhanced?.Invoke(enhancedMatches);

            // 为了向后兼容，转换并触发旧事件
            var legacyMatches = ConvertToLegacyFormat(enhancedMatches);
            OnMatchFound?.Invoke(legacyMatches);

            // 计算分数（使用增强计算）
            int earnedScore = CalculateScoreEnhanced(enhancedMatches);
            currentScore += earnedScore;
            OnScoreEarned?.Invoke(earnedScore);

            Debug.Log($"MatchManager Enhanced: Found {enhancedMatches.Count} match groups, earned {earnedScore} score");
            MatchDetector.DebugPrintMatchesEnhanced(enhancedMatches);

            return true;
        }

        /// <summary>
        /// 计算匹配带来的分数（旧版本，保留兼容性）
        /// </summary>
        /// <param name="matches">匹配组列表</param>
        /// <returns>总分数</returns>
        public int CalculateScore(List<List<Gem>> matches)
        {
            int score = 0;

            foreach (var match in matches)
            {
                // 基础分数：每个宝石baseScorePerGem分
                int matchScore = match.Count * baseScorePerGem;

                // 奖励分数：超过3个宝石的部分，每个多bonus分
                if (match.Count > 3)
                {
                    int extraGems = match.Count - 3;
                    matchScore += extraGems * bonusScorePerExtraGem;
                }

                score += matchScore;

                Debug.Log($"Match score: {match.Count} gems = {matchScore} points");
            }

            return score;
        }

        /// <summary>
        /// 计算增强匹配带来的分数（考虑匹配类型优先级）
        /// </summary>
        /// <param name="matches">增强匹配数据列表</param>
        /// <returns>总分数</returns>
        private int CalculateScoreEnhanced(List<MatchData> matches)
        {
            int score = 0;

            foreach (var match in matches)
            {
                // 基础分数：每个宝石baseScorePerGem分
                int matchScore = match.Gems.Count * baseScorePerGem;

                // 奖励分数：超过3个宝石的部分，每个多bonus分
                if (match.Gems.Count > 3)
                {
                    int extraGems = match.Gems.Count - 3;
                    matchScore += extraGems * bonusScorePerExtraGem;
                }

                // 特殊形状额外加分
                int shapeBonus = GetShapeBonus(match.Type);
                matchScore += shapeBonus;

                score += matchScore;

                Debug.Log($"Enhanced Match score: Type={match.Type}, Gems={match.Gems.Count}, " +
                         $"BaseScore={match.Gems.Count * baseScorePerGem}, ShapeBonus={shapeBonus}, Total={matchScore}");
            }

            return score;
        }

        /// <summary>
        /// 根据匹配类型获取额外分数
        /// </summary>
        private int GetShapeBonus(MatchType matchType)
        {
            switch (matchType)
            {
                case MatchType.Cross:
                    return 500; // 十字型最高奖励
                case MatchType.TShape:
                    return 300; // T型高奖励
                case MatchType.LShape:
                    return 200; // L型中等奖励
                case MatchType.Horizontal5:
                case MatchType.Vertical5:
                    return 150; // 5连奖励
                case MatchType.Horizontal4:
                case MatchType.Vertical4:
                    return 100; // 4连奖励
                default:
                    return 0; // 普通3连无额外奖励
            }
        }

        /// <summary>
        /// 将增强匹配数据转换为旧格式（向后兼容）
        /// </summary>
        private List<List<Gem>> ConvertToLegacyFormat(List<MatchData> enhancedMatches)
        {
            var legacyMatches = new List<List<Gem>>();
            foreach (var match in enhancedMatches)
            {
                legacyMatches.Add(match.Gems);
            }
            return legacyMatches;
        }

        /// <summary>
        /// 获取所有匹配的宝石（使用增强匹配系统）
        /// </summary>
        /// <returns>所有被匹配的宝石列表</returns>
        public List<Gem> GetAllMatchedGems()
        {
            var enhancedMatches = MatchDetector.FindMatchesEnhanced(board);
            var matchedGems = new List<Gem>();

            foreach (var match in enhancedMatches)
            {
                matchedGems.AddRange(match.Gems);
            }

            return matchedGems;
        }

        /// <summary>
        /// 消除所有匹配的宝石
        /// </summary>
        public void ClearMatchedGems()
        {
            var matchedGems = GetAllMatchedGems();

            Debug.Log($"MatchManager: 开始消除 {matchedGems.Count} 个匹配的宝石");

            foreach (var gem in matchedGems)
            {
                if (gem != null)
                {
                    int x = gem.GridPosition.x;
                    int y = gem.GridPosition.y;

                    // 从棋盘数据结构中移除
                    board.SetGem(x, y, null);

                    // 播放消除动画并销毁
                    gem.DestroyWithAnimation();

                    Debug.Log($"MatchManager: 消除宝石 {gem.Type} 位于 ({x}, {y})");
                }
            }
        }

        /// <summary>
        /// 验证交换是否会产生匹配
        /// </summary>
        /// <param name="gem1">第一个宝石</param>
        /// <param name="gem2">第二个宝石</param>
        /// <returns>交换后是否会产生匹配</returns>
        public bool ValidateSwap(Gem gem1, Gem gem2)
        {
            if (gem1 == null || gem2 == null || board == null)
            {
                return false;
            }

            // 暂存原始位置
            int x1 = gem1.GridPosition.x;
            int y1 = gem1.GridPosition.y;
            int x2 = gem2.GridPosition.x;
            int y2 = gem2.GridPosition.y;

            // 执行交换
            board.SetGem(x1, y1, gem2);
            board.SetGem(x2, y2, gem1);
            gem1.SetPosition(x2, y2);
            gem2.SetPosition(x1, y1);

            // 检查是否产生匹配
            bool hasMatches = MatchDetector.HasMatches(board);

            // 撤销交换
            board.SetGem(x1, y1, gem1);
            board.SetGem(x2, y2, gem2);
            gem1.SetPosition(x1, y1);
            gem2.SetPosition(x2, y2);

            return hasMatches;
        }

        /// <summary>
        /// 重置分数
        /// </summary>
        public void ResetScore()
        {
            currentScore = 0;
            Debug.Log("MatchManager: Score reset to 0");
        }

        /// <summary>
        /// 获取特定宝石的匹配信息（增强版本）
        /// </summary>
        /// <param name="gem">宝石对象</param>
        /// <returns>该宝石所在的匹配数据，如果没有匹配返回null</returns>
        public MatchData GetMatchDataForGem(Gem gem)
        {
            if (gem == null || board == null)
            {
                return null;
            }

            var enhancedMatches = MatchDetector.FindMatchesEnhanced(board);

            foreach (var match in enhancedMatches)
            {
                if (match.Gems.Contains(gem))
                {
                    return match;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取特定宝石的匹配信息（旧版本，保留兼容性）
        /// </summary>
        /// <param name="gem">宝石对象</param>
        /// <returns>该宝石所在的匹配组，如果没有匹配返回null</returns>
        public List<Gem> GetMatchForGem(Gem gem)
        {
            var matchData = GetMatchDataForGem(gem);
            return matchData?.Gems;
        }

        /// <summary>
        /// 记录分数
        /// </summary>
        /// <returns>当前累计分数</returns>
        public int AddScore(int amount)
        {
            currentScore += amount;
            return currentScore;
        }

        /// <summary>
        /// 调试：打印当前所有匹配（增强版本）
        /// </summary>
        public void DebugPrintMatches()
        {
            var enhancedMatches = MatchDetector.FindMatchesEnhanced(board);
            MatchDetector.DebugPrintMatchesEnhanced(enhancedMatches);
        }
    }
}
