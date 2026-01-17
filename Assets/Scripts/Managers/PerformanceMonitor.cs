using UnityEngine;
using System.Text;

namespace RetroMatch2D.Managers
{
    /// <summary>
    /// 性能监控器
    /// 显示FPS、内存使用和对象池统计信息
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        [Header("显示设置")]
        [SerializeField]
        private bool showMonitor = true;

        [SerializeField]
        private KeyCode toggleKey = KeyCode.F1;

        [SerializeField]
        private int fontSize = 14;

        // FPS计算
        private float deltaTime = 0.0f;
        private float updateInterval = 0.5f;
        private float accum = 0.0f;
        private int frames = 0;
        private float fps = 0.0f;
        private float lastUpdateTime = 0.0f;

        // UI样式
        private GUIStyle style;
        private Rect rect;

        private void Start()
        {
            // 初始化UI样式
            style = new GUIStyle();
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = fontSize;
            style.normal.textColor = Color.white;

            rect = new Rect(10, 10, 400, 300);
        }

        private void Update()
        {
            // 切换显示
            if (Input.GetKeyDown(toggleKey))
            {
                showMonitor = !showMonitor;
            }

            // 计算FPS
            deltaTime += Time.unscaledDeltaTime;
            accum += Time.timeScale / Time.deltaTime;
            frames++;

            if (Time.time - lastUpdateTime > updateInterval)
            {
                fps = accum / frames;
                accum = 0.0f;
                frames = 0;
                lastUpdateTime = Time.time;
            }
        }

        private void OnGUI()
        {
            if (!showMonitor) return;

            // 绘制半透明背景
            GUI.Box(rect, "");

            // 构建显示文本
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== Performance Monitor ===");
            sb.AppendLine($"Press {toggleKey} to toggle");
            sb.AppendLine();

            // FPS信息
            Color fpsColor = GetFPSColor(fps);
            sb.AppendLine($"FPS: <color={ColorToHex(fpsColor)}>{fps:F1}</color>");
            sb.AppendLine($"Frame Time: {deltaTime * 1000.0f:F1}ms");
            sb.AppendLine();

            // 内存信息
            float memoryMB = System.GC.GetTotalMemory(false) / 1048576f;
            sb.AppendLine($"Memory: {memoryMB:F1} MB");
            sb.AppendLine();

            // 对象池统计
            if (GemPool.Instance != null)
            {
                sb.AppendLine("=== Gem Pool Stats ===");
                sb.AppendLine($"Active: {GemPool.Instance.GetActiveCount()}");
                sb.AppendLine($"Available: {GemPool.Instance.GetAvailableCount()}");
                sb.AppendLine($"Total Capacity: {GemPool.Instance.GetTotalCapacity()}");

                // 计算利用率
                float utilization = GemPool.Instance.GetTotalCapacity() > 0
                    ? (float)GemPool.Instance.GetActiveCount() / GemPool.Instance.GetTotalCapacity() * 100f
                    : 0f;
                sb.AppendLine($"Utilization: {utilization:F1}%");
            }
            else
            {
                sb.AppendLine("GemPool not initialized");
            }

            GUI.Label(rect, sb.ToString(), style);
        }

        /// <summary>
        /// 根据FPS返回颜色
        /// </summary>
        private Color GetFPSColor(float fps)
        {
            if (fps >= 60f) return Color.green;
            if (fps >= 30f) return Color.yellow;
            return Color.red;
        }

        /// <summary>
        /// 将Color转换为Hex字符串
        /// </summary>
        private string ColorToHex(Color color)
        {
            return $"#{ColorUtility.ToHtmlStringRGB(color)}";
        }

        /// <summary>
        /// 在控制台打印详细统计信息
        /// </summary>
        [ContextMenu("Print Detailed Stats")]
        public void PrintDetailedStats()
        {
            Debug.Log("=== Performance Statistics ===");
            Debug.Log($"FPS: {fps:F1}");
            Debug.Log($"Frame Time: {deltaTime * 1000.0f:F1}ms");
            Debug.Log($"Memory: {System.GC.GetTotalMemory(false) / 1048576f:F1} MB");

            if (GemPool.Instance != null)
            {
                Debug.Log(GemPool.Instance.GetStatistics());
            }
        }
    }
}
