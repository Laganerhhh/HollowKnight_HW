local BasePanel = require "View.BasePanel"
local UIPanelManager = require "Logic.UIPanelManager"

local PausePanel = BasePanel.New()

function PausePanel.New()
	return PausePanel:CreateInstance()
end

function PausePanel:Ctor()
end

function PausePanel:InitUIAndMetaData()
	self.ui.continueGameButton = self.ui.root:Find("Buttons/ContinueGameBt"):GetComponent("UnityEngine.UI.Button")
	self.ui.optionButton = self.ui.root:Find("Buttons/OptionBt"):GetComponent("UnityEngine.UI.Button")
	self.ui.exitToMenuButton = self.ui.root:Find("Buttons/ExitToMenuBt"):GetComponent("UnityEngine.UI.Button")
end

function PausePanel:InitUIEvent()
	if self.ui.continueGameButton ~= nil then
		self.ui.continueGameButton.onClick:AddListener(function()
			self:OnClickContinueGame()
		end)
	end

	if self.ui.optionButton ~= nil then
		self.ui.optionButton.onClick:AddListener(function()
			self:OnClickOption()
		end)
	end

	if self.ui.exitToMenuButton ~= nil then
		self.ui.exitToMenuButton.onClick:AddListener(function()
			self:OnClickExitToMenu()
		end)
	end
end

function PausePanel:OnClickContinueGame()
	print("[Lua] PausePanel.OnClickContinueGame")

	local uiManager = self.context and self.context.uiManager or nil
	if uiManager ~= nil then
		uiManager:ResumeGame()
		return
	end

	print("[Lua] PausePanel.OnClickContinueGame failed, UIManager is nil")
end

function PausePanel:OnClickOption()
	print("[Lua] PausePanel.OnClickOption")

	if UIPanelManager.GetDefinition("OptionPanel") ~= nil then
		UIPanelManager.Open("OptionPanel")
		return
	end

	print("[Lua] PausePanel.OnClickOption failed, OptionPanel is not registered")
end

function PausePanel:OnClickExitToMenu()
	print("[Lua] PausePanel.OnClickExitToMenu")

	local uiManager = self.context and self.context.uiManager or nil
	if uiManager ~= nil then
		uiManager:ReturnToMainMenu()
		return
	end

	print("[Lua] PausePanel.OnClickExitToMenu failed, UIManager is nil")
end

function PausePanel:OnOpen(data)
	self.state.openParam = data
end

function PausePanel:RefreshView(data)
end

function PausePanel:OnHide()
end

function PausePanel:OnDispose()
end

return PausePanel
