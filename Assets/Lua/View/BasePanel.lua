local BasePanel = {}
BasePanel.__index = BasePanel

---创建一个面板“类”表，并将其元表指向 `BasePanel`。
---子类通常使用 `local XXXPanel = BasePanel.New()` 的方式定义，再在该类表上扩展显示与交互逻辑。
---如果传入现有表，则会在其基础上补齐面板基类能力。
---@param class table|nil 可选的现有类表；为空时会自动创建新表。
---@return table cls 已绑定 `BasePanel` 基类行为的面板类表。
function BasePanel.New(class)
	local cls = class or {}
	cls.__index = cls
	setmetatable(cls, BasePanel)
	return cls
end

---创建面板实例，并自动初始化基础状态后调用 `Ctor(...)`。
---@param ... any 透传给 `Ctor(...)` 的任意初始化参数。
---@return table instance 具体的面板实例对象。
function BasePanel:CreateInstance(...)
	local instance = setmetatable({
		visible = false,
		isInited = false,
		data = nil,
		ui = {},
		meta = {},
		state = {},
		definition = nil,
		context = nil,
		panelName = nil,
		prefabPath = nil,
		layer = "Normal",
		cache = true,
		isPopup = false,
		closeOnMask = false,
		gameObject = nil,
		transform = nil,
		resourceManager = nil,
	}, self)

	if instance.Ctor then
		instance:Ctor(...)
	end

	return instance
end

---面板构造函数（虚函数）。
---子类只建议在这里初始化自身状态，不建议实例化 Prefab 或查找 UI 节点。
function BasePanel:Ctor()
end

---注入面板配置。
---@param definition table PanelRegistry 中声明的面板配置。
function BasePanel:SetDefinition(definition)
	self.definition = definition or {}
	self.panelName = self.definition.name or self.panelName
	self.prefabPath = self.definition.prefabPath or self.prefabPath
	self.layer = self.definition.layer or self.layer or "Normal"
	self.cache = self.definition.cache ~= false
	self.isPopup = self.definition.isPopup == true
	self.closeOnMask = self.definition.closeOnMask == true
	self.meta = self.meta or {}
	self.meta.definition = self.definition
	self.meta.prefabPath = self.prefabPath
end

---注入 UI 框架上下文。
---@param context table UIPanelManager.Init 传入的上下文。
function BasePanel:SetContext(context)
	self.context = context or {}
	self.resourceManager = self.context.resourceManager or self.resourceManager
end

---获取当前面板应该挂载到的父节点。
---@return userdata|nil parent Transform 父节点 Transform。
function BasePanel:GetParent()
	if self.context == nil then
		return nil
	end

	if self.context.GetLayerRoot then
		return self.context.GetLayerRoot(self.layer)
	end

	local layerRoots = self.context.layerRoots
	if layerRoots ~= nil then
		return layerRoots[self.layer] or layerRoots.Normal or layerRoots.Default
	end

	return self.context.defaultRoot
end

---创建并挂载面板 Prefab。
function BasePanel:CreateGameObject()
	if self.gameObject ~= nil then
		return self.gameObject
	end

	if self.resourceManager == nil then
		print("[Lua] BasePanel.CreateGameObject failed, resourceManager is nil:", self.panelName or "UnnamedPanel")
		return nil
	end

	if self.prefabPath == nil or self.prefabPath == "" then
		print("[Lua] BasePanel.CreateGameObject failed, prefabPath is empty:", self.panelName or "UnnamedPanel")
		return nil
	end

	self.gameObject = self.resourceManager:InstantiatePrefab(self.prefabPath)

	if self.gameObject == nil then
		print("[Lua] BasePanel.CreateGameObject failed, instantiate returned nil:", self.prefabPath)
		return nil
	end

	self.transform = self.gameObject.transform
	self.ui.root = self.transform

	local parent = self:GetParent()
	if parent ~= nil and self.transform ~= nil then
		self.transform:SetParent(parent, false)
	end

	self.gameObject:SetActive(true)
	return self.gameObject
end

---显示面板。
---@param data any 打开面板时传入的业务参数。
function BasePanel:Show(data)
	self.visible = true
	self.data = data

	if self.gameObject ~= nil then
		self.gameObject:SetActive(true)
	end

	if not self.isInited then
		if self:CreateGameObject() == nil then
			self.visible = false
			return
		end

		self:InitUIAndMetaData()
		self:InitUIEvent()
		self.isInited = true
	end

	self:OnOpen(data)
	self:RefreshView(data)

	print("[Lua] Panel Show:", self.panelName or "UnnamedPanel")
end

---隐藏面板。
function BasePanel:Hide()
	self.visible = false
	self:OnHide()

	if self.gameObject ~= nil then
		self.gameObject:SetActive(false)
	end

	print("[Lua] Panel Hide:", self.panelName or "UnnamedPanel")
end

---释放面板资源。
function BasePanel:Dispose()
	if self.visible then
		self:Hide()
	end

	self:OnDispose()

	if self.gameObject ~= nil and self.resourceManager ~= nil then
		self.resourceManager:ReleaseInstance(self.gameObject)
	end

	self.visible = false
	self.isInited = false
	self.data = nil
	self.ui = {}
	self.meta = {}
	self.state = {}
	self.gameObject = nil
	self.transform = nil
end

---初始化 UI 节点与元数据（虚函数）。
function BasePanel:InitUIAndMetaData()
end

---初始化 UI 事件（虚函数）。
function BasePanel:InitUIEvent()
end

---打开面板时的前置逻辑（虚函数）。
---@param data any 当前打开时传入的数据。
function BasePanel:OnOpen(data)
end

---刷新界面表现（虚函数）。
---@param data any 当前打开时传入的数据。
function BasePanel:RefreshView(data)
end

---隐藏阶段回调（虚函数）。
function BasePanel:OnHide()
end

---最终销毁回调（虚函数）。
function BasePanel:OnDispose()
end

return BasePanel