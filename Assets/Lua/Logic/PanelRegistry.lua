local UIPanelManager = require "Logic.UIPanelManager"

local definitions = {
	{
		name = "ConfirmPanel",
		modulePath = "View.ConfirmPanel",
		prefabPath = "UI/ConfirmPanel",
		layer = "Popup",
		showMode = "Overlay",
		cache = true,
		isPopup = true,
	},
	{
		name = "DownloadPanel",
		modulePath = "View.DownloadPanel",
		prefabPath = "UI/DownloadPanel",
		layer = "Normal",
		cache = true,
		isPopup = false,
	},
	{
		name = "ExitPanel",
		modulePath = "View.ExitPanel",
		prefabPath = "UI/ExitPanel",
		layer = "Popup",
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
		name = "PausePanel",
		modulePath = "View.PausePanel",
		prefabPath = "UI/PausePanel",
		layer = "Normal",
		cache = true,
		isPopup = false,
	},
	{
		name = "SavePanel",
		modulePath = "View.SavePanel",
		prefabPath = "UI/SavePanel",
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
	{
		name = "TutorialPanel",
		modulePath = "View.TutorialPanel",
		prefabPath = "UI/TutorialPanel",
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
