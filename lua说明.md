- **第1步**：先做 `XXXPanel.prefab`
- **第2步**：右键它，点击 `创建Panel Lua`
- **第3步**：在生成的 `XXXPanel.lua` 里写显示逻辑、按钮绑定、文本刷新
- **第4步**：在业务控制器里调用
  `UIPanelManager.Open("XXXPanel", data)`

第一步：重构 Lua UIManager 架构，但只迁 TestPanel + 一个真实 UI
第二步：实现 Lua 文件 Addressables 热更新
第三步：验证远端修改 xxxPanel.lua 后 UI 行为可更新
第四步：批量迁移旧 C# UI 到 Lua Panel 