SP2 Free Camera v{PLUGIN_VERSION}
================================

适用环境：Windows x64 / SimplePlanes 2 / BepInEx 5 Mono

安装：
1. 完全关闭 SimplePlanes 2。
2. 将本压缩包内的全部内容直接解压到游戏根目录，也就是
   “SimplePlanes 2.exe”所在的目录。
3. 允许合并 BepInEx 文件夹；本包不包含或覆盖任何个人配置。
4. 启动游戏并进入飞行场景。

常用操作：
- Insert：进入或退出自由相机
- ScrollLock：打开或关闭插件菜单
- W / S / A / D / E / Q：移动相机
- 小键盘 5：切换普通/快速移动
- 普通移动速度默认为 200 m/s，快速移动速度默认为 2000 m/s
- Backspace：锁定自己的载具或角色
- 主键盘减号：锁定游戏当前选中的目标
- 鼠标中键：锁定鼠标下的地形、部件或移动对象
- 按住鼠标左键拖动：解除锁定并自由观察
- 鼠标滚轮：调整 FOV

卸载插件：
删除 BepInEx\plugins\SP2FreeCamera.dll。若其他插件仍使用 BepInEx，
请勿删除 BepInEx、winhttp.dll 或 doorstop_config.ini。

配置文件会在首次运行后生成于：
BepInEx\config\local.sp2.freecamera.cfg

隐私说明：
- 插件不包含遥测、联网更新或数据上传功能。
- 本发布包不包含开发者或用户的配置、日志、缓存、账户信息或本机路径。
- 游戏内目标名称可以在本地界面显示，但写入日志时会使用不含名称的通用提示。

第三方组件与许可证请查看 THIRD_PARTY_NOTICES.txt 和 licenses 文件夹。
