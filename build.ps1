# 一键构建 + 打包
# 用法：右键"使用 PowerShell 运行"，或在命令行执行 .\build.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$proj = Join-Path $root "Flow.Launcher.Plugin.DeepSeek\Flow.Launcher.Plugin.DeepSeek.csproj"
$out = Join-Path $root "Flow.Launcher.Plugin.DeepSeek\bin\Release"
$dist = Join-Path $root "dist\DeepSeek-1.0.0"

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    throw "未找到 dotnet，请先安装 .NET SDK 7 或更高版本：https://dotnet.microsoft.com/download"
}

Write-Host "==> 编译" -ForegroundColor Cyan
& dotnet build $proj -c Release
if ($LASTEXITCODE -ne 0) { throw "编译失败" }

Write-Host "==> 打包到 dist\DeepSeek-1.0.0" -ForegroundColor Cyan
if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
New-Item -ItemType Directory -Path $dist -Force | Out-Null
Copy-Item "$out\*" $dist -Recurse -Force

Write-Host "完成：$dist" -ForegroundColor Green
