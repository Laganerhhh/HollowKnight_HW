require "Logic.AppFacade"

--主入口函数。从这里开始Lua业务逻辑
function Main()
	print("logic start")

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
	AppFacade.OnApplicationQuit()
end