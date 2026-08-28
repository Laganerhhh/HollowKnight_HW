UIPanelManager = UIPanelManager or {}

local panelDefs = {}
local panelInstances = {}
local panelStack = {}
local context = {}
local layerRoots = {}

local DefaultLayer = "Normal"

local function getPanelLayer(name)
	local panel = panelInstances[name]
	if panel ~= nil and panel.layer ~= nil then
		return panel.layer
	end

	local definition = panelDefs[name]
	if definition ~= nil and definition.layer ~= nil then
		return definition.layer
	end

	return DefaultLayer
end

local function removeFromStack(name)
	for index = #panelStack, 1, -1 do
		if panelStack[index] == name then
			table.remove(panelStack, index)
		end
	end
end

local function findTopPanelNameInLayer(layer, ignoredName)
	for index = #panelStack, 1, -1 do
		local name = panelStack[index]
		if name ~= ignoredName and getPanelLayer(name) == layer then
			return name
		end
	end

	return nil
end

local function hideCurrentTopInLayer(layer, ignoredName)
	local topName = findTopPanelNameInLayer(layer, ignoredName)
	if topName == nil then
		return
	end

	local topPanel = panelInstances[topName]
	if topPanel ~= nil and topPanel.visible == true and topPanel.Hide ~= nil then
		topPanel:Hide()
	end
end

local function restoreTopInLayer(layer)
	local topName = findTopPanelNameInLayer(layer, nil)
	if topName == nil then
		return
	end

	local topPanel = panelInstances[topName]
	if topPanel ~= nil and topPanel.visible ~= true and topPanel.Show ~= nil then
		topPanel:Show(topPanel.data)
	end
end

local function getTransformFromGameObject(gameObject)
	if gameObject ~= nil then
		return gameObject.transform
	end
	return nil
end

local function getDefaultRoot(uiManager)
	if uiManager ~= nil and uiManager.transform ~= nil then
		return uiManager.transform
	end
	return nil
end

local function findLayerRoot(defaultRoot, layer)
	if defaultRoot == nil or layer == nil then
		return nil
	end

	local root = defaultRoot:Find(layer)
	if root ~= nil then
		return root
	end

	root = defaultRoot:Find(layer .. "Root")
	if root ~= nil then
		return root
	end

	return nil
end

local function rebuildLayerRoots()
	layerRoots = {}

	local defaultRoot = context.defaultRoot or getDefaultRoot(context.uiManager)
	context.defaultRoot = defaultRoot

	if context.uiManager ~= nil and context.uiManager.GetLayerRoot ~= nil then
		layerRoots.Default = context.uiManager:GetLayerRoot(DefaultLayer)
		layerRoots.Normal = context.uiManager:GetLayerRoot("Normal")
		layerRoots.Popup = context.uiManager:GetLayerRoot("Popup")
		layerRoots.Top = context.uiManager:GetLayerRoot("Top")
		layerRoots.Toast = context.uiManager:GetLayerRoot("Toast")
	else
		layerRoots.Default = defaultRoot
		layerRoots.Normal = findLayerRoot(defaultRoot, "Normal") or defaultRoot
		layerRoots.Popup = findLayerRoot(defaultRoot, "Popup") or layerRoots.Normal
		layerRoots.Top = findLayerRoot(defaultRoot, "Top") or layerRoots.Popup
		layerRoots.Toast = findLayerRoot(defaultRoot, "Toast") or layerRoots.Top
	end

	context.layerRoots = layerRoots
	context.GetLayerRoot = function(layer)
		return layerRoots[layer] or layerRoots[DefaultLayer] or layerRoots.Default
	end
end

function UIPanelManager.Init(initContext)
	context = initContext or {}
	context.defaultRoot = context.defaultRoot or getTransformFromGameObject(context.defaultRootGameObject) or getDefaultRoot(context.uiManager)
	rebuildLayerRoots()

	for _, panel in pairs(panelInstances) do
		if panel.SetContext then
			panel:SetContext(context)
		end
	end

	print("[Lua] UIPanelManager.Init done, defaultRoot:", context.defaultRoot ~= nil)
end

function UIPanelManager.Register(definition)
	if definition == nil or definition.name == nil then
		print("[Lua] UIPanelManager.Register failed, invalid definition")
		return nil
	end

	definition.layer = definition.layer or DefaultLayer
	definition.cache = definition.cache ~= false
	panelDefs[definition.name] = definition
	return definition
end

function UIPanelManager.GetDefinition(name)
	return panelDefs[name]
end

function UIPanelManager.Get(name)
	return panelInstances[name]
end

function UIPanelManager.IsOpen(name)
	local panel = panelInstances[name]
	return panel ~= nil and panel.visible == true
end

local function createPanel(name)
	local definition = panelDefs[name]
	if definition == nil then
		print("[Lua] UIPanelManager.Create failed, definition not found:", name)
		return nil
	end

	if definition.modulePath == nil or definition.modulePath == "" then
		print("[Lua] UIPanelManager.Create failed, modulePath is empty:", name)
		return nil
	end

	local module = require(definition.modulePath)
	if module == nil or module.New == nil then
		print("[Lua] UIPanelManager.Create failed, module.New not found:", definition.modulePath)
		return nil
	end

	local panel = module.New()
	panel:SetDefinition(definition)
	panel:SetContext(context)
	return panel
end

function UIPanelManager.Open(name, data)
	local definition = panelDefs[name]
	if definition == nil then
		print("[Lua] UIPanelManager.Open failed, definition not found:", name)
		return nil
	end

	local panel = panelInstances[name]
	if panel == nil then
		panel = createPanel(name)
		if panel == nil then
			return nil
		end

		panelInstances[name] = panel
	end

	hideCurrentTopInLayer(panel.layer or definition.layer or DefaultLayer, name)
	panel:Show(data)
	removeFromStack(name)
	table.insert(panelStack, name)
	return panel
end

function UIPanelManager.Close(name)
	local panel = panelInstances[name]
	if panel == nil then
		return
	end

	local layer = panel.layer or getPanelLayer(name)
	local wasVisible = panel.visible == true
	removeFromStack(name)

	local definition = panelDefs[name]
	if definition ~= nil and definition.cache == false then
		panel:Dispose()
		panelInstances[name] = nil
		if wasVisible then
			restoreTopInLayer(layer)
		end
		return
	end

	if panel.Hide then
		panel:Hide()
	end

	if wasVisible then
		restoreTopInLayer(layer)
	end
end

function UIPanelManager.Back()
	local name = panelStack[#panelStack]
	if name == nil then
		print("[Lua] UIPanelManager.Back skipped, stack is empty")
		return
	end

	UIPanelManager.Close(name)
end

function UIPanelManager.Destroy(name)
	local panel = panelInstances[name]
	local layer = getPanelLayer(name)
	local wasVisible = panel ~= nil and panel.visible == true
	if panel ~= nil and panel.Dispose ~= nil then
		panel:Dispose()
	end

	removeFromStack(name)
	panelInstances[name] = nil

	if wasVisible then
		restoreTopInLayer(layer)
	end
end

function UIPanelManager.Update(deltaTime)
	for _, panel in pairs(panelInstances) do
		if panel.visible == true and panel.OnUpdate ~= nil then
			panel:OnUpdate(deltaTime)
		end
	end
end

function UIPanelManager.DisposeAll()
	for name, panel in pairs(panelInstances) do
		if panel.Dispose then
			panel:Dispose()
		end
		panelInstances[name] = nil
	end

	panelDefs = {}
	panelInstances = {}
	panelStack = {}
	context = {}
	layerRoots = {}
end

return UIPanelManager