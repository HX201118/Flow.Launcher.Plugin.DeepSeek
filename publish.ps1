# 推送到 GitHub 并打 tag 触发自动构建发布
# 前置：先在 https://github.com/new 建好名为 Flow.Launcher.Plugin.DeepSeek 的 Public 仓库（不要勾选初始化）
# 用法：右键"使用 PowerShell 运行"

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$repo = "HX201118/Flow.Launcher.Plugin.DeepSeek"
$remote = "https://github.com/$repo.git"
$tag = "v1.0.0"

Write-Host "==> 检查本地仓库" -ForegroundColor Cyan
if (-not (Test-Path ".git")) { git init }
git branch -M main

$origin = git remote get-url origin 2>$null
if ($LASTEXITCODE -ne 0) { git remote add origin $remote }
elseif ($origin -ne $remote) { git remote set-url origin $remote }

git add -A
git diff --cached --quiet
if ($LASTEXITCODE -ne 0) {
    git commit -m "chore: 更新插件"
    Write-Host "已提交本地修改" -ForegroundColor Yellow
}

Write-Host "==> 推送 main（首次会弹出 GitHub 登录，登录后自动继续）" -ForegroundColor Cyan
git push -u origin main
if ($LASTEXITCODE -ne 0) { throw "推送失败，请确认 GitHub 仓库 $repo 已创建，且已登录" }

Write-Host "==> 打 tag $tag 并推送" -ForegroundColor Cyan
git tag $tag -f
git push origin $tag -f

Write-Host ""
Write-Host "完成。接下来去这里等 Actions 跑完（约 1 分钟）：" -ForegroundColor Green
Write-Host "  https://github.com/$repo/actions"
Write-Host "Release 出来后，zip 地址为："
Write-Host "  https://github.com/$repo/releases/download/$tag/Flow.Launcher.Plugin.DeepSeek.zip"
