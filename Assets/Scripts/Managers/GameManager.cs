using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RetroMatch2D.Core;

namespace RetroMatch2D.Managers
{
    /// <summary>
    /// 游戏管理器 - 游戏的核心控制器
    /// 负责游戏流程、状态管理和各个系统的协调
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Singleton Pattern
        
        private static GameManager _instance;
        
        /// <summary>
        /// 获取游戏管理器单例实例
        /// </summary>
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                    }
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Game State Enum
        
        /// <summary>
        /// 游戏状态枚举
        /// </summary>
        public enum GameState
        {
            /// <summary>游戏空闲状态，等待玩家输入</summary>
            Idle = 0,
            
            /// <summary>正在交换宝石</summary>
            Swapping = 1,
            
            /// <summary>正在检测和消除匹配</summary>
            Matching = 2,
            
            /// <summary>宝石正在下落并填充空位</summary>
            Falling = 3
        }
        
        #endregion
        
        #region Fields

        // 其他管理器和控制器引用
        [SerializeField]
        private Board _board;

        [SerializeField]
        private MatchManager _matchManager;

        [SerializeField]
        private InputController _inputController;

        [SerializeField]
        private GemGrid _gemGrid;

        // 宝石Sprite配置
        [Header("宝石Sprite配置")]
        [SerializeField]
        private Sprite _redGemSprite;

        [SerializeField]
        private Sprite _blueGemSprite;

        [SerializeField]
        private Sprite _greenGemSprite;

        [SerializeField]
        private Sprite _yellowGemSprite;

        [SerializeField]
        private Sprite _purpleGemSprite;

        [SerializeField]
        private Sprite _orangeGemSprite;

        // 游戏状态
        private GameState _currentState = GameState.Idle;
        
        // 游戏设置
        [SerializeField]
        private int _boardWidth = 8;
        
        [SerializeField]
        private int _boardHeight = 8;
        
        [SerializeField]
        private float _swapDuration = 0.3f;
        
        [SerializeField]
        private float _matchCheckDelay = 0.5f;
        
        [SerializeField]
        private float _fallDuration = 0.3f;
        
        // 游戏运行状态
        private bool _isGameRunning = false;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// 当前游戏状态
        /// </summary>
        public GameState CurrentState
        {
            get { return _currentState; }
            private set { _currentState = value; }
        }
        
        /// <summary>
        /// 游戏是否运行中
        /// </summary>
        public bool IsGameRunning
        {
            get { return _isGameRunning; }
        }
        
        /// <summary>
        /// 棋盘宽度
        /// </summary>
        public int BoardWidth
        {
            get { return _boardWidth; }
        }
        
        /// <summary>
        /// 棋盘高度
        /// </summary>
        public int BoardHeight
        {
            get { return _boardHeight; }
        }
        
        #endregion
        
        #region Lifecycle
        
        private void Awake()
        {
            // 确保单例唯一性
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        private void Start()
        {
            // 验证所有引用
            if (!ValidateReferences())
            {
                Debug.LogError("GameManager: 缺少必要的管理器引用!");
                enabled = false;
                return;
            }
            
            // 初始化游戏
            InitializeGame();
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// 验证所有必要的引用是否已设置
        /// </summary>
        private bool ValidateReferences()
        {
            if (_board == null)
            {
                Debug.LogError("GameManager: Board 未被分配!");
                return false;
            }
            
            if (_matchManager == null)
            {
                Debug.LogError("GameManager: MatchManager 未被分配!");
                return false;
            }
            
            if (_inputController == null)
            {
                Debug.LogError("GameManager: InputController 未被分配!");
                return false;
            }
            
            if (_gemGrid == null)
            {
                Debug.LogError("GameManager: GemGrid 未被分配!");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 初始化游戏棋盘和游戏状态
        /// </summary>
        public void InitializeGame()
        {
            Debug.Log("GameManager: 初始化游戏...");

            // 重置游戏状态
            _currentState = GameState.Idle;
            _isGameRunning = false;

            // 清空棋盘
            _board.ClearBoard();

            // 填充整个棋盘
            FillBoard();

            // 标记游戏运行
            _isGameRunning = true;

            // 启动游戏主循环
            StartCoroutine(GameLoop());

            Debug.Log("GameManager: 游戏初始化完成! 等待玩家输入...");
        }
        
        #endregion
        
        #region Game Loop
        
        /// <summary>
        /// 游戏主循环协程
        /// 管理游戏的基本流程：等待输入 -> 交换 -> 匹配 -> 消除 -> 下落 -> 循环
        /// </summary>
        private IEnumerator GameLoop()
        {
            while (_isGameRunning)
            {
                // 状态：空闲，等待玩家输入
                if (_currentState == GameState.Idle)
                {
                    // 注册输入事件
                    _inputController.OnSwapRequested += HandleSwap;
                    yield return null;
                }
                // 状态：执行交换
                else if (_currentState == GameState.Swapping)
                {
                    yield return new WaitForSeconds(_swapDuration);
                    
                    // 交换完成后，转移到匹配状态
                    _currentState = GameState.Matching;
                }
                // 状态：检测匹配
                else if (_currentState == GameState.Matching)
                {
                    yield return new WaitForSeconds(_matchCheckDelay);

                    // 检测匹配的宝石
                    bool hasMatches = _matchManager.CheckAndHandleMatches();

                    if (hasMatches)
                    {
                        // 消除匹配的宝石
                        _matchManager.ClearMatchedGems();

                        // 等待消除动画完成
                        yield return new WaitForSeconds(0.5f);

                        // 转移到下落状态
                        _currentState = GameState.Falling;
                    }
                    else
                    {
                        // 没有匹配，返回空闲状态
                        _currentState = GameState.Idle;
                    }
                }
                // 状态：宝石下落和填充
                else if (_currentState == GameState.Falling)
                {
                    // 获取 FallSystem 组件
                    FallSystem fallSystem = GetComponent<FallSystem>();

                    if (fallSystem != null)
                    {
                        // 让现有宝石下落（带动画）
                        yield return StartCoroutine(fallSystem.ApplyFallWithAnimation(_board));
                    }

                    // 填充剩余的空位（生成新宝石）
                    FillBoard();

                    // 等待新宝石生成
                    yield return new WaitForSeconds(0.3f);

                    // 检查是否还有匹配（连锁消除）
                    _currentState = GameState.Matching;
                }
                
                yield return null;
            }
        }
        
        #endregion
        
        #region Gem Swap Handling
        
        /// <summary>
        /// 处理宝石交换
        /// 当玩家尝试交换两个宝石时调用
        /// </summary>
        /// <param name="gem1">第一个宝石</param>
        /// <param name="gem2">第二个宝石</param>
        public void HandleSwap(Gem gem1, Gem gem2)
        {
            // 只有在空闲状态才能执行交换
            if (_currentState != GameState.Idle)
            {
                Debug.LogWarning("GameManager: 当前状态不允许交换!");
                return;
            }
            
            // 验证两个宝石都有效
            if (gem1 == null || gem2 == null)
            {
                Debug.LogWarning("GameManager: 无效的宝石引用!");
                return;
            }
            
            // 验证两个宝石相邻
            if (!AreGemsAdjacent(gem1, gem2))
            {
                Debug.LogWarning("GameManager: 宝石不相邻，无法交换!");
                return;
            }
            
            // 取消输入事件注册
            _inputController.OnSwapRequested -= HandleSwap;
            
            // 转移到交换状态
            _currentState = GameState.Swapping;
            
            // 执行交换动画和逻辑
            StartCoroutine(PerformSwap(gem1, gem2));
        }
        
        /// <summary>
        /// 验证两个宝石是否相邻（上下左右）
        /// </summary>
        private bool AreGemsAdjacent(Gem gem1, Gem gem2)
        {
            Vector2Int pos1 = gem1.GridPosition;
            Vector2Int pos2 = gem2.GridPosition;
            
            int dx = Mathf.Abs(pos1.x - pos2.x);
            int dy = Mathf.Abs(pos1.y - pos2.y);
            
            // 相邻条件：一个坐标相同，另一个坐标相差1
            return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
        }
        
        /// <summary>
        /// 执行交换动画和逻辑的协程
        /// </summary>
        private IEnumerator PerformSwap(Gem gem1, Gem gem2)
        {
            // 存储原始位置
            Vector2Int pos1 = gem1.GridPosition;
            Vector2Int pos2 = gem2.GridPosition;

            // 获取目标位置
            Vector3 targetPos1 = gem2.transform.position;
            Vector3 targetPos2 = gem1.transform.position;

            // 使用弹性动画播放交换效果
            gem1.MoveTo(targetPos1, _swapDuration, Gem.AnimationType.EaseOutElastic);
            gem2.MoveTo(targetPos2, _swapDuration, Gem.AnimationType.EaseOutElastic);

            // 等待交换动画完成
            yield return new WaitForSeconds(_swapDuration);

            // 在棋盘中交换宝石
            _board.SetGem(pos1.x, pos1.y, gem2);
            _board.SetGem(pos2.x, pos2.y, gem1);

            // 更新宝石的网格位置
            gem1.SetGridPosition(pos2);
            gem2.SetGridPosition(pos1);
        }
        
        #endregion
        
        #region Board Management
        
        /// <summary>
        /// 填充整个棋盘
        /// 为所有空位生成新的宝石
        /// </summary>
        public void FillBoard()
        {
            for (int x = 0; x < _boardWidth; x++)
            {
                for (int y = 0; y < _boardHeight; y++)
                {
                    if (_board.GetGem(x, y) == null)
                    {
                        // 生成新宝石
                        GemType randomType = GetRandomGemType();
                        SpawnGem(x, y, randomType);
                    }
                }
            }
        }
        
        /// <summary>
        /// 在指定位置生成宝石
        /// </summary>
        /// <param name="x">X坐标（列）</param>
        /// <param name="y">Y坐标（行）</param>
        /// <param name="type">宝石类型</param>
        public void SpawnGem(int x, int y, GemType type)
        {
            // 验证坐标有效性
            if (!IsValidBoardPosition(x, y))
            {
                Debug.LogWarning($"GameManager: 无效的棋盘位置 ({x}, {y})!");
                return;
            }
            
            // 创建新宝石GameObject
            GameObject gemObj = new GameObject($"Gem_{type}_{x}_{y}");
            gemObj.transform.SetParent(_gemGrid.transform);

            // 添加必要的组件
            SpriteRenderer spriteRenderer = gemObj.AddComponent<SpriteRenderer>();
            Gem newGem = gemObj.AddComponent<Gem>();

            // 获取并设置Sprite
            Sprite gemSprite = GetSpriteForGemType(type);
            if (gemSprite != null)
            {
                spriteRenderer.sprite = gemSprite;
            }
            else
            {
                Debug.LogWarning($"GameManager: 宝石类型 {type} 的Sprite未配置，将使用纯色显示");
            }

            // 初始化宝石
            newGem.Initialize(type, x, y);

            // 设置世界位置
            Vector3 worldPos = _gemGrid.GridToWorldPosition(x, y);
            gemObj.transform.position = worldPos;

            // 将宝石添加到棋盘
            _board.SetGem(x, y, newGem);

            Debug.Log($"GameManager: 在 ({x}, {y}) 生成了 {type} 类型的宝石");
        }
        
        /// <summary>
        /// 验证棋盘位置是否有效
        /// </summary>
        private bool IsValidBoardPosition(int x, int y)
        {
            return x >= 0 && x < _boardWidth && y >= 0 && y < _boardHeight;
        }
        
        /// <summary>
        /// 获取随机的宝石类型
        /// </summary>
        private GemType GetRandomGemType()
        {
            // 假设宝石类型为 Red, Blue, Green, Yellow, Purple
            GemType[] availableTypes = new GemType[]
            {
                GemType.Red,
                GemType.Blue,
                GemType.Green,
                GemType.Yellow,
                GemType.Purple
            };

            return availableTypes[Random.Range(0, availableTypes.Length)];
        }

        /// <summary>
        /// 根据宝石类型获取对应的Sprite
        /// </summary>
        /// <param name="type">宝石类型</param>
        /// <returns>对应的Sprite，如果未配置则返回null</returns>
        private Sprite GetSpriteForGemType(GemType type)
        {
            switch (type)
            {
                case GemType.Red:
                    return _redGemSprite;
                case GemType.Blue:
                    return _blueGemSprite;
                case GemType.Green:
                    return _greenGemSprite;
                case GemType.Yellow:
                    return _yellowGemSprite;
                case GemType.Purple:
                    return _purpleGemSprite;
                case GemType.Orange:
                    return _orangeGemSprite;
                default:
                    Debug.LogWarning($"GameManager: 未知的宝石类型 {type}");
                    return null;
            }
        }

        #endregion
        
        #region Game Control
        
        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void PauseGame()
        {
            _isGameRunning = false;
            Time.timeScale = 0f;
            Debug.Log("GameManager: 游戏已暂停");
        }
        
        /// <summary>
        /// 恢复游戏
        /// </summary>
        public void ResumeGame()
        {
            _isGameRunning = true;
            Time.timeScale = 1f;
            Debug.Log("GameManager: 游戏已恢复");
        }
        
        /// <summary>
        /// 重启游戏
        /// </summary>
        public void RestartGame()
        {
            _isGameRunning = false;
            StopAllCoroutines();
            InitializeGame();
        }
        
        /// <summary>
        /// 结束游戏
        /// </summary>
        public void EndGame()
        {
            _isGameRunning = false;
            StopAllCoroutines();
            Debug.Log("GameManager: 游戏已结束");
        }
        
        #endregion
        
        #region Debug Methods
        
        /// <summary>
        /// 获取当前游戏状态的字符串表示
        /// 用于调试
        /// </summary>
        public override string ToString()
        {
            return $"GameManager [State: {_currentState}, Running: {_isGameRunning}, Board: {_boardWidth}x{_boardHeight}]";
        }
        
        #endregion
    }
}
