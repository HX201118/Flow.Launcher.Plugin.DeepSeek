# 把 dist\DeepSeek-1.0.0 安装到 Flow Launcher 并重启它
# 用法：右键"使用 PowerShell 运行"

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Join-Path $root "dist\DeepSeek-1.0.0"
$dst = Join-Path $env:APPDATA "FlowLauncher\Plugins\DeepSeek-1.0.0"

if (-not (Test-Path $src)) { throw "找不到 $src，请先运行 build.ps1" }

Write-Host "==> 复制到 $dst" -ForegroundColor Cyan
if (Test-Path $dst) { Remove-Item $dst -Recurse -Force }
New-Item -ItemType Directory -Path $dst -Force | Out-Null
Copy-Item "$src\*" $dst -Recurse -Force

Write-Host "==> 重启 Flow Launcher" -ForegroundColor Cyan
Get-Process Flow.Launcher -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

$exe = Join-Path $env:LOCALAPPDATA "FlowLauncher\Flow.Launcher.exe"
if (Test-Path $exe) {
    Start-Process $exe
    Write-Host "已启动 Flow Launcher，输入 ds 你的问题 试试。" -ForegroundColor Green
}
else {
    Write-Host "没找到 Flow.Launcher.exe，请手动启动 Flow Launcher。" -ForegroundColor Yellow
}
