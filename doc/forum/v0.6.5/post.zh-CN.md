# [插件发布][Windows] SP2 Free Camera v0.6.5：自由镜头与逐画面帧目标锁定

![SP2 Free Camera 自由镜头演示](https://raw.githubusercontent.com/EEEureka/SP2FreeCamera/5826edd1d8db51b0a2b8b8433f7c30c19cae4400/doc/forum/v0.6.5/assets/01-cinematic-overview.png)

我做了一个适用于 **SimplePlanes 2** 的本地自由相机插件：**SP2 Free Camera**。

它主要用于飞行观察、目标跟拍、截图和录屏。除了常规的六自由度移动、视角旋转和 FOV 调节，还能锁定自己的载具、游戏当前目标、飞机部件、离架武器，以及可识别的动态地面目标。

当前版本是 **v0.6.5**，重点优化了移动目标锁定：相机在每个画面帧直接朝向目标当时可用的原始位置；锁定本身只改变朝向，不会带动相机位置跟随目标平移。

## 动态目标锁定

下面这段是 HUD 关闭后的连续跟拍。目标高速俯冲、转向时仍能连续保持在画面中心，相机位置不会因为锁定而自动跟随目标移动。

![动态目标逐画面帧锁定演示](https://raw.githubusercontent.com/EEEureka/SP2FreeCamera/5826edd1d8db51b0a2b8b8433f7c30c19cae4400/doc/forum/v0.6.5/assets/02-dynamic-lock.gif)

对于部件、离架导弹或炸弹、动态地面目标、自己的载具或角色，以及游戏当前选中的目标，插件会：

- 在每个画面帧重新读取目标位置并直接调整相机朝向；
- 不使用目标位置预测；
- 不使用菜单中的聚焦平滑时间；
- 在没有移动输入时保持相机位置静止；
- 允许手动移动相机，同时继续朝向锁定目标。

菜单里的“地形点锁定平滑时间”只对地形点等静态焦点生效。按住鼠标左键开始拖拽时，会解除当前锁定并回到自由观察。

## 固定观察点近距离跟拍

下面的目标从远处逐渐接近相机。载具在画面中的尺寸持续增大，可以直观看出相机没有随目标一起平移，同时朝向仍保持连续。

![目标接近固定相机的连续跟拍](https://raw.githubusercontent.com/EEEureka/SP2FreeCamera/5826edd1d8db51b0a2b8b8433f7c30c19cae4400/doc/forum/v0.6.5/assets/03-close-follow.gif)

## 自由取景与录制

关闭 HUD 后，可以用自由相机寻找更适合截图或录屏的观察位置：

![自由相机电影化录制演示](https://raw.githubusercontent.com/EEEureka/SP2FreeCamera/5826edd1d8db51b0a2b8b8433f7c30c19cae4400/doc/forum/v0.6.5/assets/04-cinematic-capture.gif)

![海面上空载具近景](https://raw.githubusercontent.com/EEEureka/SP2FreeCamera/5826edd1d8db51b0a2b8b8433f7c30c19cae4400/doc/forum/v0.6.5/assets/05-aircraft-closeup.png)

![机场上空俯拍视角](https://raw.githubusercontent.com/EEEureka/SP2FreeCamera/5826edd1d8db51b0a2b8b8433f7c30c19cae4400/doc/forum/v0.6.5/assets/06-topdown-follow.png)

## 锁定方式

![锁定离架导弹后的游戏提示](https://raw.githubusercontent.com/EEEureka/SP2FreeCamera/5826edd1d8db51b0a2b8b8433f7c30c19cae4400/doc/forum/v0.6.5/assets/07-focus-notification.png)

- `Backspace`：锁定自己的当前载具；离开载具时可锁定自己的角色；
- 主键盘数字行的 `-`：锁定游戏目标系统当前选中的目标；
- 鼠标中键：锁定鼠标下的地形、飞机部件、动态地面目标或已经离架的导弹/炸弹；
- 鼠标左键拖拽：解除锁定并自由观察。

## 默认操作

| 操作 | 默认输入 |
| --- | --- |
| 进入/退出自由相机 | `Insert` |
| 打开/关闭完整设置菜单 | `ScrollLock` |
| 向前/向后移动 | `W` / `S` |
| 向左/向右移动 | `A` / `D` |
| 向上/向下移动 | `E` / `Q` |
| 切换普通/快速速度 | `Keypad5` |
| 锁定自己的载具或角色 | `Backspace` |
| 锁定游戏当前选中的目标 | 主键盘数字行的 `-` |
| 锁定鼠标下的对象 | 鼠标中键 |
| 解除锁定并自由观察 | 按住鼠标左键拖拽 |
| 调整 FOV | 鼠标滚轮 |

默认移动速度：

- 普通模式：`200 m/s`
- 快速模式：`2000 m/s`

启用自由相机时，无修饰键的 `W / A / S / D / Q / E` 会专用于移动相机，并暂时从载具和人物的键盘映射中屏蔽，避免相机与载具同时响应。鼠标、手柄和右键载具操作不受这一规则影响。

插件内置 English 和简体中文界面。移动速度、移动平滑、观察灵敏度、FOV、地形点锁定平滑、UI 和快捷键都可以在设置菜单中调整。

## 下载

- **完整安装包：** [SP2FreeCamera-v0.6.5-win-x64.zip](https://github.com/EEEureka/SP2FreeCamera/releases/download/v0.6.5/SP2FreeCamera-v0.6.5-win-x64.zip)
- **GitHub Release：** [SP2 Free Camera v0.6.5](https://github.com/EEEureka/SP2FreeCamera/releases/tag/v0.6.5)
- **项目仓库：** [EEEureka/SP2FreeCamera](https://github.com/EEEureka/SP2FreeCamera)
- **中文完整说明：** [README.zh-CN.md](https://github.com/EEEureka/SP2FreeCamera/blob/main/doc/README.zh-CN.md)
- **校验文件：** [SHA256SUMS.txt](https://github.com/EEEureka/SP2FreeCamera/releases/download/v0.6.5/SHA256SUMS.txt)

完整安装包 SHA-256：

```text
750333d5ba93ca5eb104215479914d1cb00027d71390b1607026599a4a7cb878
```

## 安装方法

安装包已经包含 **BepInEx 5.4.23.5 Windows x64**，不需要另行安装。

1. 完全关闭 `SimplePlanes 2`；
2. 下载上方完整安装包；
3. 将压缩包中的全部内容解压到游戏根目录，也就是 `SimplePlanes 2.exe` 所在目录；
4. 如果系统询问是否合并 `BepInEx` 文件夹，选择合并；
5. 启动游戏并进入飞行场景，按 `Insert` 启用自由相机。

安装后的关键路径如下：

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

发布包不包含个人配置、日志、缓存或其他插件，所以覆盖安装不会重置已有的 Free Camera 设置。首次运行后，配置文件会生成在：

```text
BepInEx/config/local.sp2.freecamera.cfg
```

从旧版本升级时，插件只会将仍然等于旧默认值 `20/200 m/s` 的速度配置迁移为 `200/2000 m/s`；其他自定义速度不会被改写。

## 卸载

如果只想移除 Free Camera，删除下面这个文件即可：

```text
BepInEx/plugins/SP2FreeCamera.dll
```

如果还有其他插件依赖 BepInEx，请不要删除整个 `BepInEx` 目录，也不要删除 `winhttp.dll` 或 `doorstop_config.ini`。

## 隐私与联网说明

- 插件只操作本地相机和本地 UI；
- 不修改载具物理；
- 不发送游戏网络消息；
- 没有遥测、联网更新或数据上传功能；
- 发布包不包含个人配置、运行日志、缓存、账户信息、访问令牌或开发机器路径；
- 目标名称只显示在本地 UI 中，插件日志使用不含目标名称的通用提示。

演示素材中的玩家名称来自游戏内 UI。分享自己的 `BepInEx/LogOutput.log` 前，仍建议先检查游戏或其他插件写入的内容。

## 当前限制

- 当前仅支持 Windows x64 的非 VR 飞行场景；
- 本版本针对游戏 `0.7.6.100f` 构建，游戏更新后可能需要重新验证兼容性；
- 最小 FOV 为 `0.1°`；
- 如果目标自身的 Transform 只在物理帧更新，那么插件虽然会在每个画面帧重新瞄准，也只能使用该帧当时能够读取到的最新原始位置；
- 仓库目前公开可见，但尚未为 Free Camera 源码声明开源许可证。如需改编或再发布源码，请先联系作者。

遇到问题时，可以在 [GitHub Issues](https://github.com/EEEureka/SP2FreeCamera/issues) 中反馈，并尽量附上游戏版本、插件版本和复现步骤。
