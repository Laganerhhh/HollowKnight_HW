local UIPanelManager = require "Logic.UIPanelManager"

local definitions = {
	{
		name = "DownloadPanel",
		modulePath = "View.DownloadPanel",
		prefabPath = "UI/DownloadPanel",
		layer = "Normal",
		cache = false,
		isPopup = false,
	},
	{
		name = "ExitPanel",
		modulePath = "View.ExitPanel",
		prefabPath = "UI/ExitPanel",
		layer = "Normal",
		cache = true,
		isPopup = true,
	},
	{
		name = "KeyboardPanel",
		modulePath = "View.KeyboardPanel",
		prefabPath = "UI/KeyboardPanel",
		layer = "Normal",
		cache = true,
		isPopup = false,
	},
	{
		name = "OptionPanel",
		modulePath = "View.OptionPanel",
		prefabPath = "UI/OptionPanel",
		layer = "Normal",
		cache = true,
		isPopup = false,
	},
	{
		name = "StartPanel",
		modulePath = "View.StartPanel",
		prefabPath = "UI/StartPanel",
		layer = "Normal",
		cache = true,
		isPopup = false,
	},
	{
		name = "TestPanel",
		modulePath = "View.TestPanel",
		prefabPath = "UI/TestPanel",
		layer = "Normal",
		cache = true,
		isPopup = false,
	},
}

PanelRegistry = PanelRegistry or {}

function PanelRegistry.RegisterAll()
	for _, definition in ipairs(definitions) do
		UIPanelManager.Register(definition)
		print("[Lua] PanelRegistry.Register:", definition.name)
	end
end

return PanelRegistry
