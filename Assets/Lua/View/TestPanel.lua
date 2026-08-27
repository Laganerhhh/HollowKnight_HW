local BasePanel = require "View.BasePanel"

local uiContainer = {}

local TestPanel = BasePanel.New()

function TestPanel.New()
	return TestPanel:CreateInstance()
end

function TestPanel:Ctor()
end

function TestPanel:InitUIAndMetaData()
	uiContainer.img = self.ui.root:Find("Image"):GetComponent("UnityEngine.UI.Image")
	uiContainer.txt = self.ui.root:Find("text"):GetComponent("TMPro.TextMeshProUGUI")
	uiContainer.button = self.ui.root:Find("btn"):GetComponent("UnityEngine.UI.Button")

	uiContainer.img.sprite = self.resourceManager:LoadSprite("Assets/Textures/HollowKnightIcon.jpg")
	uiContainer.txt.text = "this is Test from Lua!"

	print("初始化数据")
end

function TestPanel:InitUIEvent()
	-- 在这里绑定按钮、Toggle、列表项点击等事件。

	uiContainer.button.onClick:AddListener(function()
		print("按钮点击")
	end)
end

function TestPanel:OnOpen(data)
	self.state.openMessage = data and data.message or ""
end

function TestPanel:RefreshView(data)
	print("[Lua] TestPanel.RefreshView:", self.state.openMessage)
end

function TestPanel:OnHide()
end

function TestPanel:OnDispose()
	uiContainer.img = nil
	uiContainer.txt = nil
	uiContainer.button = nil
end

return TestPanel
