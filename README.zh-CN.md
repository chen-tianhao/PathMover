# PathMover - AGV路径移动仿真库

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/PathMover)](https://www.nuget.org/packages/PathMover/)
[![许可证](https://img.shields.io/badge/license-MIT-green)](LICENSE)

PathMover 是一个基于 O2DESNet 框架构建的离散事件仿真库，用于建模和模拟仓库和物流网络中自动导引车（AGV）的移动。

## 特性

- **离散事件仿真**：基于 O2DESNet 框架，提供精确的时间基础仿真
- **基于路径的移动**：模拟 AGV 在预定义路径上控制点之间的移动
- **容量管理**：跟踪路径占用情况，防止碰撞
- **路由表支持**：预计算路由表以实现高效路径查找
- **大规模网络**：支持数千个控制点的网络
- **性能指标**：跟踪利用率、吞吐量和等待时间
- **可视化工具**：基于 Python 的网络和轨迹可视化

## 项目结构

```
PathMover/
├── PathMoverLibrary/          # 核心库（NuGet 包）
│   ├── PathMover.cs          # 主仿真引擎
│   ├── Vehicle.cs            # AGV 车辆接口和实现
│   ├── ControlPoint.cs       # 网络控制点
│   ├── PathMoverStatics.cs   # 静态网络配置
│   ├── PmPath.cs             # 控制点之间的路径
│   └── IdMapper.cs           # ID 到名称映射
├── PathMoverDemo/            # 演示应用程序
│   ├── Program.cs            # 主演示程序
│   ├── Simulator.cs          # 简单的 6 节点演示
│   ├── LargeScaleSimulator.cs # 大规模仿真
│   ├── NetworkLoader.cs      # JSON 网络加载器
│   └── SimulationLogger.cs   # 仿真日志记录
├── PathMoverRoutingGenerator/ # 路由表生成
│   ├── RoutingTableGenerator.cs
│   ├── AStarPathfinder.cs
│   └── DijkstraPathfinder.cs
├── RoutingTableBuilder/      # 路由表构建工具
├── NUnitTest_PM/             # 单元测试
├── Visualization/            # Python 可视化工具
└── layout_gen/               # 网络布局生成
```

## 安装

### NuGet 包
```bash
dotnet add package PathMover
```

### 从源代码安装
```bash
git clone https://github.com/chen-tianhao/PathMover.git
cd PathMover
dotnet build
```

## 快速开始

### 基本用法

```csharp
using PathMover;
using O2DESNet;

// 创建网络静态配置
var statics = new PathMoverStatics();

// 添加控制点
var A = new ControlPoint(0);
var B = new ControlPoint(1);
var C = new ControlPoint(2);

// 添加路径
statics.AddPath(A.Id, B.Id, new PmPath(A, B));
statics.AddPath(B.Id, C.Id, new PmPath(B, C));

// 创建 PathMover 实例
var pathMover = new PathMover(statics, smoothFactor: 1.0, coldStartDelay: 0.0);

// 创建 AGV
var agv = new Vehicle
{
    Name = "AGV-001",
    Speed = 1.0,
    CapacityNeeded = 1
};

// 请求进入
pathMover.RequestToEnter(agv, A);
```

### 运行演示

```bash
cd PathMoverDemo
dotnet run
```

选择仿真模式：
1. **简单演示**：6 个控制点，5 个 AGV
2. **大规模网络**：从 JSON 数据加载的 14,944 个控制点

## 核心概念

### 控制点
网络节点，AGV 可以在此进入、退出或改变方向。

### 路径
控制点之间的连接，具有容量约束。

### 车辆（AGV）
- 在控制点之间的路径上移动
- 具有速度和容量要求
- 遵循路由表到达目的地

### 路由表
所有控制点对之间的预计算最短路径。

## 高级功能

### 大规模网络
从 JSON 文件加载网络数据：
```csharp
var networkLoader = new NetworkLoader();
var statics = networkLoader.LoadFromJson("network.json");
```

### 性能指标
- 路径占用率
- 车辆等待时间
- 吞吐量统计
- 利用率指标

### 可视化
使用 Python 可视化工具：
```bash
cd Visualization
pip install -r requirements.txt
python visualize_network.py
python visualize_trajectories.py
```

## API 参考

### PathMover 类
- `RequestToEnter(IVehicle vehicle, ControlPoint cp)`：请求 AGV 进入
- `AttemptToDepart(IVehicle vehicle)`：尝试从当前路径离开
- `ReadyToExit(IVehicle vehicle, PmPath path)`：发出准备退出路径的信号

### IVehicle 接口
- `Name`：车辆标识符
- `Speed`：移动速度
- `CapacityNeeded`：所需路径容量
- `CurrentPath`：当前占用的路径
- `TargetList`：目的地控制点

## 示例

### 简单网络仿真
查看 `PathMoverDemo/Simulator.cs` 获取包含 6 个控制点和多个 AGV 的完整示例。

### 自定义车辆实现
```csharp
public class CustomVehicle : IVehicle
{
    public string Name { get; set; }
    public double Speed { get; set; }
    public int CapacityNeeded { get; set; }
    public PmPath CurrentPath { get; set; }
    public PmPath? PengingPath { get; set; }
    public bool IsStoped { get; set; }
    public List<ControlPoint> TargetList { get; set; }
    public PathMoverStatics PathMoverStatics { get; set; }

    // 实现接口方法
    public void RemoveTarget(ushort controlPointId) { ... }
    public PmPath NextPath(ushort currentPointId) { ... }
}
```

## 开发

### 从源代码构建
```bash
dotnet build PathMover.sln
```

### 运行测试
```bash
cd NUnitTest_PM
dotnet test
```

### 创建 NuGet 包
```bash
cd PathMoverLibrary
dotnet pack --configuration Release
```

## 贡献

1. Fork 仓库
2. 创建功能分支
3. 进行更改
4. 添加测试（如果适用）
5. 提交拉取请求

## 许可证

本项目采用 MIT 许可证 - 详情请参阅 [LICENSE](LICENSE) 文件。

## 致谢

- 基于 [O2DESNet](https://github.com/O2DESNet/O2DESNet) 框架构建
- 新加坡国立大学（NUS）开发

## 支持

如有问题和疑问，请使用 [GitHub Issues](https://github.com/chen-tianhao/PathMover/issues) 页面。

## 更新日志

### 版本 2.0.0
- 性能优化：改进了大规模网络下的路径查找算法，提升了仿真速度
- 新增功能：添加了动态路由支持，允许车辆根据实时网络状态调整路径
- API 改进：简化了车辆接口，提供了更灵活的配置选项
- 可视化增强：改进了 Python 可视化工具，支持实时仿真监控

### 版本 1.0.4
- 模型修订：由于不允许超车，只有 OutPengList 中的第一个车辆可以添加到相关的 InPendingList。

### 版本 1.0.2
- 错误修复：将 PengingPath 添加到 IVehicle 接口，以管理 AttemptToDepart 事件中 path.InPendingList 中的项目。

### 版本 1.0.1
- 错误修复：当容量释放向后传播时，尝试离开 currentPath.InPendingList 中的特定车辆（而不是 previousPath.OutPendingList 中的第一个车辆）。

### 版本 1.0.0
- 初始版本