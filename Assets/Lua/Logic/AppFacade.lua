local ManagerRegistry = require "Logic.ManagerRegistry"
local CtrlManager = require "Logic.CtrlManager"

AppFacade = AppFacade or {}
local started = false
local startRequested = false

function AppFacade.Start()
	if started then
		print("[Lua] AppFacade.Start ignored: already started")
		return
	end

	if startRequested then
		print("[Lua] AppFacade.Start ignored: already requested")
		return
	end

	startRequested = true
	started = true
	print("[Lua] AppFacade.Start")

	ManagerRegistry.Init()
	CtrlManager.Init()
	CtrlManager.StartUp()
end

function AppFacade.OnSceneLoaded(level)
	ManagerRegistry.Refresh("game")
	ManagerRegistry.Refresh("ui")
	ManagerRegistry.Refresh("input")
	ManagerRegistry.Refresh("camera")
	ManagerRegistry.Refresh("sound")

	if CtrlManager.OnSceneLoaded then
		CtrlManager.OnSceneLoaded(level)
	end
end
function AppFacade.OnApplicationQuit()
	print("[Lua] AppFacade.OnApplicationQuit")

	if CtrlManager.Shutdown then
		CtrlManager.Shutdown()
	end
end

return AppFacade