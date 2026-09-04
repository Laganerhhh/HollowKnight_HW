local BasePanel = require "View.BasePanel"
local UIPanelManager = require "Logic.UIPanelManager"

local ConfirmPanel = BasePanel.New()

function ConfirmPanel.New()
	return ConfirmPanel:CreateInstance()
end

function ConfirmPanel:Ctor()
end

function ConfirmPanel:InitUIAndMetaData()
	self.ui.prompt = self.ui.root:Find("Buttons/Prompt/Text (TMP)"):GetComponent("TMPro.TextMeshProUGUI")
	self.ui.yesButton = self.ui.root:Find("Buttons/YesBt"):GetComponent("UnityEngine.UI.Button")
	self.ui.noButton = self.ui.root:Find("Buttons/NoBt"):GetComponent("UnityEngine.UI.Button")
end

function ConfirmPanel:InitUIEvent()
	self.ui.yesButton.onClick:AddListener(function()
		self:OnClickYes()
	end)

	self.ui.noButton.onClick:AddListener(function()
		self:OnClickNo()
	end)
end

function ConfirmPanel:OnClickYes()
	local openParam = self.state.openParam
	UIPanelManager.Close("ConfirmPanel")

	if openParam ~= nil and openParam.onConfirm ~= nil then
		openParam.onConfirm()
	end
end

function ConfirmPanel:OnClickNo()
	UIPanelManager.Close("ConfirmPanel")
end

function ConfirmPanel:OnOpen(data)
	self.state.openParam = data
end

function ConfirmPanel:RefreshView(data)
	local message = "Confirmed?"
	if data ~= nil and data.message ~= nil then
		message = data.message
	end

	self.ui.prompt.text = message
end

function ConfirmPanel:OnHide()
end

function ConfirmPanel:OnDispose()
end

return ConfirmPanel
