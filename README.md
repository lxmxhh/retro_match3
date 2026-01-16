# Retro Match 2D

一个使用Unity 2022.3.23f1开发的经典三消游戏（Match-3 Puzzle Game）。

![Unity Version](https://img.shields.io/badge/Unity-2022.3.23f1-black)
![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Mac%20%7C%20Linux-blue)

## 📝 项目简介

Retro Match 2D 是一款经典的消除类益智游戏，玩家通过交换相邻的宝石来创造3个或更多相同类型的宝石连线，从而消除它们并获得分数。

### 核心玩法

- 🎮 拖动相邻宝石进行交换
- 💎 匹配3个或更多相同类型的宝石
- ⬇️ 宝石自动下落填充空位
- 🔗 支持连锁消除获取额外分数
- 🎯 简单易上手的操作体验

## ✨ 功能特性

### 已实现功能

- ✅ **完整的游戏循环**：Idle → Swapping → Matching → Falling
- ✅ **匹配检测系统**：支持水平和垂直方向的3消检测
- ✅ **输入系统**：基于坐标转换的点击拖拽系统（无需Collider）
- ✅ **下落动画**：平滑的宝石下落效果
- ✅ **消除动画**：淡出和缩放效果
- ✅ **状态管理**：完善的游戏状态机
- ✅ **分数系统**：基础分数 + 额外宝石奖励

### 计划中功能

- ⏳ UI界面（分数显示、目标、暂停菜单）
- ⏳ 关卡系统
- ⏳ 特殊宝石（炸弹、彩虹宝石等）
- ⏳ 粒子特效
- ⏳ 音效和背景音乐
- ⏳ 关卡编辑器

## 🛠️ 技术架构

### 核心系统

```
┌─────────────────────────────────┐
│      GameManager                │
│  游戏核心控制器，管理游戏流程   │
└──────────────┬──────────────────┘
               │
    ┌──────────┼──────────┐
    │          │          │
    ▼          ▼          ▼
┌────────┐ ┌────────┐ ┌─────────┐
│ Match  │ │ Fall   │ │  Input  │
│Manager │ │System  │ │Controller│
└────────┘ └────────┘ └─────────┘
    │          │          │
    └──────────┴──────────┘
               │
    ┌──────────┴──────────┐
    │                     │
    ▼                     ▼
┌────────┐           ┌────────┐
│ Board  │           │GemGrid │
│  棋盘  │           │网格管理│
└────────┘           └────────┘
```

### 关键组件

- **GameManager**：游戏主控制器，管理游戏状态和流程
- **MatchManager**：匹配检测和消除管理
- **MatchDetector**：静态匹配算法实现
- **FallSystem**：宝石下落逻辑
- **InputController**：输入检测和处理
- **Board & GemGrid**：棋盘数据管理
- **Gem**：单个宝石对象

## 🚀 快速开始

### 环境要求

- Unity 2022.3.23f1 或更高版本
- .NET 4.x 或更高

### 安装步骤

1. 克隆仓库
```bash
git clone https://github.com/你的用户名/retro_match2d.git
```

2. 使用Unity Hub打开项目
   - 打开Unity Hub
   - 点击"添加"
   - 选择克隆的项目文件夹

3. 打开主场景
   - 在Project窗口中找到 `Assets/Scenes/SampleScene.unity`
   - 双击打开

4. 运行游戏
   - 点击Unity编辑器顶部的播放按钮▶️
   - 在Game窗口中拖动宝石进行游戏

### 项目结构

```
retro_match2d/
├── Assets/
│   ├── Scenes/           # 游戏场景
│   │   └── SampleScene.unity
│   ├── Scripts/          # 所有C#脚本
│   │   ├── Core/         # 核心游戏逻辑
│   │   ├── Managers/     # 管理器类
│   │   └── UI/           # UI脚本
│   ├── Sprites/          # 图片资源
│   │   └── Gems/         # 宝石精灵图
│   └── Screenshots/      # 游戏截图
├── Documentation/        # 项目文档
├── ProjectSettings/      # Unity项目设置
└── Packages/            # Unity包管理
```

## 🎮 操作说明

- **鼠标点击拖动**：选择宝石并向相邻方向拖动进行交换
- **有效交换**：只有能产生3连消的交换才会执行
- **连锁消除**：消除后新宝石下落可能产生新的匹配

## 📚 文档

详细技术文档在 [`Documentation/`](./Documentation/) 目录：

### 核心文档

| 文档 | 说明 | 适合对象 |
|------|------|----------|
| [Architecture.md](./Documentation/Architecture.md) | 完整的系统架构设计 | 想了解整体架构的开发者 |
| [MatchDetector.md](./Documentation/MatchDetector.md) | 匹配检测系统API文档 | 需要调用匹配功能的开发者 |
| [API-Reference.md](./Documentation/API-Reference.md) | 快速参考和使用示例 | 需要快速查找API的开发者 |

### 推荐阅读顺序

**快速上手**：README.md（本文） → API-Reference.md

**深入理解**：README.md → Architecture.md → MatchDetector.md → API-Reference.md

**功能扩展**：Architecture.md（扩展架构部分） → 开始编码

## 🔧 开发相关

### 宝石类型

当前支持6种宝石类型：

```csharp
public enum GemType
{
    Red,      // 红宝石
    Blue,     // 蓝宝石
    Green,    // 绿宝石
    Yellow,   // 黄宝石
    Purple,   // 紫宝石
    Orange    // 橙宝石
}
```

### 游戏状态

```csharp
public enum GameState
{
    Idle,      // 空闲，等待玩家输入
    Swapping,  // 正在交换宝石
    Matching,  // 正在检测和消除匹配
    Falling    // 宝石正在下落填充空位
}
```

### 关键参数

在GameManager中可配置：

- **棋盘尺寸**：默认 8x8
- **交换动画时长**：0.3秒
- **匹配检测延迟**：0.5秒
- **下落动画时长**：0.3秒
- **基础分数**：每个宝石100分
- **额外宝石奖励**：每个额外宝石50分

## 🐛 已知问题

目前项目处于开发阶段，可能存在以下问题：

- 缺少UI界面
- 没有音效
- 没有关卡系统
- 没有存档功能

## 🤝 贡献指南

欢迎贡献代码！请遵循以下步骤：

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启Pull Request

## 📄 许可证

本项目使用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件

## 📧 联系方式

如有问题或建议，请通过以下方式联系：

- 提交 Issue
- Pull Request
- Email: your-email@example.com

## 🙏 致谢

- Unity Technologies - 游戏引擎
- Claude AI - 开发协助

---

⭐ 如果觉得这个项目有帮助，请给个星标！

**版本**: 0.1.0 (Alpha)
**最后更新**: 2026年1月16日
