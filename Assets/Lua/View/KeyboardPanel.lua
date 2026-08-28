local BasePanel = require "View.BasePanel"
local UIPanelManager = require "Logic.UIPanelManager"

local KeyboardPanel = BasePanel.New()

function KeyboardPanel.New()
	return KeyboardPanel:CreateInstance()
end

function KeyboardPanel:Ctor()
end

function KeyboardPanel:InitUIAndMetaData()
	self.ui.returnButton = self.ui.root:Find("Buttons/ReturnBt"):GetComponent("UnityEngine.UI.Button")
end

function KeyboardPanel:InitUIEvent()
	if self.ui.returnButton ~= nil then
		self.ui.returnButton.onClick:AddListener(function()
			self:OnClickReturn()
		end)
	end
end

function KeyboardPanel:OnClickReturn()
	print("[Lua] KeyboardPanel.OnClickReturn")
	UIPanelManager.Close("KeyboardPanel")
end

function KeyboardPanel:OnOpen(data)
	self.state.openParam = data
end

function KeyboardPanel:RefreshView(data)
end

function KeyboardPanel:OnHide()
end

function KeyboardPanel:OnDispose()
end

return KeyboardPanel
