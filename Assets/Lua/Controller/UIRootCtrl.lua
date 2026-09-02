---@diagnostic disable: undefined-global
local BaseCtrl = require "Controller.BaseCtrl"
local ManagerRegistry = require "Logic.ManagerRegistry"
local UIPanelManager = require "Logic.UIPanelManager"
local PanelRegistry = require "Logic.PanelRegistry"
local UIRootCtrl = BaseCtrl.New()
local StartupSceneBuildIndex = 0
local SceneBoundPanels = {
	"DownloadPanel",
	"ExitPanel",
	"KeyboardPanel",
	"OptionPanel",
	"PausePanel",
	"StartPanel",
	"TestPanel",
	"TutorialPanel",
}

function UIRootCtrl.New()
	return UIRootCtrl:CreateInstance()
end

function UIRootCtrl:Ctor()
	self.uiManager = nil
	self.gameManager = nil
	self.resourceManager = nil
	self.hasRunStartupFlow = false
	self.currentSceneBuildIndex = StartupSceneBuildIndex
end

function UIRootCtrl:GetCurrentSceneBuildIndex()
	if type(self.currentSceneBuildIndex) ~= "number" then
		return StartupSceneBuildIndex
	end

	return self.currentSceneBuildIndex
end

function UIRootCtrl:SetCurrentSceneBuildIndex(level)
	if type(level) == "number" then
		self.currentSceneBuildIndex = level
		return
	end

	self.currentSceneBuildIndex = StartupSceneBuildIndex
end

function UIRootCtrl:ResetSceneBoundPanels()
	for _, panelName in ipairs(SceneBoundPanels) do
		UIPanelManager.Destroy(panelName)
	end
end

function UIRootCtrl:OpenStartupSceneUI()
	if not self.hasRunStartupFlow and UIPanelManager.GetDefinition("DownloadPanel") ~= nil then
		self.hasRunStartupFlow = true
		UIPanelManager.Open("DownloadPanel", { message = "Lua UI startup success" })
		return
	end

	self.hasRunStartupFlow = true

	if UIPanelManager.GetDefinition("StartPanel") ~= nil then
		UIPanelManager.Open("StartPanel", { message = "Lua UI startup success" })
	end
end

function UIRootCtrl:HandleSceneUI()
	local sceneBuildIndex = self:GetCurrentSceneBuildIndex()
	self:ResetSceneBoundPanels()

	if sceneBuildIndex == StartupSceneBuildIndex then
		self:OpenStartupSceneUI()
		return
	end

	print("[Lua] UIRootCtrl.HandleSceneUI skip startup panels for buildIndex:", sceneBuildIndex)
end

function UIRootCtrl:Start()
	self.uiManager = ManagerRegistry.UI()
	self.gameManager = ManagerRegistry.Game()
	self.resourceManager = ManagerRegistry.Resource()
	self:SetCurrentSceneBuildIndex(StartupSceneBuildIndex)

	print("[Lua] UIRootCtrl.Start")
	print("[Lua] UIManager ready:", self.uiManager ~= nil)
	print("[Lua] GameManager ready:", self.gameManager ~= nil)
	print("[Lua] ResourceManager ready:", self.resourceManager ~= nil)

	UIPanelManager.Init({
		uiManager = self.uiManager,
		resourceManager = self.resourceManager,
	})

	PanelRegistry.RegisterAll()
	self:HandleSceneUI()
end

function UIRootCtrl:RefreshManagers()
	self.uiManager = ManagerRegistry.Refresh("ui")
	self.gameManager = ManagerRegistry.Refresh("game")
	self.resourceManager = ManagerRegistry.Refresh("resource")

	UIPanelManager.Init({
		uiManager = self.uiManager,
		resourceManager = self.resourceManager,
	})
end

function UIRootCtrl:OnSceneLoaded(level)
	self:SetCurrentSceneBuildIndex(level)
	self:RefreshManagers()
	print("[Lua] UIRootCtrl.OnSceneLoaded:", level)
	print("[Lua] UIManager ready after scene load:", self.uiManager ~= nil)
	print("[Lua] GameManager ready after scene load:", self.gameManager ~= nil)
	self:HandleSceneUI()
end

function UIRootCtrl:Update(deltaTime)
	UIPanelManager.Update(deltaTime)
end

function UIRootCtrl:Dispose()
	UIPanelManager.DisposeAll()
end

return UIRootCtrl