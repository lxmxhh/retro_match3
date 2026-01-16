using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RetroMatch2D.Managers;

namespace RetroMatch2D.Core
{
    /// <summary>
    /// 下落系统 - 处理宝石下落逻辑
    /// 功能：
    /// - 检测并填充空位
    /// - 检测宝石是否需要下落
    /// - 统计棋盘空位
    /// </summary>
    public class FallSystem : MonoBehaviour
    {
        [SerializeField]
        private float fallDuration = 0.3f; // 下落动画时长

        private GemGrid gemGrid; // GemGrid 引用

        private void Start()
        {
            // 自动获取 GemGrid 实例
            gemGrid = GemGrid.Instance;
            if (gemGrid == null)
            {
                Debug.LogError("FallSystem: 无法找到 GemGrid 实例!");
            }
        }

        /// <summary>
        /// 应用宝石下落动画（协程版本）
        /// 让上方的宝石下落到空位，带平滑动画
        /// </summary>
        /// <param name="board">游戏棋盘</param>
        /// <returns>协程</returns>
        public IEnumerator ApplyFallWithAnimation(Board board)
        {
            if (board == null)
            {
                Debug.LogError("FallSystem: Board 为空!");
                yield break;
            }

            if (gemGrid == null)
            {
                gemGrid = GemGrid.Instance;
                if (gemGrid == null)
                {
                    Debug.LogError("FallSystem: 无法找到 GemGrid 实例!");
                    yield break;
                }
            }

            bool hasMovement = false;

            // 遍历每一列
            for (int col = 0; col < board.Width; col++)
            {
                // 收集该列所有非空宝石
                List<Gem> columnGems = new List<Gem>();
                for (int row = 0; row < board.Height; row++)
                {
                    Gem gem = board.GetGem(col, row);
                    if (gem != null)
                    {
                        columnGems.Add(gem);
                        // 先从棋盘中清除
                        board.SetGem(col, row, null);
                    }
                }

                // 将宝石重新放置到底部，从下往上填充
                for (int i = 0; i < columnGems.Count; i++)
                {
                    Gem gem = columnGems[i];
                    int targetRow = i; // 从底部开始填充

                    // 获取原始位置
                    Vector2Int oldPos = gem.GridPosition;

                    // 只有位置改变时才播放动画
                    if (oldPos.y != targetRow)
                    {
                        // 更新棋盘数据
                        board.SetGem(col, targetRow, gem);

                        // 更新宝石的网格位置
                        gem.SetGridPosition(new Vector2Int(col, targetRow));

                        // 计算目标世界坐标
                        Vector3 targetPos = gemGrid.GridToWorldPosition(col, targetRow);

                        // 播放下落动画
                        gem.MoveTo(targetPos, fallDuration);

                        hasMovement = true;

                        Debug.Log($"FallSystem: 宝石 {gem.Type} 从 ({col}, {oldPos.y}) 下落到 ({col}, {targetRow})");
                    }
                    else
                    {
                        // 位置没变，直接放回棋盘
                        board.SetGem(col, targetRow, gem);
                    }
                }
            }

            if (hasMovement)
            {
                // 等待所有下落动画完成
                yield return new WaitForSeconds(fallDuration);
                Debug.Log("FallSystem: 所有宝石下落完成");
            }
        }

        /// <summary>
        /// 检测并填充所有空位
        /// </summary>
        /// <param name="board">游戏棋盘</param>
        /// <returns>是否有宝石下落</returns>
        public bool FillEmptySpaces(Board board)
        {
            if (board == null)
            {
                Debug.LogError("Board为空，无法填充空位");
                return false;
            }

            bool hasMovement = false;

            // 遍历每一列
            for (int col = 0; col < board.Width; col++)
            {
                hasMovement |= ProcessColumn(board, col);
            }

            return hasMovement;
        }

        /// <summary>
        /// 处理单列的空位填充和下落
        /// </summary>
        /// <param name="board">游戏棋盘</param>
        /// <param name="column">列索引</param>
        /// <returns>是否有宝石下落</returns>
        private bool ProcessColumn(Board board, int column)
        {
            bool hasMovement = false;
            List<Gem> gemsToFall = new List<Gem>();

            // 从下往上扫描该列
            for (int row = 0; row < board.Height; row++)
            {
                Gem gem = board.GetGem(column, row);

                // 发现空位
                if (gem == null)
                {
                    // 查找上方的宝石
                    for (int searchRow = row + 1; searchRow < board.Height; searchRow++)
                    {
                        Gem gemAbove = board.GetGem(column, searchRow);
                        if (gemAbove != null)
                        {
                            gemsToFall.Add(gemAbove);
                            board.SetGem(column, searchRow, null);
                        }
                    }

                    // 将收集的宝石依次放置在这个空位及以下
                    for (int i = 0; i < gemsToFall.Count; i++)
                    {
                        board.SetGem(column, row + i, gemsToFall[i]);
                        hasMovement = true;
                    }

                    gemsToFall.Clear();
                }
            }

            return hasMovement;
        }

        /// <summary>
        /// 检测宝石是否需要下落
        /// </summary>
        /// <param name="board">游戏棋盘</param>
        /// <returns>是否有宝石需要下落</returns>
        public bool HasGemsFalling(Board board)
        {
            if (board == null)
            {
                Debug.LogError("Board为空，无法检测下落");
                return false;
            }

            // 遍历每一列检测是否有空位
            for (int col = 0; col < board.Width; col++)
            {
                for (int row = 0; row < board.Height - 1; row++)
                {
                    Gem gem = board.GetGem(col, row);
                    Gem gemBelow = board.GetGem(col, row - 1);

                    // 如果当前位置有宝石，下方没有宝石，则需要下落
                    if (gem != null && gemBelow == null && row > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 统计棋盘中的空位数量
        /// </summary>
        /// <param name="board">游戏棋盘</param>
        /// <returns>空位数量</returns>
        public int CountEmptySpaces(Board board)
        {
            if (board == null)
            {
                return 0;
            }

            int emptyCount = 0;
            for (int col = 0; col < board.Width; col++)
            {
                for (int row = 0; row < board.Height; row++)
                {
                    if (board.GetGem(col, row) == null)
                    {
                        emptyCount++;
                    }
                }
            }

            return emptyCount;
        }
    }
}
