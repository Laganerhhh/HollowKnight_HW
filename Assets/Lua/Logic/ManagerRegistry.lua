ManagerRegistry = ManagerRegistry or {}

local cache = {}
local getters = {}

local function tryGet(name, getter)
	local ok, value = pcall(getter)
	if not ok then
		print("[Lua] ManagerRegistry get failed:", name, value)
		return nil
	end
	return value
end

local function register(name, getter)
	getters[name] = getter
end

function ManagerRegistry.Init()
	cache = {}
	getters = {}

	register("resource", function()
		return ResourceManager.EnsureInstance()
	end)

	register("resourceUpdate", function()
		return ResourceUpdateBridge.EnsureInstance()
	end)

	register("bundle", function()
		return BundleManager.EnsureInstance()
	end)

	register("game", function()
		return GameManager.instance
	end)

	register("ui", function()
		return UIManager.Instance
	end)

	register("input", function()
		return InputManager.instance
	end)

	register("camera", function()
		return CameraManager.instance
	end)

	register("sound", function()
		return SoundManager.instance
	end)

	print("[Lua] ManagerRegistry.Init done")
end

function ManagerRegistry.Get(name, forceRefresh)
	local getter = getters[name]
	if not getter then
		print("[Lua] ManagerRegistry getter not found:", name)
		return nil
	end

	if forceRefresh then
		cache[name] = nil
	end

	if cache[name] ~= nil then
		return cache[name]
	end

	local value = tryGet(name, getter)
	if value ~= nil then
		cache[name] = value
	end
	return value
end

function ManagerRegistry.Refresh(name)
	return ManagerRegistry.Get(name, true)
end

function ManagerRegistry.Game()
	return ManagerRegistry.Get("game")
end

function ManagerRegistry.UI()
	return ManagerRegistry.Get("ui")
end

function ManagerRegistry.Resource()
	return ManagerRegistry.Get("resource")
end

function ManagerRegistry.ResourceUpdate()
	return ManagerRegistry.Get("resourceUpdate")
end

function ManagerRegistry.Input()
	return ManagerRegistry.Get("input")
end

function ManagerRegistry.Camera()
	return ManagerRegistry.Get("camera")
end

function ManagerRegistry.Sound()
	return ManagerRegistry.Get("sound")
end

return ManagerRegistry