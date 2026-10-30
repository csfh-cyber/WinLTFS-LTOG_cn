# LTOG 中文界面版

简体中文 | [English](README.en.md)

LTOG 是 Windows 上的 LTO 磁带管理界面，通过 WinLtfs 将 LTFS 磁带挂载为盘符，
便于在资源管理器中浏览、复制和管理文件。本仓库提供简体中文界面与挂载、卸载完成提醒。

基于 [rlaphoenix/LTOG](https://github.com/rlaphoenix/LTOG)，沿用 GPLv3 许可及相关组件的版权和许可声明。

## 安装包下载

[GitHub Releases 中文版 2.0.0](https://github.com/csfh-cyber/WinLTFS-LTOG_cn/releases/tag/cn-v2.0.0-20261029)
提供完整安装包与 SHA256 校验文件。
[直接下载安装包](https://github.com/csfh-cyber/WinLTFS-LTOG_cn/releases/download/cn-v2.0.0-20261029/LTOG-2.0.0-setup.exe)。

安装包包含 Windows x64 程序、.NET / WinUI 运行时、WinLtfs 1.2.0 和 WinFsp 2.1 安装组件。
可使用 [SHA256SUMS.txt](https://github.com/csfh-cyber/WinLTFS-LTOG_cn/releases/download/cn-v2.0.0-20261029/SHA256SUMS.txt)
校验下载文件。

## 主要功能

- 简体中文 WinUI 3 界面，提供磁带机、磁带、搜索、日志、设置及相关工具窗口。
- LTFS 2.4 文件系统管理，可将磁带以只读或读写方式挂载为 Windows 盘符。
- 磁带格式化、卸载、弹出及维护工具。
- 磁带索引浏览与备份、文件搜索和属性查看。
- 挂载及正常卸载完成弹窗，挂载提醒包含盘符和读写模式。
- 多磁带完成提醒依次显示；取消、失败或异常退出不会显示成功提醒。

## 运行环境与磁带机支持范围

- Windows 10/11，或 Windows Server 2019 及以上版本，64 位。
- 兼容的 LTO-5 至 LTO-9 磁带机，以及符合该磁带机规格的 LTFS 磁带。
- 正确安装的磁带机、连接卡和 WinFsp 驱动；设备应能在 Windows 中正常识别。

磁带机兼容性由 [WinLtfs 后端](https://github.com/rlaphoenix/WinLtfs/blob/v1.2.0/README.md#supported-tape-drives)提供：

| 磁带机品牌 | LTO-5 | LTO-6 | LTO-7 | LTO-8 | LTO-9 |
| --- | :---: | :---: | :---: | :---: | :---: |
| HP / HPE | ✅ | ✅ | ✅ | ✅ | ✅ |
| Quantum | ✅ | ✅ | ✅ | ✅ | ✅ |
| Tandberg | ✅ | ✅ | ❓ | ❓ | ❓ |
| IBM | ❌ | ❌ | ❌ | ❌ | ❌ |

- ✅：上游白名单支持或已有实测反馈。
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
指南保留供已安装用户参考，并移除了安装说明。

### 为什么基于 3.5.0，而不是 3.6.0

HPE StoreOpen 3.6 确实存在，官方 [3.6 版本文档](https://support.hpe.com/hpesc/public/docDisplay?docId=sd00001245en_us&docLocale=en_US)
仍可查阅。需要区分**版本文档、安装程序和可用于继续开发的源代码**。

根据 [WinLtfs 上游背景说明](https://github.com/rlaphoenix/WinLtfs/blob/b2bcbd2db15e26975133e412f49f609967177a36/README.md#background)，
维护者尚未找到 HPE StoreOpen **3.6.0 源码**的可用下载、镜像或归档。因此 WinLtfs 使用由
[leavelet/ltfs-hp](https://github.com/leavelet/ltfs-hp) 留存的 **HPE StoreOpen 3.5.0 源码继续迭代**。
这不表示 3.6 从未发布，也不表示现在能保证取得其安装包。
HPE 的[版本下载可用性说明](https://support.hpe.com/hpesc/public/docDisplay?docId=sf000117077en_us&docLocale=en_US)
另提示，门户展示的版本可能受产品可用性及账号授权影响；这类入口也不能证明源码可下载。

原 HPE Windows 移植依赖 UMFSDK / FUSE4Win。WinLtfs 将文件系统集成迁移至
[WinFsp](https://winfsp.dev)，LTOG 再提供图形管理界面，本仓库在其上完成中文界面与完成提醒。
原作者、HPE、IBM、OSR 及相关开源组件的版权和许可声明均予以保留。

### 三个上游分支的关系

| 分支 | 用途 |
| --- | --- |
| [`hpe`](https://github.com/rlaphoenix/WinLtfs/tree/hpe) | HPE StoreOpen 3.5.0 源码基线，保留原始 Windows 移植及许可。 |
| [`master`](https://github.com/rlaphoenix/WinLtfs/tree/master) | WinFsp 移植主线，维护引擎、命令行工具、兼容性和使用说明。 |
| [`feat-shellext`](https://github.com/rlaphoenix/WinLtfs/tree/feat-shellext) | Windows 资源管理器 Shell 扩展实验分支，独立于标准安装包。 |

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

使用 Windows x64 和 .NET 8 SDK，详细步骤见[英文构建说明](README.en.md#building)。

```powershell
dotnet build src/LTOG.csproj -c Release -p:Platform=x64
```

完整安装包仍使用原有 `build.ps1`，例如在仓库根目录执行：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Version 2.0.0
```

该脚本下载并校验 WinLtfs 与 WinFsp，构建程序并使用 Inno Setup 生成安装包。

## 界面测试

无需磁带硬件的提醒与显示检查：

```shell
dotnet run --project tests/UiChecks/UiChecks.csproj
```

测试直接编译新增的界面提醒代码，使用轻量边界替身模拟 WinUI 对话框、驱动器状态和进程结果。
覆盖成功、失败、取消、重复通知、接管挂载、多设备排队、与维护确认框共存、关闭窗口和移除设备。
这些检查不替代 Windows 原生界面和磁带实机测试。

## GitHub Actions 安装包

仓库提供 Windows 构建、安装和启动检查工作流，可在 [Actions](https://github.com/csfh-cyber/WinLTFS-LTOG_cn/actions)
中查看构建产物。日常安装请使用 [Releases](https://github.com/csfh-cyber/WinLTFS-LTOG_cn/releases)
中的完整安装包和 SHA256 校验文件。

## 许可与致谢

LTOG 中文版沿用原项目的 [GPLv3](LICENSE)；WinLtfs / HPE LTFS 引擎采用 LGPL-2.1，
WinFsp 及其他组件保留各自许可。完整组件清单及源码来源见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。

感谢 [rlaphoenix/LTOG](https://github.com/rlaphoenix/LTOG)、[rlaphoenix/WinLtfs](https://github.com/rlaphoenix/WinLtfs)、
HPE、IBM、OSR、[WinFsp](https://github.com/winfsp/winfsp)，以及保存历史源码的
[nix-community/hpe-ltfs](https://github.com/nix-community/hpe-ltfs) 和 [leavelet/ltfs-hp](https://github.com/leavelet/ltfs-hp)。
