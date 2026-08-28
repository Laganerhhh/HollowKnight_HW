local BasePanel = require "View.BasePanel"
local UIPanelManager = require "Logic.UIPanelManager"

local StartPanel = BasePanel.New()

function StartPanel.New()
	return StartPanel:CreateInstance()
end

function StartPanel:Ctor()
end

function StartPanel:InitUIAndMetaData()
	self.ui.startGameButton = self.ui.root:Find("Buttons/StartGameBt"):GetComponent("UnityEngine.UI.Button")
	self.ui.optionButton = self.ui.root:Find("Buttons/OptionBt"):GetComponent("UnityEngine.UI.Button")
	self.ui.exitButton = self.ui.root:Find("Buttons/ExitBt"):GetComponent("UnityEngine.UI.Button")
end

function StartPanel:InitUIEvent()
	if self.ui.startGameButton ~= nil then
		self.ui.startGameButton.onClick:AddListener(function()
			self:OnClickStartGame()
		end)
	end

	if self.ui.optionButton ~= nil then
		self.ui.optionButton.onClick:AddListener(function()
			self:OnClickOption()
		end)
	end

	if self.ui.exitButton ~= nil then
		self.ui.exitButton.onClick:AddListener(function()
			self:OnClickExit()
		end)
	end
end

function StartPanel:OnClickStartGame()
	print("[Lua] StartPanel.OnClickStartGame")

	local uiManager = self.context and self.context.uiManager or nil
	if uiManager ~= nil then
		uiManager:EnterNextScene()
		return
	end

	print("[Lua] StartPanel.OnClickStartGame failed, UIManager is nil")
end

function StartPanel:OnClickOption()
	print("[Lua] StartPanel.OnClickOption")

	if UIPanelManager.GetDefinition("OptionPanel") ~= nil then
		UIPanelManager.Open("OptionPanel")
		return
	end

	print("[Lua] OptionPanel has not been migrated yet")
end

function StartPanel:OnClickExit()
	print("[Lua] StartPanel.OnClickExit")

	UIPanelManager.Open("ExitPanel")
end

function StartPanel:OnOpen(data)
	self.state.openParam = data
end

function StartPanel:RefreshView(data)
end

function StartPanel:OnHide()
end

function StartPanel:OnDispose()
end

return StartPanel
