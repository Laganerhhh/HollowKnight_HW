local BasePanel = require "View.BasePanel"
local UIPanelManager = require "Logic.UIPanelManager"

local OptionPanel = BasePanel.New()

function OptionPanel.New()
	return OptionPanel:CreateInstance()
end

function OptionPanel:Ctor()
end

function OptionPanel:InitUIAndMetaData()
	self.ui.gameButton = self.ui.root:Find("Buttons/GameBt"):GetComponent("UnityEngine.UI.Button")
	self.ui.keyboardButton = self.ui.root:Find("Buttons/KeybordBt"):GetComponent("UnityEngine.UI.Button")
	self.ui.soundButton = self.ui.root:Find("Buttons/SoundBt"):GetComponent("UnityEngine.UI.Button")
	self.ui.videoButton = self.ui.root:Find("Buttons/VideoBt"):GetComponent("UnityEngine.UI.Button")
	self.ui.returnButton = self.ui.root:Find("Buttons/ReturnBt"):GetComponent("UnityEngine.UI.Button")
end

function OptionPanel:InitUIEvent()
	if self.ui.gameButton ~= nil then
		self.ui.gameButton.onClick:AddListener(function()
			self:OnClickGame()
		end)
	end

	if self.ui.keyboardButton ~= nil then
		self.ui.keyboardButton.onClick:AddListener(function()
			self:OnClickKeyboard()
		end)
	end

	if self.ui.soundButton ~= nil then
		self.ui.soundButton.onClick:AddListener(function()
			self:OnClickSound()
		end)
	end

	if self.ui.videoButton ~= nil then
		self.ui.videoButton.onClick:AddListener(function()
			self:OnClickVideo()
		end)
	end

	if self.ui.returnButton ~= nil then
		self.ui.returnButton.onClick:AddListener(function()
			self:OnClickReturn()
		end)
	end
end

function OptionPanel:OnClickGame()
	print("[Lua] OptionPanel.OnClickGame")
end

function OptionPanel:OnClickKeyboard()
	print("[Lua] OptionPanel.OnClickKeyboard")
	UIPanelManager.Open("KeyboardPanel")
end

function OptionPanel:OnClickSound()
	print("[Lua] OptionPanel.OnClickSound")
end

function OptionPanel:OnClickVideo()
	print("[Lua] OptionPanel.OnClickVideo")
end

function OptionPanel:OnClickReturn()
	print("[Lua] OptionPanel.OnClickReturn")
	UIPanelManager.Close("OptionPanel")
end

function OptionPanel:OnOpen(data)
	self.state.openParam = data
end

function OptionPanel:RefreshView(data)
end

function OptionPanel:OnHide()
end

function OptionPanel:OnDispose()
end

return OptionPanel
