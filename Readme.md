# Flow.Launcher.Plugin.DeepSeek

Flow Launcher 插件：**在浏览器里直接调用网页版 DeepSeek 提问**，不需要 API Key、不消耗 token 额度。

改自 [Flow.Launcher.Plugin.ChatGPT](https://github.com/ruinmi/Flow.Launcher.Plugin.ChatGPT)，把 ChatGPT 的 `?prompt=` 换成了 DeepSeek 网页版实际支持的 `?q=` 接口，并补上了深度思考 / 联网搜索 / 自动发送这几个开关。

---

## 用法

```
ds 你的问题
```

输入 `ds` + 空格 + 问题，回车后默认浏览器直接打开 DeepSeek 网页版并带上你的提问。

结果列表有三条：

| 条目 | 作用 |
| --- | --- |
| 你的问题（第一行） | 按当前设置打开 DeepSeek 并提问 |
| 用深度思考（R1）提问 | 强制带 `thinking=1`，不受全局开关影响 |
| 复制这条提示词到剪贴板 | 只复制，不开浏览器 |

右键任意一条还能选：不带深度思考/联网打开、带联网搜索打开、带深度思考打开、复制 DeepSeek 链接。

## 插件设置

Flow Launcher → 设置 → 插件 → DeepSeek：

- **打开后自动发送提示词** — 默认开，对应网页端 `autosend=1`
- **默认开启深度思考（R1）** — 对应 `thinking=1`
- **默认开启联网搜索** — 对应 `search=1`
- **结果里始终显示"复制提示词"** — 默认开
- **提示词前缀（可留空）** — 会拼在每条提问最前面，例如"请用中文简洁回答"

## 安装

**方式一（推荐）**：把 `dist\DeepSeek-1.0.0` 整个文件夹复制到

```
%APPDATA%\FlowLauncher\Plugins\
```

然后重启 Flow Launcher。

**方式二**：右键运行 `install.ps1`，自动复制并重启 Flow Launcher。

## 重新编译

需要 .NET SDK 7.0 或更高版本。

```powershell
.\build.ps1      # 编译 + 打包到 dist\DeepSeek-1.0.0
.\install.ps1    # 安装到 Flow Launcher 并重启
```

或者直接用 dotnet：

```powershell
dotnet build Flow.Launcher.Plugin.DeepSeek\Flow.Launcher.Plugin.DeepSeek.csproj -c Release
```

目标框架 `net7.0-windows`，可在 Flow Launcher 1.x（.NET 7）和 2.x（.NET 8/9）宿主下加载。

## 目录结构

```
Flow.Launcher.Plugin.DeepSeek-main/
├─ Flow.Launcher.Plugin.DeepSeek/      插件源码
│  ├─ Main.cs                          查询、URL 拼装、右键菜单
│  ├─ Settings.cs                      设置项
│  ├─ ViewModels/                      设置面板 VM
│  ├─ Views/SettingsControl.xaml       设置面板 UI
│  ├─ Languages/{en,zh-cn}.xaml        多语言
│  ├─ Images/app.ico                   图标（16~256 七种尺寸）
│  └─ plugin.json                      插件元数据（ActionKeyword = ds）
├─ tests/
│  ├─ SmokeTest/                       逻辑冒烟测试（真实 IPublicAPI 代理）
│  └─ HostLoadTest/                    在 net9 宿主程序集下加载验证
├─ tools/make_icon.py                  重新生成图标
├─ dist/DeepSeek-1.0.0/                可直接安装的成品
├─ build.ps1                           编译 + 打包
└─ install.ps1                         安装 + 重启 Flow Launcher
```

## 已知限制

- `q` / `autosend` / `thinking` / `search` 是 DeepSeek 网页端使用的 URL 参数，属于社区总结的非官方接口。官方若调整，参数可能失效——失效后最差情况只是打开空白对话页。
- 首次使用需要先在浏览器里登录 DeepSeek 账号。
- 提示词拼进网址后超过 8000 字符时，插件会自动改为"复制到剪贴板 + 打开首页"，避免链接被浏览器截断。
- 需要 Flow Launcher 已装好的浏览器能正常打开 `https://chat.deepseek.com`。

## 测试情况

在真实 Flow Launcher 2.1.4 环境验证过：

- 插件被宿主正常加载：日志 `Total init cost for <DeepSeek> is <2ms>`
- 设置项自动落盘：`%APPDATA%\FlowLauncher\Settings\Plugins\Flow.Launcher.Plugin.DeepSeek\Settings.json`
- 逻辑测试 31 项全通过（涵盖空输入、中文编码、开关组合、超长提示词、右键菜单、图标解码、多语言键完整性），并在 `net7.0-windows` 与 `net9.0-windows` 两套运行时、两套 `Flow.Launcher.Plugin` 程序集下各跑一遍

```powershell
dotnet build tests\SmokeTest\SmokeTest.csproj -c Release
dotnet exec tests\SmokeTest\bin\Release\net7.0-windows\SmokeTest.dll
```
