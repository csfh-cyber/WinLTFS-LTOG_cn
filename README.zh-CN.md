# LTOG 中文界面版

[English](README.md) | 简体中文

LTOG 是 Windows 上的 LTO 磁带管理界面，通过 WinLtfs 将 LTFS 磁带挂载为盘符，
便于在资源管理器中浏览、复制和管理文件。本仓库提供简体中文界面与挂载、卸载完成提醒。

基于 [rlaphoenix/LTOG](https://github.com/rlaphoenix/LTOG) 的 `85756fe` 提交，
修改日期：2026-10-29。保留原项目版权声明、GPLv3 许可及第三方许可。

当前主分支：`main`；中文开发分支：`LTOG_cn_20261029`。

## 安装包下载

[GitHub Releases 中文版 2.0.0](https://github.com/csfh-cyber/WinLTFS-LTOG_cn/releases/tag/cn-v2.0.0-20261029)
提供经过 Windows 构建、静默安装和主窗口启动验证的完整安装包、中文更新说明及 SHA256 校验文件。
[直接下载安装包](https://github.com/csfh-cyber/WinLTFS-LTOG_cn/releases/download/cn-v2.0.0-20261029/LTOG-2.0.0-setup.exe)。

安装包包含 Windows x64 程序、.NET / WinUI 运行时、WinLtfs 1.2.0 和 WinFsp 2.1 安装组件。
可使用 [SHA256SUMS.txt](https://github.com/csfh-cyber/WinLTFS-LTOG_cn/releases/download/cn-v2.0.0-20261029/SHA256SUMS.txt)
校验下载文件，并通过同一 Release 的 `build-info.json` 核对构建来源。

## 本次修改

- 汉化磁带机、磁带、搜索、日志、设置、关于、索引查看器、文件属性及搜索筛选窗口。
- 汉化可访问性标签、提示文字和界面状态；文件名、卷标、设备原始数据、命令参数及引擎日志保留原文。
- 在现有状态从“正在挂载”变为“已挂载”，且挂载进程仍在运行时，显示完成提醒及盘符、读写模式。
- 在已挂载磁带进入卸载状态，随后进程以退出码 0 结束、状态回到空闲时，显示卸载完成提醒。
- 取消挂载、挂载失败、异常退出及启动时接管已有挂载均不会产生成功提醒。
- 多磁带提醒与格式化确认共用对话框队列，关闭窗口会取消待显示的完成提醒。

提醒由窗口层订阅现有状态实现。`src/Core/`、`DriveStore.cs`、`SchemaParser.cs`、
`Catalog.cs`、构建脚本、安装器及引擎版本均保持原样。界面模型中的修改仅涉及显示字符串。

## 运行环境与磁带机支持范围

- Windows 10/11，或 Windows Server 2019 及以上版本，64 位。
- 兼容的 LTO-5 至 LTO-9 磁带机，以及符合该磁带机规格的 LTFS 磁带。
- 正确安装的磁带机、连接卡和 WinFsp 驱动；设备应能在 Windows 中正常识别。

以下矩阵同步自当前安装包固定使用的 [WinLtfs v1.2.0 README](https://github.com/rlaphoenix/WinLtfs/blob/v1.2.0/README.md#supported-tape-drives)，
与 2026-10-30 核查的上游 `master` README 一致：

| 磁带机品牌 | LTO-5 | LTO-6 | LTO-7 | LTO-8 | LTO-9 |
| --- | :---: | :---: | :---: | :---: | :---: |
| HP / HPE | ✅ | ✅ | ✅ | ✅ | ✅ |
| Quantum | ✅ | ✅ | ✅ | ✅ | ✅ |
| Tandberg | ✅ | ✅ | ❓ | ❓ | ❓ |
| IBM | ❌ | ❌ | ❌ | ❌ | ❌ |

- ✅：上游白名单支持或已有测试反馈，不代表本中文分支逐型号完成了实机验收。
- ❓：兼容性尚待实机确认。
- ❌：当前随包磁带后端不支持；IBM 磁带机需要不同的后端。

该表描述的是**磁带机品牌**，不是磁带盒品牌。磁带机与磁带之间的跨代读写兼容性，
仍以具体设备规格为准，不能由此表推断任意代数之间均可读写。

WinLtfs 依据 SCSI INQUIRY 返回的产品 ID 匹配设备，而非只看外壳品牌：

| 设备系列 | 上游识别的产品 ID 形式 |
| --- | --- |
| HP / HPE | `Ultrium N-SCSI` |
| Quantum | `ULTRIUM N` |
| Tandberg | `LTO-N HH` |

`N` 表示 LTO 代数。可以在“设备管理器 → 属性 → 详细信息 → 硬件 ID”中查看，
也可以使用 SCSI inquiry 工具核对。OEM 贴牌设备应按实际产品 ID 确认。
发现表格遗漏、兼容性问题或新的实测结果，可向 [WinLtfs 上游反馈](https://github.com/rlaphoenix/WinLtfs/issues)。

## HPE StoreOpen 停用背景与源码来源

### 停止提供与下载下架时间

HPE 在 [StoreOpen 3.6 Windows 官方用户指南](https://support.hpe.com/hpesc/public/docDisplay?docId=sd00001247en_us&page=GUID-D7147C7F-2016-0901-084D-000000000473.html&docLocale=en_US)
中明确说明：**HPE StoreOpen Software 已于 2026 年 7 月 31 日停止提供，且不再供下载。**
指南保留供已安装用户参考，并移除了安装说明。这里采用的是官方产品停用及下载下架日期，
没有将其推定为最后一次代码更新日期。

### 为什么基于 3.5.0，而不是 3.6.0

HPE StoreOpen 3.6 确实存在，官方 [3.6 发行说明](https://support.hpe.com/hpesc/public/docDisplay?docId=sd00001245en_us&docLocale=en_US)
仍可查阅。需要区分**版本文档、安装程序和可用于继续开发的源代码**。

根据 [WinLtfs 上游背景说明](https://github.com/rlaphoenix/WinLtfs/blob/b2bcbd2db15e26975133e412f49f609967177a36/README.md#background)，
维护者尚未找到 HPE StoreOpen **3.6.0 源码**的可用下载、镜像或归档；截至本次核查（2026-10-30），
本项目也未取得可核验的 3.6.0 源码。因此 WinLtfs 使用由
[leavelet/ltfs-hp](https://github.com/leavelet/ltfs-hp) 留存的 **HPE StoreOpen 3.5.0 源码继续迭代**。
这不表示 3.6 从未发布，也不表示现在能保证取得其安装包。
HPE 的[版本下载可用性说明](https://support.hpe.com/hpesc/public/docDisplay?docId=sf000117077en_us&docLocale=en_US)
另提示，门户展示的版本可能受产品可用性及账号授权影响；这类入口也不能证明源码可下载。

原 HPE Windows 移植依赖 UMFSDK / FUSE4Win。WinLtfs 将文件系统集成迁移至
[WinFsp](https://winfsp.dev)，LTOG 再提供图形管理界面，本仓库在其上完成中文界面与完成提醒。
原作者、HPE、IBM、OSR 及相关开源组件的版权和许可声明均予以保留。

### 三个上游分支的关系

本次已完整拉取 WinLtfs 的三个分支及历史，并分别检查 README 与提交关系：

| 分支 | 核查提交 | 背景与用途 |
| --- | --- | --- |
| [`hpe`](https://github.com/rlaphoenix/WinLtfs/tree/329f28ec46cb63263886540df8a1c0464e3826d6) | `329f28e` | HPE 原始源码脉络：先导入 3.4.2，再应用 3.5.0 变更；该分支 [README](https://github.com/rlaphoenix/WinLtfs/blob/329f28ec46cb63263886540df8a1c0464e3826d6/README) 明确标识 3.5.0。 |
| [`master`](https://github.com/rlaphoenix/WinLtfs/tree/b2bcbd2db15e26975133e412f49f609967177a36) | `b2bcbd2` | 从 `hpe` 演进的 WinFsp 移植主线，维护引擎、命令行工具、兼容性和使用说明。 |
| [`feat-shellext`](https://github.com/rlaphoenix/WinLtfs/tree/ff6f0cec481e2dbda91ae7e169365358c7075e3c) | `ff6f0ce` | 从开发主线分出的 Windows 资源管理器 Shell 扩展实验分支，未集成到当前中文发布版。 |

**当前安装包仍固定使用 WinLtfs 1.2.0 与 WinFsp 2.1。** 本次读取新分支用于完善说明，
没有升级引擎，也没有将上游较新功能视为当前安装包已具备的功能。

## 基本使用与数据保护

1. 安装完整安装包及所需 WinFsp 组件，确认 Windows 能识别磁带机。
2. 插入兼容的 LTFS 磁带，在界面选择盘符和只读／读写方式后挂载。
3. 在资源管理器中复制和浏览文件。需要初始化磁带时可使用格式化功能；格式化会清空原有数据。
4. 使用界面卸载磁带，等待正常卸载完成提醒后，再按需取出磁带。

建议采用**复制**而非移动文件：磁带机内部缓冲区尚未写入磁带的数据，可能在设备、链路或电源故障时丢失。
应在正常卸载并完成必要的数据核验后，再按需删除源文件。保留并备份磁带索引 XML，便于查找文件和恢复时参考。

连续写入期间，窗口可见时的周期性 MAM 属性读取可能影响速度；最小化窗口会跳过后续的挂载磁带 MAM 刷新。
这项缓解措施不等于彻底解决性能问题，实际影响取决于硬件，参见 [上游 PR #13 讨论](https://github.com/rlaphoenix/LTOG/pull/13)。

## 构建

使用 Windows x64 和 .NET 8 SDK，按原项目 [README](README.md) 构建。

```powershell
dotnet build src/LTOG.csproj -c Release -p:Platform=x64
```

完整安装包仍使用原有 `build.ps1`，例如在仓库根目录执行：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Version 2.0.0
```

该脚本下载并校验固定版本的 WinLtfs 与 WinFsp，构建程序并使用 Inno Setup 生成安装包。

## 验证

无需磁带硬件的提醒与显示检查：

```shell
dotnet run --project tests/UiChecks/UiChecks.csproj
```

测试直接编译新增的界面提醒代码，使用轻量边界替身模拟 WinUI 对话框、驱动器状态和进程结果。
覆盖成功、失败、取消、重复通知、接管挂载、多设备排队、与维护确认框共存、关闭窗口和移除设备。
这些检查不替代 Windows 原生界面和磁带实机测试。

已发布版本的 [Windows Actions 验证](https://github.com/csfh-cyber/WinLTFS-LTOG_cn/actions/runs/40839094553)
使用 Windows Server 2022 和 .NET 8：编译 0 错误、0 警告，32 项提醒与显示检查通过，
完整安装包生成、静默安装、所需文件检查和主窗口启动检查均通过。
CI 安装验证跳过 WinFsp 驱动安装，未进行真实磁带的挂载、卸载和读写测试，
也不等同于对上表所有磁带机完成实机认证。

## GitHub Actions 安装包

`.github/workflows/windows-cn.yml` 在 `LTOG_cn_20261029` 分支推送时自动运行。
工作流使用 Windows Server 2022 runner 和 .NET 8，验证后端文件与上游基线一致，
运行提醒测试，调用原有 `build.ps1` 构建完整安装包，再执行静默安装及程序窗口启动检查。
安装验证省略 WinFsp 驱动安装任务；安装包仍包含原版 WinFsp MSI。

成功运行的 Actions 页面会提供 `LTOG-cn-windows-x64-<提交号>` 构建产物，内含安装包、
SHA256 校验值及对应源码提交信息。日志作为独立构建产物保存。
正式 Release 由独立发布工作流取用已通过验证的安装包，上传前后核对 SHA256，
提供不受 Actions 构建产物保留期限影响的版本下载链接。

Windows 验收时应检查：中文显示及缩放、只读和读写挂载、长时间卸载、取消挂载、
异常退出，以及两台磁带机同时完成操作时的弹窗顺序。

## 许可与致谢

LTOG 中文版沿用原项目的 [GPLv3](LICENSE)；WinLtfs / HPE LTFS 引擎采用 LGPL-2.1，
WinFsp 及其他组件保留各自许可。完整组件清单及源码来源见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。

感谢 [rlaphoenix/LTOG](https://github.com/rlaphoenix/LTOG)、[rlaphoenix/WinLtfs](https://github.com/rlaphoenix/WinLtfs)、
HPE、IBM、OSR、[WinFsp](https://github.com/winfsp/winfsp)，以及保存历史源码的
[nix-community/hpe-ltfs](https://github.com/nix-community/hpe-ltfs) 和 [leavelet/ltfs-hp](https://github.com/leavelet/ltfs-hp)。
