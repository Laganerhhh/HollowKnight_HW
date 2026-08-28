local UIRootCtrl = require "Controller.UIRootCtrl"

CtrlManager = CtrlManager or {}
local ctrls = {}

function CtrlManager.Init()
	ctrls = {}
	print("[Lua] CtrlManager.Init")
end

function CtrlManager.Register(name, ctrl)
	ctrls[name] = ctrl
	return ctrl
end

function CtrlManager.Get(name)
	return ctrls[name]
end

function CtrlManager.StartUp()
	local uiRootCtrl = CtrlManager.Register("UIRootCtrl", UIRootCtrl.New())
	uiRootCtrl:Start()
	print("[Lua] CtrlManager.StartUp complete")
end

function CtrlManager.OnSceneLoaded(level)
	for _, ctrl in pairs(ctrls) do
		if ctrl.OnSceneLoaded then
			ctrl:OnSceneLoaded(level)
		end
	end
end

function CtrlManager.Update(deltaTime)
	for _, ctrl in pairs(ctrls) do
		if ctrl.Update then
			ctrl:Update(deltaTime)
		end
	end
end

function CtrlManager.Shutdown()
	for _, ctrl in pairs(ctrls) do
		if ctrl.Dispose then
			ctrl:Dispose()
		end
	end

	ctrls = {}
	print("[Lua] CtrlManager.Shutdown")
end

return CtrlManager