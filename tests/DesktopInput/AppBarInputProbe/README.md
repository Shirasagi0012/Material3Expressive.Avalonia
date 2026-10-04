# AppBar desktop input probe

独立 Windows Win32 TopLevel，Fluent/Material 模板、ComposeAnimation 实时帧与当前显示 DPI。
检查上下栏/Scaffold/stock ScrollViewer 的触摸消费、正文惯性、新 press 中断、顶部直接拖动及吸附。

```powershell
dotnet run --project tests/DesktopInput/AppBarInputProbe -- --raw-input
dotnet run --project tests/DesktopInput/AppBarInputProbe
```

第一种向本窗口真实 Avalonia 输入根发送 RawTouch，不证明 OS touch delivery。
第二种调用 InitializeTouchInjection/InjectTouchInput，每个点先核验命中自己的窗口；
锁屏或其他窗口覆盖时失败退出，不绕过覆盖窗口。须在已解锁桌面运行。
输出 MODE、RenderScaling、状态与 PASS/FAIL，失败返回非零。
系统注入仍不能代替真实触摸屏、触控笔和辅助技术人工验收。
为取得 Avalonia 12.1.2 RawTouch 入口，测试工程开启 unstable input APIs 并局部读取 WindowImpl.Owner。
