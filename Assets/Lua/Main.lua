require "Logic.AppFacade"

VERSION = "0.0.1" -- 版本号

local appUpdateListener = nil

--主入口函数。从这里开始Lua业务逻辑
function Main()
	print("logic start")

	if appUpdateListener == nil and UpdateBeat ~= nil then
		appUpdateListener = UpdateBeat:CreateListener(function()
			AppFacade.Update(Time.unscaledDeltaTime)
		end)
		UpdateBeat:AddListener(appUpdateListener)
	end

	coroutine.start(function()
		coroutine.step(1)
		AppFacade.Start()
	end)
end

--场景切换通知
function OnLevelWasLoaded(level)
	collectgarbage("collect")
	Time.timeSinceLevelLoad = 0
	AppFacade.OnSceneLoaded(level)
end

function OnApplicationQuit()
	if appUpdateListener ~= nil and UpdateBeat ~= nil then
		UpdateBeat:RemoveListener(appUpdateListener)
		appUpdateListener = nil
	end

	AppFacade.OnApplicationQuit()
end