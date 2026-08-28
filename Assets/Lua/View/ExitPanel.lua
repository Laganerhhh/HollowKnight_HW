local BasePanel = require "View.BasePanel"
local UIPanelManager = require "Logic.UIPanelManager"

local ExitPanel = BasePanel.New()

function ExitPanel.New()
	return ExitPanel:CreateInstance()
end

function ExitPanel:Ctor()
end

function ExitPanel:InitUIAndMetaData()
	self.ui.yesButton = self.ui.root:Find("Buttons/YesBt"):GetComponent("UnityEngine.UI.Button")
	self.ui.noButton = self.ui.root:Find("Buttons/NoBt"):GetComponent("UnityEngine.UI.Button")
end

function ExitPanel:InitUIEvent()
	if self.ui.yesButton ~= nil then
		self.ui.yesButton.onClick:AddListener(function()
			self:OnClickYes()
		end)
	end

	if self.ui.noButton ~= nil then
		self.ui.noButton.onClick:AddListener(function()
			self:OnClickNo()
		end)
	end
end

function ExitPanel:OnClickYes()
	print("[Lua] ExitPanel.OnClickYes")

	local uiManager = self.context and self.context.uiManager or nil
	if uiManager ~= nil then
		uiManager:ExitGame()
		return
	end

	print("[Lua] ExitPanel.OnClickYes failed, UIManager is nil")
end

function ExitPanel:OnClickNo()
	print("[Lua] ExitPanel.OnClickNo")
	UIPanelManager.Close("ExitPanel")
end

function ExitPanel:OnOpen(data)
	self.state.openParam = data
end

function ExitPanel:RefreshView(data)
end

function ExitPanel:OnHide()
end

function ExitPanel:OnDispose()
end

return ExitPanel
