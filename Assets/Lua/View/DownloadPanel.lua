local BasePanel = require "View.BasePanel"
local ManagerRegistry = require "Logic.ManagerRegistry"
local UIPanelManager = require "Logic.UIPanelManager"

local DownloadPanel = BasePanel.New()

local CompleteSwitchDelay = 0.5
local ProgressSmoothSpeed = 2.5
local DownloadLabels = ""

function DownloadPanel.New()
	return DownloadPanel:CreateInstance()
end

function DownloadPanel:Ctor()
	self.state.isUpdating = false
	self.state.switchTimer = nil
	self.state.targetProgress = 0
	self.state.displayedProgress = 0
	self.state.currentBytes = 0
	self.state.totalBytes = 0
	self.state.downloadBytesPerSecond = 0
end

function DownloadPanel:InitUIAndMetaData()
	self.ui.progressImage = self.ui.root:Find("progressBar/progress"):GetComponent("UnityEngine.UI.Image")
	self.ui.progressPercentText = self.ui.root:Find("progressPercentText"):GetComponent("TMPro.TextMeshProUGUI")
	self.ui.downloadSizeText = self.ui.root:Find("downloadSizeText"):GetComponent("TMPro.TextMeshProUGUI")
	self.ui.downloadSpeedText = self.ui.root:Find("downloadSpeedText"):GetComponent("TMPro.TextMeshProUGUI")
	self.ui.statusTip = self.ui.root:Find("statusTip"):GetComponent("TMPro.TextMeshProUGUI")
	self.ui.retryButton = self.ui.root:Find("retryBtn"):GetComponent("UnityEngine.UI.Button")
end

function DownloadPanel:InitUIEvent()
	if self.ui.retryButton ~= nil then
		self.ui.retryButton.onClick:RemoveAllListeners()
		self.ui.retryButton.onClick:AddListener(function()
			self:OnClickRetry()
		end)
	end
end

function DownloadPanel:OnOpen(data)
	self.state.openParam = data
	self.state.downloadLabels = data and data.labels or DownloadLabels
	self.state.completed = false
	self:ResetProgressView()
	self:StartUpdateFlow()
end

function DownloadPanel:RefreshView(data)
end

function DownloadPanel:OnUpdate(deltaTime)
	self:UpdateProgressVisual(deltaTime)
	self:UpdateSwitchTimer(deltaTime)
end

function DownloadPanel:OnHide()
end

function DownloadPanel:OnDispose()
	if self.ui.retryButton ~= nil then
		self.ui.retryButton.onClick:RemoveAllListeners()
	end
end

function DownloadPanel:OnClickRetry()
	if self.state.isUpdating then
		return
	end

	local bridge = self:GetBridge()
	if bridge ~= nil and bridge:IsRemoteUpdateInProgress() then
		return
	end

	self:StartUpdateFlow()
end

function DownloadPanel:GetBridge()
	if self.state.bridge ~= nil then
		return self.state.bridge
	end

	self.state.bridge = ManagerRegistry.ResourceUpdate()
	if self.state.bridge == nil then
		print("[Lua] ResourceUpdateBridge is not ready")
	end

	return self.state.bridge
end

function DownloadPanel:StartUpdateFlow()
	local bridge = self:GetBridge()
	if bridge == nil then
		self:OnDownloadError("Resource update bridge is not ready.")
		return
	end

	if bridge:IsRemoteUpdateInProgress() then
		return
	end

	self.state.isUpdating = true
	self.state.switchTimer = nil
	self:SetRetryVisible(false)
	self:UpdateStatus("Preparing to check resources...")
	self:SetProgressData(0, 0, 0, 0)

	bridge:StartUpdateByLabelString(
		self.state.downloadLabels or "",
		function(status)
			self:OnDownloadProgress(status)
		end,
		function()
			self:OnDownloadCompleted()
		end,
		function(message)
			self:OnDownloadError(message)
		end
	)
end

function DownloadPanel:OnDownloadProgress(status)
	if status == nil then
		return
	end

	self:UpdateStatus(status.StatusMessage)
	self:SetProgressData(status.Progress, status.DownloadedBytes, status.TotalBytes, status.DownloadBytesPerSecond)
end

function DownloadPanel:OnDownloadCompleted()
	self.state.isUpdating = false
	self.state.downloadBytesPerSecond = 0
	self.state.targetProgress = 1
	self.state.displayedProgress = 1
	self:ApplyProgressVisual(1, self.state.currentBytes, self.state.totalBytes, 0)
	self:UpdateStatus("Resource download completed.")
	self.state.switchTimer = CompleteSwitchDelay
end

function DownloadPanel:OnDownloadError(message)
	self.state.isUpdating = false
	self.state.downloadBytesPerSecond = 0
	self.state.switchTimer = nil
	self:ApplyProgressVisual(self.state.displayedProgress, self.state.currentBytes, self.state.totalBytes, 0)
	self:UpdateStatus(message ~= nil and message ~= "" and message or "Resource update failed.")
	self:SetRetryVisible(true)
	print("[Lua] DownloadPanel.OnDownloadError:", message)
