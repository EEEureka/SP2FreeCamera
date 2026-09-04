# SP2 Free Camera

`SP2 Free Camera` 是适用于 Windows x64 版 `SimplePlanes 2` 的本地自由相机插件。
当前版本为 `0.6.4`，使用 `BepInEx 5 Mono x64` 加载，已针对游戏版本 `0.7.6.100f`
构建。插件只操作本地相机和本地 UI，不修改载具物理，不发送网络消息，也不依赖其他自定义插件。

## 直接安装

仓库的 `release` 目录提供可部署压缩包：

```text
release/SP2FreeCamera-v0.6.4-win-x64.zip
```

压缩包已经包含 Free Camera DLL 和完整的 BepInEx `5.4.23.5` Windows x64 运行时，
不需要另行安装 BepInEx。

1. 完全关闭 `SimplePlanes 2`。
2. 打开压缩包，将其中全部内容直接解压到游戏根目录，也就是
   `SimplePlanes 2.exe` 所在目录。
3. 如果系统询问是否合并 `BepInEx` 文件夹，选择合并。
4. 启动游戏并进入飞行场景。

安装后的关键文件结构如下：

```text
SimplePlanes 2/
├─ .doorstop_version
├─ doorstop_config.ini
├─ winhttp.dll
├─ BepInEx/
│  ├─ core/
│  └─ plugins/
│     └─ SP2FreeCamera.dll
└─ SimplePlanes 2.exe
```

发布包不包含 `BepInEx/config`、日志、缓存或其他插件，所以覆盖安装不会重置现有
Free Camera 配置。压缩包的 SHA-256 位于 `release/SHA256SUMS.txt`。

## 默认操作

| 操作 | 默认输入 |
| --- | --- |
| 进入/退出自由相机 | `Insert` |
| 打开/关闭独立菜单 | `ScrollLock` |
| 向前/向后移动 | `W` / `S` |
| 向左/向右移动 | `A` / `D` |
| 向上/向下移动 | `E` / `Q` |
| 普通/快速速度切换 | `Keypad5` |
| 锁定自己的载具或角色 | `Backspace` |
| 锁定游戏当前选中的目标 | `-`（主键盘数字行） |
| 锁定鼠标下的地形、部件或移动对象 | 鼠标中键 |
| 解除锁定并自由观察 | 按住鼠标左键拖拽 |
| 调整 FOV | 鼠标滚轮 |

配置文件在首次运行后生成：

```text
BepInEx/config/local.sp2.freecamera.cfg
```

插件内置 English 和简体中文界面。可从快捷菜单或完整设置菜单切换语言。

## 锁定行为

- 部件、离架武器、动态地面目标、自己的载具/角色和游戏当前目标，会在每个画面帧
  读取当前原始目标点并直接调整相机朝向。
- 动态锁定不使用目标预测或聚焦平滑，也不会带动相机位置。
- 菜单中的“地形点锁定平滑时间”只影响地形点等静态焦点。
- 左键开始拖拽后会解除当前锁定，恢复自由观察。

## 卸载

只卸载 Free Camera 时，删除：

```text
BepInEx/plugins/SP2FreeCamera.dll
```

如果还有其他插件使用 BepInEx，请勿删除 `BepInEx`、`winhttp.dll` 或
`doorstop_config.ini`。

## 隐私与发布边界

- 插件没有遥测、联网更新或数据上传功能。
- 仓库和发布包不包含个人配置、运行日志、缓存、账户信息、访问令牌、私网地址或
  开发机器的绝对路径。
- 游戏内目标名称仅用于本地界面展示；写入 BepInEx 日志时使用不含目标名称的通用提示。
- 分享 `BepInEx/LogOutput.log` 前仍建议自行检查其中由游戏或其他插件产生的内容。
- 发布包中的 BepInEx 来自官方 `5.4.23.5` Windows x64 发行包，并使用固定 SHA-256
  校验；不会从本机游戏目录复制 BepInEx、配置或其他插件。

第三方组件及许可证见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。本仓库尚未为
Free Camera 自身声明开源许可证；第三方许可证不授予 Free Camera 源码的再发布权。

## 从源码构建

构建需要：

- Windows PowerShell 5.1 或 PowerShell 7
- Visual Studio Build Tools（Roslyn C# 编译器）
- 本机安装的 `SimplePlanes 2`

通过环境变量指定游戏目录，然后构建：

```powershell
$env:SP2_GAME_DIR = '<SteamLibrary>\steamapps\common\SimplePlanes 2'
.\build.ps1
```

如果脚本无法自动找到编译器，可显式传入一个通用位置：

```powershell
.\build.ps1 -Csc '<VisualStudio>\MSBuild\Current\Bin\Roslyn\csc.exe'
```

构建并生成完整可部署压缩包：

```powershell
.\build.ps1 -Package
```

构建并部署 DLL 到本机游戏（游戏运行时脚本会拒绝覆盖）：

```powershell
.\build.ps1 -Deploy
```

构建结果位于 `bin/SP2FreeCamera.dll`，发布包位于 `release`。打包脚本只会从
BepInEx 官方发行地址下载版本 `5.4.23.5`，并校验官方 SHA-256：

```text
82f9878551030f54657792c0740d9d51a09500eeae1fba21106b0c441e6732c4
```

`verify-release.ps1` 会复核压缩包文件白名单、BepInEx 逐文件哈希、DLL 版本，
并扫描本机用户路径、仓库路径、UNC 路径和私网地址。

## 当前边界

- 当前仅支持非 VR 飞行场景。
- 最小 FOV 为 `0.1°`；极小 FOV 会使用自定义透视矩阵。
- 如果目标原始 Transform 只在物理帧更新，逐画面帧直瞄仍只能使用当时可见的原始位置。
- 实际游戏版本升级后，建议重新构建并进行飞行场景回归测试。
