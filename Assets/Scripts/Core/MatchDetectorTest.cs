using UnityEngine;
using System.Collections.Generic;

namespace RetroMatch2D.Core
{
    /// <summary>
    /// 匹配检测系统的测试脚本
    /// 用于验证MatchDetector的各项功能
    /// </summary>
    public class MatchDetectorTest : MonoBehaviour
    {
        [SerializeField]
        private Board board;

        [SerializeField]
        private bool runTestOnStart = true;

        private void Start()
        {
            if (runTestOnStart && board != null)
            {
                RunTests();
            }
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public void RunTests()
        {
            Debug.Log("========== MatchDetector Tests Start ==========");

            TestFindMatches();
            TestHasMatches();
            TestGetTotalMatchCount();
            TestIsGemMatched();

            Debug.Log("========== MatchDetector Tests End ==========");
        }

        /// <summary>
        /// 测试基本的匹配查找功能
        /// </summary>
        private void TestFindMatches()
        {
            Debug.Log("\n--- Test: FindMatches ---");

            var matches = MatchDetector.FindMatches(board);

            if (matches != null)
            {
                Debug.Log($"FindMatches Test PASSED: Found {matches.Count} matches");
                MatchDetector.DebugPrintMatches(matches);
            }
            else
            {
                Debug.LogError("FindMatches Test FAILED: Returned null");
            }
        }

        /// <summary>
        /// 测试检查是否有匹配的功能
        /// </summary>
        private void TestHasMatches()
        {
            Debug.Log("\n--- Test: HasMatches ---");

            bool hasMatches = MatchDetector.HasMatches(board);
            Debug.Log($"HasMatches Test PASSED: HasMatches = {hasMatches}");
        }

        /// <summary>
        /// 测试获取总匹配数的功能
        /// </summary>
        private void TestGetTotalMatchCount()
        {
            Debug.Log("\n--- Test: GetTotalMatchCount ---");

            int totalCount = MatchDetector.GetTotalMatchCount(board);
            Debug.Log($"GetTotalMatchCount Test PASSED: Total matched gems = {totalCount}");
        }

        /// <summary>
        /// 测试检查特定宝石是否被匹配的功能
        /// </summary>
        private void TestIsGemMatched()
        {
            Debug.Log("\n--- Test: IsGemMatched ---");

            var matches = MatchDetector.FindMatches(board);

            if (matches.Count > 0)
            {
                var firstMatchGem = matches[0][0];
                int x = firstMatchGem.GridPosition.x;
                int y = firstMatchGem.GridPosition.y;

                bool isMatched = MatchDetector.IsGemMatched(board, x, y);
                Debug.Log($"IsGemMatched Test PASSED: Gem at ({x}, {y}) is matched = {isMatched}");
            }
            else
            {
                Debug.Log("IsGemMatched Test SKIPPED: No matches found");
            }
        }

        /// <summary>
        /// 从编辑器调用的测试方法
        /// </summary>
        public void OnTestButtonClick()
        {
            if (board == null)
            {
                Debug.LogError("Board not assigned!");
                return;
            }

            RunTests();
        }
    }
}
