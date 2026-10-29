# LTOG 中文界面版

基于 [rlaphoenix/LTOG](https://github.com/rlaphoenix/LTOG) 的 `85756fe` 提交，
修改日期：2026-10-29。保留原项目版权声明、GPLv3 许可及第三方许可。

分支：`LTOG_cn_20261029`。

## 安装包下载

[GitHub Releases 中文版 2.0.0](https://github.com/csfh-cyber/WinLTFS-LTOG_cn/releases/tag/cn-v2.0.0-20261029)
提供经过 Windows 构建、静默安装和主窗口启动验证的完整安装包、中文更新说明及 SHA256 校验文件。
[直接下载安装包](https://github.com/csfh-cyber/WinLTFS-LTOG_cn/releases/download/cn-v2.0.0-20261029/LTOG-2.0.0-setup.exe)。

## 本次修改

- 汉化磁带机、磁带、搜索、日志、设置、关于、索引查看器、文件属性及搜索筛选窗口。
- 汉化可访问性标签、提示文字和界面状态；文件名、卷标、设备原始数据、命令参数及引擎日志保留原文。
- 在现有状态从“正在挂载”变为“已挂载”，且挂载进程仍在运行时，显示完成提醒及盘符、读写模式。
- 在已挂载磁带进入卸载状态，随后进程以退出码 0 结束、状态回到空闲时，显示卸载完成提醒。
- 取消挂载、挂载失败、异常退出及启动时接管已有挂载均不会产生成功提醒。
- 多磁带提醒与格式化确认共用对话框队列，关闭窗口会取消待显示的完成提醒。

提醒由窗口层订阅现有状态实现。`src/Core/`、`DriveStore.cs`、`SchemaParser.cs`、
`Catalog.cs`、构建脚本、安装器及引擎版本均保持原样。界面模型中的修改仅涉及显示字符串。

## 构建

使用 Windows x64 和 .NET 8 SDK，按原项目 [README](README.md) 构建。

```powershell
dotnet build src/LTOG.csproj -c Release -p:Platform=x64
```

完整安装包仍使用原有 `build.ps1`。

## 验证

无需磁带硬件的提醒与显示检查：

```shell
dotnet run --project tests/UiChecks/UiChecks.csproj
```

测试直接编译新增的界面提醒代码，使用轻量边界替身模拟 WinUI 对话框、驱动器状态和进程结果。
覆盖成功、失败、取消、重复通知、接管挂载、多设备排队、与维护确认框共存、关闭窗口和移除设备。
这些检查不替代 Windows 原生界面和磁带实机测试。

本次开发环境为 macOS：32 项提醒与显示检查通过，C# 12 语法检查及 XAML XML 检查通过。
NuGet 依赖现已恢复成功：先前的 NU1301 来自本机沙箱限制 .NET 读取系统域名，
并非依赖版本不存在。解除该执行限制后可以正常恢复。Windows XAML 编译器需要 Windows 环境。

## GitHub Actions 安装包

`.github/workflows/windows-cn.yml` 在 `LTOG_cn_20261029` 分支推送时自动运行。
工作流使用 Windows Server 2022 runner 和 .NET 8，验证后端文件与上游基线一致，
运行提醒测试，调用原有 `build.ps1` 构建完整安装包，再执行静默安装及程序窗口启动检查。
安装验证省略 WinFsp 驱动安装任务；安装包仍包含原版 WinFsp MSI。

成功运行的 Actions 页面会提供 `LTOG-cn-windows-x64-<提交号>` 构建产物，内含安装包、
SHA256 校验值及对应源码提交信息。日志作为独立构建产物保存。构建不会自动发布正式 Release。

Windows 验收时应检查：中文显示及缩放、只读和读写挂载、长时间卸载、取消挂载、
异常退出，以及两台磁带机同时完成操作时的弹窗顺序。
