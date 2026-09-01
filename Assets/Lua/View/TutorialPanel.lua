local BasePanel = require "View.BasePanel"

local TutorialPanel = BasePanel.New()

local TutorialTypeNames = {
	[0] = "Jump",
	[1] = "Attack",
	[2] = "Climb",
	[3] = "SuperDash",
	[4] = "Recover",
	[5] = "Skill",
	[6] = "Dash",
	[7] = "Attack_Down",
}

local TutorialNodeNames = {
	[0] = "tutorial_jump",
	[1] = "tutorial_attack",
	[2] = "tutorial_climb",
	[3] = "tutorial_superdash",
	[4] = "tutorial_recover",
	[5] = "tutorial_skill",
	[6] = "tutorial_dash",
	[7] = "tutorial_attack_down",
}

local activeHideTokens = {}

function TutorialPanel.New()
	return TutorialPanel:CreateInstance()
end

function TutorialPanel:Ctor()
end

function TutorialPanel:CreateGameObject()
	if self.gameObject ~= nil then
		return self.gameObject
	end

	local rootGameObject = self.data and self.data.rootGameObject or nil
	if rootGameObject == nil then
		print("[Lua] TutorialPanel.CreateGameObject failed, rootGameObject is nil")
		return nil
	end

	self:BindRootGameObject(rootGameObject)
	return self.gameObject
end

function TutorialPanel:BindRootGameObject(rootGameObject)
	if rootGameObject == nil then
		return
	end

	if self.gameObject == rootGameObject and self.ui.root ~= nil then
		return
	end

	self.gameObject = rootGameObject
	self.transform = rootGameObject.transform
	self.ui.root = self.transform
	self.gameObject:SetActive(true)
	self:InitUIAndMetaData()
	self.isInited = true
end

function TutorialPanel:InitUIAndMetaData()
	self.ui.tutorials = {}

	for typeValue, nodeName in pairs(TutorialNodeNames) do
		local node = self.ui.root:Find(nodeName)
		if node ~= nil then
			self.ui.tutorials[typeValue] = node.gameObject
			self.ui.tutorials[typeValue]:SetActive(false)
		else
			print("[Lua] TutorialPanel missing node:", nodeName)
		end
	end
end

function TutorialPanel:InitUIEvent()
end

function TutorialPanel:OnOpen(data)
	self.state.openParam = data

	if data ~= nil and data.rootGameObject ~= nil then
		self:BindRootGameObject(data.rootGameObject)
	end

	if data ~= nil and data.typeValue ~= nil then
		self:ShowTutorial(data.typeValue, data.displayTime)
	end
end

function TutorialPanel:RefreshView(data)
end

function TutorialPanel:OnHide()
	self:HideAllTutorials()
end

function TutorialPanel:OnDispose()
	activeHideTokens = {}
end

function TutorialPanel:IsValidType(typeValue)
	return TutorialNodeNames[typeValue] ~= nil
end

function TutorialPanel:GetTutorial(typeValue)
	if not self:IsValidType(typeValue) then
		print("[Lua] TutorialPanel invalid tutorial type:", typeValue)
		return nil
	end

	return self.ui.tutorials and self.ui.tutorials[typeValue] or nil
end

function TutorialPanel:HideAllTutorials()
	if self.ui.tutorials == nil then
		return
	end

	for _, tutorial in pairs(self.ui.tutorials) do
		if tutorial ~= nil then
			tutorial:SetActive(false)
		end
	end
end

function TutorialPanel:ShowTutorial(typeValue, displayTime)
	typeValue = tonumber(typeValue)
	displayTime = tonumber(displayTime) or 5

	local tutorial = self:GetTutorial(typeValue)
	if tutorial == nil then
		return
	end

	if tutorial.activeSelf then
		return
	end

	self:HideAllTutorials()
	tutorial:SetActive(true)

	print("[Lua] TutorialPanel.ShowTutorial:", TutorialTypeNames[typeValue] or tostring(typeValue), displayTime)
end

function TutorialPanel:HideTutorial(typeValue)
	typeValue = tonumber(typeValue)

	local tutorial = self:GetTutorial(typeValue)
	if tutorial == nil then
		return
	end

	activeHideTokens[typeValue] = (activeHideTokens[typeValue] or 0) + 1
	tutorial:SetActive(false)
	print("[Lua] TutorialPanel.HideTutorial:", TutorialTypeNames[typeValue] or tostring(typeValue))
end

TutorialPanelBridge = TutorialPanelBridge or {}

function TutorialPanelBridge.ShowTutorial(rootGameObject, typeValue, displayTime)
	if UIPanelManager == nil then
		print("[Lua] TutorialPanelBridge.ShowTutorial failed, UIPanelManager is nil")
		return
	end

	UIPanelManager.Open("TutorialPanel", {
		rootGameObject = rootGameObject,
		typeValue = typeValue,
		displayTime = displayTime,
	})
end

function TutorialPanelBridge.HideTutorial(rootGameObject, typeValue)
	if UIPanelManager == nil then
		print("[Lua] TutorialPanelBridge.HideTutorial failed, UIPanelManager is nil")
		return
	end

	local panel = UIPanelManager.Get("TutorialPanel")
	if panel == nil then
		UIPanelManager.Open("TutorialPanel", {
			rootGameObject = rootGameObject,
		})
		panel = UIPanelManager.Get("TutorialPanel")
	end

	if panel ~= nil then
		panel:HideTutorial(typeValue)
	end
end

return TutorialPanel