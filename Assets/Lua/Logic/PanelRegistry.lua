local UIPanelManager = require "Logic.UIPanelManager"

local definitions = {
	{
		name = "DownloadPanel",
		modulePath = "View.DownloadPanel",
		prefabPath = "UI/DownloadPanel",
		layer = "Normal",
		showMode = "Replace",
		cache = true,
		isPopup = false,
	},
	{
		name = "ExitPanel",
		modulePath = "View.ExitPanel",
		prefabPath = "UI/ExitPanel",
		layer = "Popup",
		showMode = "Overlay",
		cache = true,
		isPopup = true,
	},
	{
		name = "KeyboardPanel",
		modulePath = "View.KeyboardPanel",
		prefabPath = "UI/KeyboardPanel",
		layer = "Popup",
		showMode = "Replace",
		cache = true,
		isPopup = true,
	},
	{
		name = "OptionPanel",
		modulePath = "View.OptionPanel",
		prefabPath = "UI/OptionPanel",
		layer = "Popup",
		showMode = "Replace",
		cache = true,
		isPopup = true,
	},
	{
		name = "PausePanel",
		modulePath = "View.PausePanel",
		prefabPath = "UI/PausePanel",
		layer = "Popup",
		showMode = "Replace",
		cache = true,
		isPopup = true,
	},
	{
		name = "StartPanel",
		modulePath = "View.StartPanel",
		prefabPath = "UI/StartPanel",
		layer = "Normal",
		showMode = "Replace",
		cache = true,
		isPopup = false,
	},
	{
		name = "TestPanel",
		modulePath = "View.TestPanel",
		prefabPath = "UI/TestPanel",
		layer = "Normal",
		showMode = "Replace",
		cache = true,
		isPopup = false,
	},
	{
		name = "TutorialPanel",
		modulePath = "View.TutorialPanel",
		prefabPath = "UI/TutorialPanel",
		layer = "Normal",
		showMode = "Replace",
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
