UIPanelManager = UIPanelManager or {}

local panels = {}

function UIPanelManager.Register(name, panel)
	panels[name] = panel
	return panel
end

function UIPanelManager.Get(name)
	return panels[name]
end

function UIPanelManager.Open(name, data)
	local panel = panels[name]
	if not panel then
		print("[Lua] UIPanelManager.Open failed, panel not found:", name)
		return nil
	end

	panel:Show(data)
	return panel
end

function UIPanelManager.Close(name)
	local panel = panels[name]
	if panel and panel.Hide then
		panel:Hide()
	end
end

function UIPanelManager.DisposeAll()
	for _, panel in pairs(panels) do
		if panel.Dispose then
			panel:Dispose()
		end
	end

	panels = {}
end

return UIPanelManager