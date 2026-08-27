local BasePanel = require "View.BasePanel"
local ManagerRegistry = require "Logic.ManagerRegistry"

local uiContainer = {}

local TestPanel = BasePanel.New()

function TestPanel.New()
	return TestPanel:CreateInstance()
end

function TestPanel:Ctor()
	self.panelName = "TestPanel"
	self.prefabPath = "UI/TestPanel.prefab"
	self.gameObject = nil
	self.transform = nil
	self.resourceManager = nil
end

function TestPanel:InitUIAndMetaData()
	self.resourceManager = self.resourceManager or ManagerRegistry.Resource()

	if self.gameObject == nil and self.resourceManager ~= nil then
		self.gameObject = self.resourceManager:InstantiatePrefab(self.prefabPath)
	end

	if self.gameObject ~= nil then
		self.transform = self.gameObject.transform
		self.ui.root = self.transform
	end

	self.meta.prefabPath = self.prefabPath

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
	if self.gameObject ~= nil then
		self.gameObject:SetActive(true)
	end

	print("[Lua] TestPanel.RefreshView:", self.state.openMessage)
end

function TestPanel:OnHide()
	if self.gameObject ~= nil then
		self.gameObject:SetActive(false)
	end
end

function TestPanel:OnDispose()
	if self.gameObject ~= nil and self.resourceManager ~= nil then
		self.resourceManager:ReleaseInstance(self.gameObject)
		self.gameObject = nil
	end

	self.transform = nil
	self.resourceManager = nil
end

return TestPanel
