---@diagnostic disable: undefined-global
local BasePanel = require "View.BasePanel"
local UIPanelManager = require "Logic.UIPanelManager"
local SavePanel = BasePanel.New()
local MaxSlotCount = 10
local SlotPrefabPath = "UI/SaveSlotItem"
local DefaultIconPath = "SaveIcon/Area_Dirtmouth"

function SavePanel.New()
	return SavePanel:CreateInstance()
end

function SavePanel:Ctor()
	self.state.slotItems = {}
end

function SavePanel:InitUIAndMetaData()
	self.ui.content = self.ui.root:Find("scroll_view/Viewport/Content")
	self.state.slotItems = {}
end

function SavePanel:InitUIEvent()
end

function SavePanel:OnOpen(data)
	self.state.openParam = data
	self:BuildSlotItems()
	self:RefreshSlotItems()
end

function SavePanel:RefreshView(data)
	self:RefreshSlotItems()
end

function SavePanel:BuildSlotItems()
	if #self.state.slotItems > 0 then
		return
	end

	for slotId = 1, MaxSlotCount do
		local itemObject = self.resourceManager:InstantiatePrefab(SlotPrefabPath, self.ui.content)
		itemObject.name = "SaveSlotItem_" .. slotId
		local itemTransform = itemObject.transform
		local item = {
			slotId = slotId,
			gameObject = itemObject,
			transform = itemTransform,
			button = itemObject:GetComponent("UnityEngine.UI.Button"),
			deleteButton = itemTransform:Find("btn_delete"):GetComponent("UnityEngine.UI.Button"),
			icon = itemTransform:Find("icon"):GetComponent("UnityEngine.UI.Image"),
			lblOrder = itemTransform:Find("lbl_order"):GetComponent("TMPro.TextMeshProUGUI"),
			lblDesc = itemTransform:Find("lbl_desc"):GetComponent("TMPro.TextMeshProUGUI"),
			info = itemTransform:Find("info"),
			lblLocation = itemTransform:Find("info/lbl_location"):GetComponent("TMPro.TextMeshProUGUI"),
			lblTime = itemTransform:Find("info/lbl_time"):GetComponent("TMPro.TextMeshProUGUI"),
			healthImg = itemTransform:Find("HealthUI/HealthImg"),
			soulPower = itemTransform:Find("HealthUI/HealthImg/SoulPower"):GetComponent("UnityEngine.UI.Image"),
		}

		item.button.onClick:AddListener(function()
			self:OnClickSlot(slotId)
		end)

		item.deleteButton.onClick:AddListener(function()
			self:OnClickDeleteSlot(slotId)
		end)

		self.state.slotItems[slotId] = item
	end
end

function SavePanel:RefreshSlotItems()
	for slotId = 1, MaxSlotCount do
		self:RefreshSlotItem(self.state.slotItems[slotId])
	end
end

function SavePanel:RefreshSlotItem(item)
	if item == nil or item.lblLocation == nil then
		return
	end

	local hasSlot = SaveManager.HasSlot(item.slotId)
	item.lblOrder.text = tostring(item.slotId) .. "."
	item.lblDesc.gameObject:SetActive(not hasSlot)
	item.info.gameObject:SetActive(hasSlot)
	item.healthImg.gameObject:SetActive(hasSlot)
	item.deleteButton.gameObject:SetActive(hasSlot)

	local iconPath = DefaultIconPath
	if hasSlot then
		item.lblLocation.text = SaveManager.GetSlotLocationName(item.slotId)
		item.lblTime.text = SaveManager.GetSlotPlayTimeText(item.slotId)
		iconPath = SaveManager.GetSlotIconPath(item.slotId)
		self:RefreshHealth(item, SaveManager.GetSlotCurrentHealth(item.slotId), SaveManager.GetSlotMaxHealth(item.slotId))
		item.soulPower.fillAmount = SaveManager.GetSlotSoulPowerRate(item.slotId)
	else
		item.lblDesc.text = "New Game"
		item.soulPower.fillAmount = 0
	end

	item.icon.sprite = self.resourceManager:LoadSprite(iconPath)
end

function SavePanel:RefreshHealth(item, currentHealth, maxHealth)
	for index = 1, 5 do
		local healthIcon = item.healthImg:Find("Health" .. index)
		healthIcon.gameObject:SetActive(index <= maxHealth and index <= currentHealth)
	end
end

function SavePanel:OnClickSlot(slotId)
	print("[Lua] SavePanel.OnClickSlot", slotId)
	SaveManager.LoadSlotOrNewGame(slotId)
end

function SavePanel:OnClickDeleteSlot(slotId)
	print("[Lua] SavePanel.OnClickDeleteSlot", slotId)

	UIPanelManager.Open("ConfirmPanel", {
		message = "Delete this save?",
		onConfirm = function()
			SaveManager.DeleteSlot(slotId)
			self:RefreshSlotItems()
		end,
	})
end

function SavePanel:OnHide()
end

function SavePanel:OnDispose()
	for _, item in pairs(self.state.slotItems) do
		if item.gameObject ~= nil then
			self.resourceManager:ReleaseInstance(item.gameObject)
		end
	end
	self.state.slotItems = {}
end

return SavePanel
