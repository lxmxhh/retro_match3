using UnityEngine;
using System;
using RetroMatch2D.Core;
using RetroMatch2D.Managers;

/// <summary>
/// 输入控制系统
/// 负责检测鼠标点击、拖拽操作，并触发相应的游戏事件
/// </summary>
public class InputController : MonoBehaviour
{
    [Header("拖拽设置")]
    [SerializeField] private float dragThreshold = 0.5f; // 最小拖拽距离（世界坐标单位）
    [SerializeField] private float dragTimeThreshold = 0.2f; // 最小拖拽时间（秒）

    // 事件声明
    public event Action<Gem> OnGemSelected;
    public event Action<Gem, Gem> OnSwapRequested;

    // 状态变量
    private bool isDragging = false;
    private Gem selectedGem = null;
    private Vector3 dragStartPosition = Vector3.zero;
    private float dragStartTime = 0f;
    private Camera mainCamera;

    // 拖拽方向枚举
    private enum DragDirection
    {
        None,
        Up,
        Down,
        Left,
        Right
    }

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("InputController: 未找到Main Camera!");
        }
    }

    private void Update()
    {
        HandleMouseInput();
    }

    /// <summary>
    /// 处理鼠标输入
    /// </summary>
    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseDown();
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            HandleMouseDrag();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            HandleMouseUp();
        }
    }

    /// <summary>
    /// 处理鼠标按下事件
    /// </summary>
    private void HandleMouseDown()
    {
        Gem gem = GetGemAtMousePosition();
        
        if (gem != null)
        {
            selectedGem = gem;
            dragStartPosition = Input.mousePosition;
            dragStartTime = Time.time;
            isDragging = true;

            // 触发宝石选中事件
            OnGemSelected?.Invoke(gem);
        }
    }

    /// <summary>
    /// 处理鼠标拖拽事件
    /// </summary>
    private void HandleMouseDrag()
    {
        if (selectedGem == null)
            return;

        Vector3 currentMousePosition = Input.mousePosition;
        Vector3 dragDelta = currentMousePosition - dragStartPosition;
        float dragDistance = dragDelta.magnitude;
        float dragTime = Time.time - dragStartTime;

        // 检查是否满足拖拽条件
        if (dragDistance >= dragThreshold && dragTime >= dragTimeThreshold)
        {
            DragDirection direction = CalculateDragDirection(dragDelta);
            
            if (direction != DragDirection.None)
            {
                Gem adjacentGem = GetAdjacentGem(selectedGem, direction);
                
                if (adjacentGem != null)
                {
                    // 触发交换事件
                    OnSwapRequested?.Invoke(selectedGem, adjacentGem);
                    
                    // 重置状态，结束拖拽
                    isDragging = false;
                    selectedGem = null;
                }
            }
        }
    }

    /// <summary>
    /// 处理鼠标释放事件
    /// </summary>
    private void HandleMouseUp()
    {
        isDragging = false;
        selectedGem = null;
    }

    /// <summary>
    /// 通过鼠标位置获取宝石
    /// 使用坐标转换方法，不需要Collider
    /// </summary>
    /// <returns>检测到的宝石，如果没有则返回null</returns>
    private Gem GetGemAtMousePosition()
    {
        if (mainCamera == null)
        {
            Debug.LogWarning("InputController: Main Camera 为空!");
            return null;
        }

        // 1. 将鼠标屏幕坐标转换为世界坐标
        // 对于正交相机，需要设置正确的Z深度
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -mainCamera.transform.position.z; // 相机在Z=-10，所以这里设置为10
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(mousePos);

        // 2. 世界坐标转换为网格坐标
        Vector2Int gridPosition = GemGrid.Instance.WorldToGridPosition(mouseWorldPosition);

        // 3. 检查网格坐标是否有效
        if (!GemGrid.Instance.IsValidPosition(gridPosition.x, gridPosition.y))
        {
            return null;
        }

        // 4. 通过网格坐标获取宝石
        Gem gem = GemGrid.Instance.GetGem(gridPosition.x, gridPosition.y);

        if (gem != null)
        {
            Debug.Log($"InputController: 检测到宝石 {gem.Type} 在 ({gridPosition.x}, {gridPosition.y})");
        }

        return gem;
    }

    /// <summary>
    /// 计算拖拽方向
    /// </summary>
    /// <param name="dragDelta">拖拽的屏幕坐标差值</param>
    /// <returns>拖拽方向</returns>
    private DragDirection CalculateDragDirection(Vector3 dragDelta)
    {
        float absDragX = Mathf.Abs(dragDelta.x);
        float absDragY = Mathf.Abs(dragDelta.y);

        // 比较X和Y的移动距离，确定主要方向
        if (absDragX > absDragY)
        {
            // 横向拖拽
            return dragDelta.x > 0 ? DragDirection.Right : DragDirection.Left;
        }
        else if (absDragY > absDragX)
        {
            // 纵向拖拽
            // 注意：屏幕Y坐标向下递增，但游戏世界Y坐标向上递增
            return dragDelta.y > 0 ? DragDirection.Up : DragDirection.Down;
        }

        return DragDirection.None;
    }

    /// <summary>
    /// 获取指定方向的相邻宝石
    /// </summary>
    /// <param name="gem">当前宝石</param>
    /// <param name="direction">拖拽方向</param>
    /// <returns>相邻的宝石，如果没有则返回null</returns>
    private Gem GetAdjacentGem(Gem gem, DragDirection direction)
    {
        if (gem == null)
            return null;

        // 获取宝石的网格位置
        Vector2Int gemGridPos = gem.GridPosition;
        Vector2Int adjacentGridPos = gemGridPos;

        // 根据方向计算相邻宝石的网格位置
        switch (direction)
        {
            case DragDirection.Up:
                adjacentGridPos.y += 1;
                break;
            case DragDirection.Down:
                adjacentGridPos.y -= 1;
                break;
            case DragDirection.Left:
                adjacentGridPos.x -= 1;
                break;
            case DragDirection.Right:
                adjacentGridPos.x += 1;
                break;
            default:
                return null;
        }

        // 通过网格管理器查找相邻宝石
        Gem adjacentGem = GemGrid.Instance.GetGem(adjacentGridPos.x, adjacentGridPos.y);
        
        return adjacentGem;
    }

    /// <summary>
    /// 重置输入状态
    /// 在需要中断当前拖拽操作时调用
    /// </summary>
    public void ResetInputState()
    {
        isDragging = false;
        selectedGem = null;
    }

    /// <summary>
    /// 获取当前是否正在拖拽
    /// </summary>
    public bool IsDragging => isDragging;

    /// <summary>
    /// 获取当前选中的宝石
    /// </summary>
    public Gem SelectedGem => selectedGem;
}
