using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace RetroMatch2D.UI
{
    /// <summary>
    /// UI管理器 - 负责游戏UI的显示和更新
    /// 实现单例模式，管理分数、移动次数、目标分数等UI元素
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        /// <summary>单例实例</summary>
        private static UIManager instance;

        /// <summary>单例属性</summary>
        public static UIManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<UIManager>();
                    if (instance == null)
                    {
                        GameObject singleton = new GameObject("UIManager");
                        instance = singleton.AddComponent<UIManager>();
                    }
                }
                return instance;
            }
        }

        [Header("分数显示")]
        /// <summary>分数文本组件</summary>
        [SerializeField]
        private TextMeshProUGUI scoreText;

        /// <summary>分数前缀标签</summary>
        [SerializeField]
        private string scorePrefix = "分数: ";

        [Header("移动次数显示")]
        /// <summary>移动次数文本组件</summary>
        [SerializeField]
        private TextMeshProUGUI movesText;

        /// <summary>移动次数前缀标签</summary>
        [SerializeField]
        private string movesPrefix = "移动: ";

        [Header("目标分数显示")]
        /// <summary>目标分数文本组件</summary>
        [SerializeField]
        private TextMeshProUGUI targetText;

        /// <summary>目标分数前缀标签</summary>
        [SerializeField]
        private string targetPrefix = "目标: ";

        [Header("游戏结束UI")]
        /// <summary>游戏结束面板</summary>
        [SerializeField]
        private GameObject gameOverPanel;

        /// <summary>游戏结束标题文本</summary>
        [SerializeField]
        private TextMeshProUGUI gameOverTitle;

        /// <summary>游戏结束描述文本</summary>
        [SerializeField]
        private TextMeshProUGUI gameOverDescription;

        /// <summary>胜利时的标题文本</summary>
        [SerializeField]
        private string winTitle = "胜利!";

        /// <summary>失败时的标题文本</summary>
        [SerializeField]
        private string loseTitle = "失败!";

        /// <summary>胜利时的颜色</summary>
        [SerializeField]
        private Color winTitleColor = Color.green;

        /// <summary>失败时的颜色</summary>
        [SerializeField]
        private Color loseTitleColor = Color.red;

        [Header("关卡开始UI")]
        /// <summary>关卡开始面板</summary>
        [SerializeField]
        private GameObject levelStartPanel;

        /// <summary>关卡开始标题文本</summary>
        [SerializeField]
        private TextMeshProUGUI levelStartTitle;

        /// <summary>关卡开始前缀标签</summary>
        [SerializeField]
        private string levelStartPrefix = "关卡 ";

        /// <summary>关卡开始显示持续时间（秒）</summary>
        [SerializeField]
        private float levelStartDisplayDuration = 2f;

        /// <summary>当前显示的分数</summary>
        private int currentScore = 0;

        /// <summary>当前显示的移动次数</summary>
        private int currentMoves = 0;

        /// <summary>当前显示的目标分数</summary>
        private int currentTarget = 0;

        /// <summary>关卡开始显示的协程</summary>
        private Coroutine levelStartCoroutine;

        private void Awake()
        {
            // 实现单例模式
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // 初始化UI面板状态
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            if (levelStartPanel != null)
            {
                levelStartPanel.SetActive(false);
            }

            // 初始化UI显示
            UpdateScore(0);
            UpdateMoves(0);
            UpdateTarget(0);
        }

        /// <summary>
        /// 更新分数显示
        /// </summary>
        /// <param name="score">新的分数值</param>
        public void UpdateScore(int score)
        {
            currentScore = score;

            if (scoreText != null)
            {
                scoreText.text = scorePrefix + currentScore.ToString();
            }
            else
            {
                Debug.LogWarning("UIManager: scoreText 组件未设置!");
            }
        }

        /// <summary>
        /// 更新移动次数显示
        /// </summary>
        /// <param name="moves">新的移动次数</param>
        public void UpdateMoves(int moves)
        {
            currentMoves = moves;

            if (movesText != null)
            {
                movesText.text = movesPrefix + currentMoves.ToString();
            }
            else
            {
                Debug.LogWarning("UIManager: movesText 组件未设置!");
            }
        }

        /// <summary>
        /// 更新目标分数显示
        /// </summary>
        /// <param name="target">新的目标分数</param>
        public void UpdateTarget(int target)
        {
            currentTarget = target;

            if (targetText != null)
            {
                targetText.text = targetPrefix + currentTarget.ToString();
            }
            else
            {
                Debug.LogWarning("UIManager: targetText 组件未设置!");
            }
        }

        /// <summary>
        /// 显示游戏结束UI
        /// </summary>
        /// <param name="win">true表示胜利，false表示失败</param>
        public void ShowGameOver(bool win)
        {
            if (gameOverPanel == null)
            {
                Debug.LogWarning("UIManager: gameOverPanel 未设置!");
                return;
            }

            gameOverPanel.SetActive(true);

            if (gameOverTitle != null)
            {
                gameOverTitle.text = win ? winTitle : loseTitle;
                gameOverTitle.color = win ? winTitleColor : loseTitleColor;
            }

            if (gameOverDescription != null)
            {
                if (win)
                {
                    gameOverDescription.text = $"你赢了! 最终分数: {currentScore}";
                }
                else
                {
                    gameOverDescription.text = $"游戏结束! 还需要 {currentTarget - currentScore} 分";
                }
            }

            Debug.Log($"UIManager: 显示游戏结束UI - {(win ? "胜利" : "失败")}");
        }

        /// <summary>
        /// 隐藏游戏结束UI
        /// </summary>
        public void HideGameOver()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 显示关卡开始UI
        /// </summary>
        /// <param name="level">关卡数字</param>
        public void ShowLevelStart(int level)
        {
            if (levelStartPanel == null)
            {
                Debug.LogWarning("UIManager: levelStartPanel 未设置!");
                return;
            }

            // 如果已有协程在运行，先停止它
            if (levelStartCoroutine != null)
            {
                StopCoroutine(levelStartCoroutine);
            }

            levelStartCoroutine = StartCoroutine(ShowLevelStartCoroutine(level));
        }

        /// <summary>
        /// 显示关卡开始UI的协程
        /// </summary>
        private System.Collections.IEnumerator ShowLevelStartCoroutine(int level)
        {
            levelStartPanel.SetActive(true);

            if (levelStartTitle != null)
            {
                levelStartTitle.text = levelStartPrefix + level.ToString();
            }

            Debug.Log($"UIManager: 显示关卡开始UI - 关卡 {level}");

            // 等待指定时间后隐藏
            yield return new WaitForSeconds(levelStartDisplayDuration);

            levelStartPanel.SetActive(false);
            levelStartCoroutine = null;
        }

        /// <summary>
        /// 隐藏关卡开始UI
        /// </summary>
        public void HideLevelStart()
        {
            if (levelStartCoroutine != null)
            {
                StopCoroutine(levelStartCoroutine);
                levelStartCoroutine = null;
            }

            if (levelStartPanel != null)
            {
                levelStartPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 获取当前分数
        /// </summary>
        public int GetCurrentScore()
        {
            return currentScore;
        }

        /// <summary>
        /// 获取当前移动次数
        /// </summary>
        public int GetCurrentMoves()
        {
            return currentMoves;
        }

        /// <summary>
        /// 获取当前目标分数
        /// </summary>
        public int GetCurrentTarget()
        {
            return currentTarget;
        }
    }
}
