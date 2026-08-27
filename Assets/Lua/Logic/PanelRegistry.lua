local UIPanelManager = require "Logic.UIPanelManager"

local definitions = {
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
