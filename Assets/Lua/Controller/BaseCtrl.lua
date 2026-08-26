local BaseCtrl = {}
BaseCtrl.__index = BaseCtrl

---创建一个控制器“类”表，并将其元表指向 `BaseCtrl`。
---子类通常使用 `local XXXCtrl = BaseCtrl.New()` 的方式声明，随后在该表上实现自己的成员函数。
---如果传入现有表，则会在保留原字段的基础上补齐控制器基类能力。
---@param class table|nil 可选的现有类表；为空时会自动创建新表。
---@return table cls 已绑定 `BaseCtrl` 基类行为的控制器类表。
function BaseCtrl.New(class)
	local cls = class or {}
	cls.__index = cls
	setmetatable(cls, BaseCtrl)
	return cls
end

---创建控制器实例，并在实例创建后自动调用 `Ctor(...)` 完成初始化。
---这里通常只负责“构造期”逻辑，不建议在此处直接执行依赖外部系统完整就绪的业务。
---如果子类需要保存初始参数、建立字段默认值、缓存配置数据，推荐重写 `Ctor(...)`。
---@param ... any 透传给 `Ctor(...)` 的任意初始化参数。
---@return table instance 具体的控制器实例对象。
function BaseCtrl:CreateInstance(...)
	local instance = setmetatable({}, self)
	if instance.Ctor then
		instance:Ctor(...)
	end
	return instance
end

---控制器构造函数（虚函数）。
---子类可重写此函数，用于初始化成员变量、设置默认状态、记录外部传入参数等。
---建议只做轻量初始化，避免在这里执行依赖场景对象、Manager 单例或异步资源的复杂逻辑。
---@param ... any 由 `CreateInstance(...)` 透传进来的初始化参数。
function BaseCtrl:Ctor()
end

---控制器启动函数（虚函数）。
---当控制器被正式注册并准备开始工作时调用，适合在这里编写真正的业务启动逻辑。
---例如：获取 Manager、注册事件监听、打开默认界面、发起首批数据请求等。
---如果子类重写该函数，建议保证其幂等性，避免重复启动导致重复注册或重复打开界面。
function BaseCtrl:Start()
end

---控制器销毁函数（虚函数）。
---当控制器不再使用时调用，子类应在这里释放由自身持有的资源。
---常见清理内容包括：注销事件、关闭或释放面板、清空缓存引用、停止定时器或协程等。
---为了避免内存泄漏或脏状态残留，凡是 `Start()` / `Ctor()` 中申请的长期资源，都应尽量在这里回收。
function BaseCtrl:Dispose()
end

return BaseCtrl