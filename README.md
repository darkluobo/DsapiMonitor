# DeepSeek 用量监控

WPF 桌面悬浮监控工具，实时追踪 DeepSeek API 账户余额和用量。

## 功能

- **余额监控** — 每 30 秒自动查询 API 余额，折线图展示延迟
- **用量追踪** — 通过余额差值累计今日/本月消耗，跨天/跨月自动重置
- **余额预警** — 余额低于阈值时红色警告提醒
- **余额变化提醒** — 每次消耗实时显示 `-¥0.0012`
- **每日限额** — 设置每日消费上限，超出后迷你悬浮框变红
- **迷你模式** — 点击 ▼ 缩成右下角小悬浮框，只显示余额
- **系统托盘** — 关闭窗口后最小化到托盘，双击恢复

## 截图

深色仪表盘风格，300×480 悬浮窗，始终置顶。

## 使用

1. 下载 `DsapiMonitor.exe`
2. 运行，点击 ⚙ 输入 DeepSeek API Key（`sk-...` 格式）
3. 余额和延迟自动开始监控
4. 点击 ▼ 切换迷你悬浮框
5. 右键托盘图标可退出

## 构建

```bash
dotnet build src/DsapiMonitor/DsapiMonitor.csproj

# 发布单文件 EXE
dotnet publish src/DsapiMonitor/DsapiMonitor.csproj \
  -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:EnableCompressionInSingleFile=true \
  -p:DebugType=None -p:DebugSymbols=false \
  -o publish
```

## 技术栈

- .NET 8 + WPF
- WebView2（登录窗口备用）
- Hardcodet.NotifyIcon.Wpf（系统托盘）
- Microsoft.Extensions.DependencyInjection
