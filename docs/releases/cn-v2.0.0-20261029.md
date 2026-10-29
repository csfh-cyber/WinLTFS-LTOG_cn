LTOG 中文界面版，基于原项目 2.0.0，适用于 Windows x64。

## 中文更新说明

- 汉化磁带机、磁带、搜索、日志、设置、关于、索引查看器、文件属性和搜索筛选界面，以及相关提示和可访问性标签。
- 磁带挂载成功后显示完成弹窗，包含盘符和读写模式。
- 已挂载磁带正常卸载完成后显示完成弹窗；取消挂载、挂载失败、异常退出和接管已有挂载不显示成功提醒。
- 多台磁带机同时完成操作时按顺序显示弹窗，并与原有维护确认框共用对话框队列。
- 提醒通过界面层观察现有状态实现，原有磁带后端、引擎版本、命令参数、读写和维护逻辑保持不变。

## 完整安装包

[下载 LTOG-2.0.0-setup.exe](https://github.com/csfh-cyber/WinLTFS-LTOG_cn/releases/download/cn-v2.0.0-20261029/LTOG-2.0.0-setup.exe)

文件大小：58,520,441 字节。安装包包含自包含的 Windows x64 程序、原版 WinLtfs 组件和 WinFsp MSI。

SHA256：

```text
b2025380a3cdf6d9523e91f9391158d4c8311dcc39e5a24a68c239aaa410b8b2
```

同时提供 [SHA256SUMS.txt](https://github.com/csfh-cyber/WinLTFS-LTOG_cn/releases/download/cn-v2.0.0-20261029/SHA256SUMS.txt) 和 [build-info.json](https://github.com/csfh-cyber/WinLTFS-LTOG_cn/releases/download/cn-v2.0.0-20261029/build-info.json)，便于核验文件与构建来源。

在 PowerShell 中执行 `Get-FileHash .\LTOG-2.0.0-setup.exe -Algorithm SHA256`，确认结果与上方一致。

## 验证结果与范围

- [Windows Actions 验证通过](https://github.com/csfh-cyber/WinLTFS-LTOG_cn/actions/runs/40839094553)：编译 0 错误、0 警告，32 项提醒与显示测试通过。
- 完整安装包生成、静默安装、所需文件检查和程序主窗口启动检查通过。
- 原有后端文件与上游基线一致性检查通过。
- CI 安装验证跳过 WinFsp 驱动安装；未进行真实磁带硬件的挂载、卸载和读写测试。

本次发布直接取用上述验证通过的原始安装包，发布前及上传后均核对 SHA256。
对应源码提交为 [`2b3a98f5581a0081a0d3bed0ca18791a414273a7`](https://github.com/csfh-cyber/WinLTFS-LTOG_cn/commit/2b3a98f5581a0081a0d3bed0ca18791a414273a7)，版本标签为 `cn-v2.0.0-20261029`。

保留 [rlaphoenix/LTOG](https://github.com/rlaphoenix/LTOG) 原作者版权、GPLv3 许可和第三方许可声明。中文界面及提醒修改日期：2026-10-29。
