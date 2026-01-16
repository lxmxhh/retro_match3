using UnityEngine;

namespace RetroMatch2D.Core
{
    /// <summary>
    /// 棋盘管理类，负责管理棋盘上的所有宝石对象
    /// </summary>
    public class Board : MonoBehaviour
    {
        [SerializeField]
        private int width = 8;

        [SerializeField]
        private int height = 8;

        [SerializeField]
        private Vector3 boardOffset = Vector3.zero;

        private Gem[,] gems;

        /// <summary>
        /// 获取棋盘宽度
        /// </summary>
        public int Width => width;

        /// <summary>
        /// 获取棋盘高度
        /// </summary>
        public int Height => height;

        /// <summary>
        /// 获取棋盘的世界坐标偏移量
        /// </summary>
        public Vector3 BoardOffset => boardOffset;

        private void Awake()
        {
            InitializeBoard();
        }

        /// <summary>
        /// 初始化棋盘，创建宝石数组
        /// </summary>
        public void InitializeBoard()
        {
            gems = new Gem[width, height];
            Debug.Log($"Board initialized: {width}x{height}");
        }

        /// <summary>
        /// 获取指定位置的宝石
        /// </summary>
        /// <param name="x">X坐标（列）</param>
        /// <param name="y">Y坐标（行）</param>
        /// <returns>宝石对象，如果位置无效或没有宝石返回null</returns>
        public Gem GetGem(int x, int y)
        {
            if (!IsValidPosition(x, y))
            {
                Debug.LogWarning($"Invalid position: ({x}, {y})");
                return null;
            }

            return gems[x, y];
        }

        /// <summary>
        /// 在指定位置设置宝石
        /// </summary>
        /// <param name="x">X坐标（列）</param>
        /// <param name="y">Y坐标（行）</param>
        /// <param name="gem">要设置的宝石对象</param>
        public void SetGem(int x, int y, Gem gem)
        {
            if (!IsValidPosition(x, y))
            {
                Debug.LogWarning($"Invalid position: ({x}, {y})");
                return;
            }

            gems[x, y] = gem;
        }

        /// <summary>
        /// 检查坐标是否有效
        /// </summary>
        /// <param name="x">X坐标（列）</param>
        /// <param name="y">Y坐标（行）</param>
        /// <returns>如果坐标在棋盘范围内返回true，否则返回false</returns>
        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        /// <summary>
        /// 清空棋盘上的所有宝石
        /// </summary>
        public void ClearBoard()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    gems[x, y] = null;
                }
            }
        }

        /// <summary>
        /// 获取棋盘上的所有宝石
        /// </summary>
        /// <returns>包含所有宝石的数组</returns>
        public Gem[,] GetAllGems()
        {
            return gems;
        }
    }
}
