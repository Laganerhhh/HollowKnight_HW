local UIPanelManager = require "Logic.UIPanelManager"

local definitions = {
	{ name = "TestPanel", module = require "View.TestPanel" },
}

PanelRegistry = PanelRegistry or {};

function PanelRegistry.RegisterAll()
	for _, definition in ipairs(definitions) do
		if UIPanelManager.Get(definition.name) == nil and definition.module and definition.module.New then
			UIPanelManager.Register(definition.name, definition.module.New())
			print("[Lua] PanelRegistry.Register:", definition.name)
		end
	end
end

return PanelRegistry
