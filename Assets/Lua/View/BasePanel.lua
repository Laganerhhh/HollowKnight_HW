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
---基类默认会创建 `visible`、`isInited`、`data`、`ui`、`meta`、`state` 等字段，便于子类按“参考模板式”结构扩展。
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
	}, self)

	if instance.Ctor then
		instance:Ctor(...)
	end

	return instance
end

---面板构造函数（虚函数）。
---子类可在这里初始化基础字段，例如 `panelName`、`prefabPath`、缓存引用占位、默认状态等。
---建议只做轻量的“数据准备”，不要在此阶段实例化 Prefab 或访问尚未创建的 UI 节点。
---@param ... any 由 `CreateInstance(...)` 透传进来的初始化参数。
function BasePanel:Ctor()
end

---显示面板。
---这是基类固定的模板方法，建议子类不要重写，而是通过重写 `InitUIAndMetaData()`、`InitUIEvent()`、`OnOpen()`、`RefreshView()` 扩展行为。
---首次显示时会自动执行一次性初始化；之后每次打开都会进入刷新流程。
---@param data any 打开面板时传入的业务参数。
function BasePanel:Show(data)
	self.visible = true
	self.data = data

	if not self.isInited then
		self:InitUIAndMetaData()
		self:InitUIEvent()
		self.isInited = true
	end

	self:OnOpen(data)
	self:RefreshView(data)

	print("[Lua] Panel Show:", self.panelName or "UnnamedPanel")
end

---隐藏面板。
---基类会统一维护 `visible` 状态，并调用 `OnHide()` 供子类补充隐藏期逻辑。
function BasePanel:Hide()
	self.visible = false
	self:OnHide()
	print("[Lua] Panel Hide:", self.panelName or "UnnamedPanel")
end

---释放面板资源。
---基类默认会先调用 `Hide()`，再调用 `OnDispose()`，最后清空运行时状态表，便于下次重新创建时保持干净。
function BasePanel:Dispose()
	self:Hide()
	self:OnDispose()
	self.isInited = false
	self.data = nil
	self.ui = {}
	self.meta = {}
	self.state = {}
end

---初始化 UI 节点与元数据（虚函数）。
---仅在首次 `Show()` 时调用一次，适合在这里实例化 Prefab、缓存 Transform、查找控件、记录静态配置等。
function BasePanel:InitUIAndMetaData()
end

---初始化 UI 事件（虚函数）。
---仅在首次 `Show()` 时调用一次，适合在这里绑定按钮点击、Toggle 切换、滚动列表回调等事件。
---如果事件需要在隐藏时解绑，可配合 `OnHide()` 或 `OnDispose()` 实现。
function BasePanel:InitUIEvent()
end

---打开面板时的前置逻辑（虚函数）。
---每次 `Show()` 都会在 `RefreshView()` 前调用，适合做运行时状态重置、记录打开参数、切换显示标记等轻量逻辑。
---@param data any 当前打开时传入的数据。
function BasePanel:OnOpen(data)
end

---刷新界面表现（虚函数）。
---每次 `Show()` 都会调用，适合根据 `data` 或当前状态刷新文本、图片、列表、红点以及显隐逻辑。
---@param data any 当前打开时传入的数据。
function BasePanel:RefreshView(data)
end

---隐藏阶段回调（虚函数）。
---每次 `Hide()` 时调用，适合暂停特效、停止定时器、关闭子界面或移除临时监听。
function BasePanel:OnHide()
end

---最终销毁回调（虚函数）。
---面板彻底销毁时调用，适合释放实例化对象、清空强引用、注销全局事件、回收 Lua/C# 资源。
function BasePanel:OnDispose()
end

return BasePanel