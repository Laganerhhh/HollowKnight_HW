- **第1步**：先做 `XXXPanel.prefab`
- **第2步**：右键它，点击 `创建Panel Lua`
- **第3步**：在生成的 `XXXPanel.lua` 里写显示逻辑、按钮绑定、文本刷新
- **第4步**：在业务控制器里调用
  `UIPanelManager.Open("XXXPanel", data)`