end

function DownloadPanel:ResetProgressView()
	self.state.targetProgress = 0
	self.state.displayedProgress = 0
	self.state.currentBytes = 0
	self.state.totalBytes = 0
	self.state.downloadBytesPerSecond = 0
	self:SetRetryVisible(false)
	self:UpdateStatus("Preparing to check resources...")
	self:ApplyProgressVisual(0, 0, 0, 0)
end

function DownloadPanel:SetProgressData(progress, currentBytes, totalBytes, downloadBytesPerSecond)
	local targetProgress = self:Clamp01(self:ToNumber(progress, 0))
	self.state.targetProgress = targetProgress
	self.state.currentBytes = math.max(0, self:ToNumber(currentBytes, 0))
	self.state.totalBytes = math.max(0, self:ToNumber(totalBytes, 0))
	self.state.downloadBytesPerSecond = math.max(0, self:ToNumber(downloadBytesPerSecond, 0))

	if targetProgress < self.state.displayedProgress then
		self.state.displayedProgress = targetProgress
	end

	if targetProgress <= 0 and self.state.currentBytes <= 0 and self.state.totalBytes <= 0 then
		self.state.displayedProgress = 0
	end

	self:ApplyProgressVisual(self.state.displayedProgress, self.state.currentBytes, self.state.totalBytes, self.state.downloadBytesPerSecond)
end

function DownloadPanel:UpdateProgressVisual(deltaTime)
	local displayedProgress = self.state.displayedProgress or 0
	local targetProgress = self.state.targetProgress or 0
	if math.abs(displayedProgress - targetProgress) <= 0.0001 then
		return
	end

	local maxDelta = ProgressSmoothSpeed * (deltaTime or 0)
	if displayedProgress < targetProgress then
		displayedProgress = math.min(targetProgress, displayedProgress + maxDelta)
	else
		displayedProgress = math.max(targetProgress, displayedProgress - maxDelta)
	end

	self.state.displayedProgress = displayedProgress
	self:ApplyProgressVisual(displayedProgress, self.state.currentBytes, self.state.totalBytes, self.state.downloadBytesPerSecond)
end

function DownloadPanel:UpdateSwitchTimer(deltaTime)
	if self.state.switchTimer == nil then
		return
	end

	self.state.switchTimer = self.state.switchTimer - (deltaTime or 0)
	if self.state.switchTimer > 0 then
		return
	end

	self.state.switchTimer = nil
	UIPanelManager.Open("StartPanel")
	UIPanelManager.Close("DownloadPanel")
end

function DownloadPanel:ApplyProgressVisual(progress, currentBytes, totalBytes, downloadBytesPerSecond)
	local clampedProgress = self:Clamp01(progress or 0)

	if self.ui.progressImage ~= nil then
		self.ui.progressImage.fillAmount = clampedProgress
	end

	if self.ui.progressPercentText ~= nil then
		self.ui.progressPercentText.text = string.format("%.2f%%", clampedProgress * 100)
	end

	if self.ui.downloadSizeText ~= nil then
		self.ui.downloadSizeText.text = string.format("%s / %s", self:FormatSize(currentBytes), self:FormatSize(totalBytes))
	end

	if self.ui.downloadSpeedText ~= nil then
		self.ui.downloadSpeedText.text = string.format("%s/s", self:FormatSize(downloadBytesPerSecond))
	end
end

function DownloadPanel:UpdateStatus(message)
	if self.ui.statusTip ~= nil then
		self.ui.statusTip.text = message or ""
	end
end

function DownloadPanel:SetRetryVisible(visible)
	if self.ui.retryButton ~= nil then
		self.ui.retryButton.gameObject:SetActive(visible == true)
	end
end

function DownloadPanel:Clamp01(value)
	if value < 0 then
		return 0
	end

	if value > 1 then
		return 1
	end

	return value
end

function DownloadPanel:ToNumber(value, fallback)
	if value == nil then
		return fallback or 0
	end

	if type(value) == "number" then
		return value
	end

	local text = tostring(value)
	local numericText = string.match(text, "%-?%d+%.?%d*")
	local numberValue = tonumber(numericText)
	if numberValue == nil then
		return fallback or 0
	end

	return numberValue
end

function DownloadPanel:FormatSize(bytes)
	local safeBytes = math.max(0, self:ToNumber(bytes, 0))
	if safeBytes < 1024 then
		return string.format("%d B", safeBytes)
	end

	local kiloBytes = safeBytes / 1024
	if kiloBytes < 1024 then
		return string.format("%.1f KB", kiloBytes)
	end

	local megaBytes = kiloBytes / 1024
	if megaBytes < 1024 then
		return string.format("%.1f MB", megaBytes)
	end

	return string.format("%.1f GB", megaBytes / 1024)
end

return DownloadPanel
