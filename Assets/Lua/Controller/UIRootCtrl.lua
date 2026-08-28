local BaseCtrl = require "Controller.BaseCtrl"
local ManagerRegistry = require "Logic.ManagerRegistry"
local UIPanelManager = require "Logic.UIPanelManager"
local PanelRegistry = require "Logic.PanelRegistry"
local UIRootCtrl = BaseCtrl.New()

function UIRootCtrl.New()
	return UIRootCtrl:CreateInstance()
end

function UIRootCtrl:Ctor()
	self.uiManager = nil
	self.gameManager = nil
	self.resourceManager = nil
end

function UIRootCtrl:Start()
	self.uiManager = ManagerRegistry.UI()
	self.gameManager = ManagerRegistry.Game()
	self.resourceManager = ManagerRegistry.Resource()

	print("[Lua] UIRootCtrl.Start")
	print("[Lua] UIManager ready:", self.uiManager ~= nil)
	print("[Lua] GameManager ready:", self.gameManager ~= nil)
	print("[Lua] ResourceManager ready:", self.resourceManager ~= nil)

	UIPanelManager.Init({
		uiManager = self.uiManager,
		resourceManager = self.resourceManager,
	})

	PanelRegistry.RegisterAll()

	if UIPanelManager.GetDefinition("DownloadPanel") then
		UIPanelManager.Open("DownloadPanel", { message = "Lua UI startup success" })
	elseif UIPanelManager.GetDefinition("StartPanel") then
		UIPanelManager.Open("StartPanel", { message = "Lua UI startup success" })
	end
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
	self:RefreshManagers()
	print("[Lua] UIRootCtrl.OnSceneLoaded:", level)
	print("[Lua] UIManager ready after scene load:", self.uiManager ~= nil)
	print("[Lua] GameManager ready after scene load:", self.gameManager ~= nil)
end

function UIRootCtrl:Update(deltaTime)
	UIPanelManager.Update(deltaTime)
end

function UIRootCtrl:Dispose()
	UIPanelManager.DisposeAll()
end

return UIRootCtrl