## ToLua 执行机制学习笔记

### 一、从一行 Lua 代码看 ToLua 调用链

示例代码：

```lua
uiContainer.img = self.ui.root:Find("Image"):GetComponent("UnityEngine.UI.Image")
```

这行代码的作用是：**从当前面板根节点下找到名为 `Image` 的子节点，并获取它身上的 `UnityEngine.UI.Image` 组件，然后缓存到 `uiContainer.img` 中。**

---

### 1. `self.ui.root` 是什么？

在 `TestPanel.lua` 中，面板初始化时会先实例化 Prefab：

```lua
self.gameObject = self.resourceManager:InstantiatePrefab(self.prefabPath)
```

然后保存根节点：

```lua
self.transform = self.gameObject.transform
self.ui.root = self.transform
```

所以：

- `self.gameObject` 是 C# 层真实的 `UnityEngine.GameObject`。
- `self.gameObject.transform` 是真实的 `UnityEngine.Transform`。
- `self.ui.root` 是 Lua 侧持有的 ToLua 包装对象，底层引用真实的 C# `Transform`。

Lua 里看到的是对象代理，真实对象仍然在 Unity/C# 层。

---

### 2. `:Find("Image")` 怎么执行？

Lua 的冒号调用：

```lua
self.ui.root:Find("Image")
```

等价于：

```lua
self.ui.root.Find(self.ui.root, "Image")
```

也就是 Lua 会自动把 `self.ui.root` 作为第一个参数传入。

ToLua 生成的 `UnityEngine_TransformWrap.cs` 中注册了 `Find`：

```csharp
L.RegFunction("Find", Find);
```

对应 wrapper 逻辑大致是：

```csharp
static int Find(IntPtr L)
{
    ToLua.CheckArgsCount(L, 2);
    UnityEngine.Transform obj = (UnityEngine.Transform)ToLua.CheckObject<UnityEngine.Transform>(L, 1);
    string arg0 = ToLua.CheckString(L, 2);
    UnityEngine.Transform o = obj.Find(arg0);
    ToLua.Push(L, o);
    return 1;
}
```

执行链条：

```text
Lua 调用 self.ui.root:Find("Image")
    ↓
进入 TransformWrap.Find
    ↓
从 Lua 栈第 1 个参数取出 Transform 对象
    ↓
从 Lua 栈第 2 个参数取出字符串 "Image"
    ↓
调用 Unity API：Transform.Find("Image")
    ↓
找到子节点 Transform
    ↓
ToLua.Push 把 Transform 包装回 Lua
```

关键点：**`Find` 返回的是场景中真实子节点的 `Transform` 引用，不是拷贝。**

---

### 3. 为什么 `Transform` 可以调用 `GetComponent`？

链式调用中：

```lua
self.ui.root:Find("Image"):GetComponent("UnityEngine.UI.Image")
```

`Find("Image")` 返回的是 `Transform`。

在 Unity 中：

```text
Transform : Component : Object
```

所以 `Transform` 继承自 `Component`，可以调用 `GetComponent`。

ToLua 生成的 `UnityEngine_ComponentWrap.cs` 中注册了 `GetComponent`：

```csharp
L.RegFunction("GetComponent", GetComponent);
```

其中字符串重载大致是：

```csharp
else if (count == 2 && TypeChecker.CheckTypes<string>(L, 2))
{
    UnityEngine.Component obj = (UnityEngine.Component)ToLua.CheckObject<UnityEngine.Component>(L, 1);
    string arg0 = ToLua.ToString(L, 2);
    UnityEngine.Component o = obj.GetComponent(arg0);
    ToLua.Push(L, o);
    return 1;
}
```

所以 Lua：

```lua
:GetComponent("UnityEngine.UI.Image")
```

最终会调用 C#：

```csharp
obj.GetComponent("UnityEngine.UI.Image")
```

其中 `obj` 是刚才找到的 `Image` 节点的 `Transform`。

---

### 4. `uiContainer.img` 最终保存了什么？

执行完：

```lua
uiContainer.img = self.ui.root:Find("Image"):GetComponent("UnityEngine.UI.Image")
```

之后：

```lua
uiContainer.img
```

保存的是一个 Lua userdata/proxy，底层引用真实的：

```csharp
UnityEngine.UI.Image
```

可以理解为：

```text
Lua table: uiContainer
  img ───────→ ToLua userdata/proxy
                    ↓
              C# UnityEngine.UI.Image 真实组件
                    ↓
              场景里的 Image 节点
```

Lua 没有复制组件，也没有新建组件，只是保存了 C# 组件引用。

---

### 5. 为什么必须导出 `UnityEngine.UI.Image`？

ToLua 不是完全动态反射系统，它依赖提前生成的 Wrap。

如果没有导出 `UnityEngine.UI.Image`，那么 C# 层虽然能通过 `GetComponent("UnityEngine.UI.Image")` 找到真实组件，但 Lua 层不知道这个类型有哪些属性和方法。

例如没有 `UnityEngine_UI_ImageWrap.cs` 时，访问：

```lua
uiContainer.img.sprite
```

就可能报错：

```text
field or property sprite does not exist
```

导出 `Image` 后，生成的 `UnityEngine_UI_ImageWrap.cs` 会注册属性：

```csharp
L.RegVar("sprite", get_sprite, set_sprite);
```

这表示：

- Lua 读取 `image.sprite` 时调用 `get_sprite`。
- Lua 设置 `image.sprite = xxx` 时调用 `set_sprite`。

---

### 6. 真正修改场景 Image 的是哪句？

真正改变图片显示的是：

```lua
uiContainer.img.sprite = self.resourceManager:LoadSprite("Assets/Textures/HollowKnightIcon.jpg")
```

执行过程分为两步。

#### 6.1 右侧加载 Sprite

```lua
self.resourceManager:LoadSprite("Assets/Textures/HollowKnightIcon.jpg")
```

会通过 `ResourceManagerWrap.cs` 调到 C# 的 `ResourceManager.LoadSprite`，然后再由资源系统加载真实的 `UnityEngine.Sprite`。

结果是：Lua 侧拿到一个 ToLua 包装后的 `Sprite` 引用。

#### 6.2 左侧设置 Image.sprite

Lua 执行赋值：

```lua
uiContainer.img.sprite = loadedSprite
```

会触发 `UnityEngine_UI_ImageWrap.cs` 中注册的 `set_sprite`：

```csharp
static int set_sprite(IntPtr L)
{
    object o = ToLua.ToObject(L, 1);
    UnityEngine.UI.Image obj = (UnityEngine.UI.Image)o;
    UnityEngine.Sprite arg0 = (UnityEngine.Sprite)ToLua.CheckObject(L, 2, typeof(UnityEngine.Sprite));
    obj.sprite = arg0;
    return 0;
}
```

最关键的是：

```csharp
obj.sprite = arg0;
```

这里的：

- `obj` 是场景中真实的 `UnityEngine.UI.Image` 组件。
- `arg0` 是真实的 `UnityEngine.Sprite` 对象。

所以它等价于在 C# 中写：

```csharp
imageComponent.sprite = loadedSprite;
```

---

### 7. 单行 UI 查找与属性修改总链路

```text
Lua: self.ui.root:Find("Image")
    ↓
TransformWrap.Find
    ↓
C#: Transform.Find("Image")
    ↓
返回子节点 Transform
    ↓
Lua: :GetComponent("UnityEngine.UI.Image")
    ↓
ComponentWrap.GetComponent
    ↓
C#: Component.GetComponent("UnityEngine.UI.Image")
    ↓
返回真实 Image 组件
    ↓
ToLua.Push 包装成 Lua userdata
    ↓
Lua: uiContainer.img = Image userdata
    ↓
Lua: uiContainer.img.sprite = loadedSprite
    ↓
ImageWrap.set_sprite
    ↓
C#: image.sprite = sprite
    ↓
场景中的 Image 显示发生变化
```

---

## 二、LuaState 与 Lua 虚拟机如何执行 Lua 文件

### 1. 谁管理 Lua 虚拟机？

在当前项目中，**Lua 虚拟机由 `LuaClient.cs` 管理。**

核心关系：

```text
Unity GameObject
  └── LuaClient.cs
        ├── 创建 LuaState
        ├── 初始化 Lua 搜索路径
        ├── 注册 C# Wrap 类型
        ├── 执行 Main.lua
        ├── 挂载 LuaLooper
        └── 销毁 LuaState
```

`LuaClient` 是一个 `MonoBehaviour`，所以它跟 Unity 生命周期绑定。

启动入口是：

```csharp
protected void Awake()
{
    Instance = this;
    Init();
    SceneManager.sceneLoaded += OnSceneLoaded;
}
```

当挂着 `LuaClient` 的 GameObject 被加载时，Unity 调用 `Awake()`，Lua 系统开始初始化。

---

### 2. Lua 虚拟机什么时候创建？

`LuaClient.Init()` 中创建虚拟机：

```csharp
protected void Init()
{
    InitLoader();
    luaState = new LuaState();
    OpenLibs();
    luaState.LuaSetTop(0);
    Bind();
    LoadLuaFiles();
}
```

真正创建 Lua VM 的是：

```csharp
luaState = new LuaState();
```

---

### 3. `LuaState`、`LuaStatePtr`、`lua_State*` 的关系

`LuaState.cs` 中：

```csharp
public class LuaState : LuaStatePtr, IDisposable
```

`LuaStatePtr.cs` 中持有底层指针：

```csharp
protected IntPtr L;
```

这个 `IntPtr L` 就是 C# 对底层 Lua C API 中 `lua_State*` 的包装。

关系如下：

```text
LuaState
  └── LuaStatePtr
        └── IntPtr L
              └── lua_State*，真正的 Lua 虚拟机
```

---

### 4. Lua VM 底层创建链条

`LuaState` 构造函数中：

```csharp
L = LuaNewState();
```

`LuaStatePtr.LuaNewState()` 中：

```csharp
public IntPtr LuaNewState()
{
    return LuaDLL.luaL_newstate();
}
```

所以底层调用的是 Lua C API：

```c
luaL_newstate()
```

完整创建链条：

```text
Unity 调用 LuaClient.Awake()
    ↓
LuaClient.Init()
    ↓
new LuaState()
    ↓
LuaState 构造函数
    ↓
LuaStatePtr.LuaNewState()
    ↓
LuaDLL.luaL_newstate()
    ↓
底层 C/LuaJIT 创建 lua_State*
    ↓
C# 得到 IntPtr L
```

---

### 5. 创建 Lua VM 后初始化了什么？

`LuaState` 构造函数大致会做这些事：

```text
InitTypeTraits()
    ↓
InitStackTraits()
    ↓
L = LuaNewState()
    ↓
LuaException.Init(L)
    ↓
OpenToLuaLibs()
    ↓
ToLua.OpenLibs(L)
    ↓
OpenBaseLibs()
    ↓
LuaSetTop(0)
    ↓
InitLuaPath()
```

主要职责：

- 初始化 C# 类型与 Lua 栈的转换规则。
- 打开 ToLua 底层库。
- 注册基础 C# 类型，例如 `System.Object`、`System.String`、`UnityEngine.Object`。
- 初始化 Lua 文件搜索路径。
- 准备后续 `DoFile`、`require`、C# Wrap 调用等能力。

---

### 6. C# 类型什么时候注册给 Lua？

在 `LuaClient.Init()` 中执行：

```csharp
Bind();
```

对应逻辑：

```csharp
protected virtual void Bind()
{
    LuaBinder.Bind(luaState);
    DelegateFactory.Init();
    LuaCoroutine.Register(luaState, this);
}
```

这里有三件关键事情。

#### 6.1 `LuaBinder.Bind(luaState)`

注册所有生成的 Wrap 类型。

例如：

```text
UnityEngine_TransformWrap
UnityEngine_GameObjectWrap
UnityEngine_UI_ImageWrap
UnityEngine_UI_ButtonWrap
ResourceManagerWrap
```

这些 Wrap 会把 C# 类型、方法、属性注册进 Lua VM。

#### 6.2 `DelegateFactory.Init()`

负责 Lua function 与 C# delegate 的转换。

例如按钮事件：

```lua
button.onClick:AddListener(function()
    print("按钮点击")
end)
```

这里的 Lua 匿名函数要转成 C# 的 `UnityAction`，就依赖 `DelegateFactory`。

#### 6.3 `LuaCoroutine.Register(luaState, this)`

负责把 Lua coroutine 和 Unity 的 MonoBehaviour 驱动机制接起来。

例如：

```lua
coroutine.start(function()
    coroutine.step(1)
    AppFacade.Start()
end)
```

---

### 7. `Main.lua` 是怎么执行的？

`LuaClient.OnLoadFinished()` 中：

```csharp
protected virtual void OnLoadFinished()
{
    luaState.Start();
    StartLooper();
    StartMain();
}
```

然后 `StartMain()`：

```csharp
protected virtual void StartMain()
{
    luaState.DoFile("Main.lua");
    levelLoaded = luaState.GetFunction("OnLevelWasLoaded");
    CallMain();
}
```

注意：

- `DoFile("Main.lua")` 会执行 `Main.lua` 的顶层代码。
- `GetFunction("OnLevelWasLoaded")` 会缓存 Lua 中的场景切换回调。
- `CallMain()` 才是真正调用 Lua 全局函数 `Main()`。

---

### 8. `DoFile("Main.lua")` 的底层流程

`LuaState.DoFile()`：

```csharp
public void DoFile(string fileName)
{
    byte[] buffer = LoadFileBuffer(fileName);
    fileName = LuaChunkName(fileName);
    LuaLoadBuffer(buffer, fileName);
}
```

它不是直接让 Lua 自己读文件，而是 C# 先读取 Lua 文件内容。

流程：

```text
DoFile("Main.lua")
    ↓
LoadFileBuffer("Main.lua")
    ↓
LuaFileUtils.Instance.ReadFile("Main.lua")
    ↓
LuaFileUtils.FindFile("Main.lua")
    ↓
File.ReadAllBytes(path)
    ↓
得到 byte[] buffer
    ↓
LuaLoadBuffer(buffer, "@Main.lua")
```

---

### 9. Lua 文件如何被找到？

`LuaFileUtils` 负责查找和读取 Lua 文件。

`LuaState.InitLuaPath()` 会添加搜索路径，例如：

```text
LuaConst.toluaDir
LuaConst.luaDir
LuaConst.luaResDir
```

`LuaFileUtils.FindFile()` 会遍历搜索路径：

```csharp
for (int i = 0; i < searchPaths.Count; i++)
{
    fullPath = searchPaths[i].Replace("?", fileName);

    if (File.Exists(fullPath))
    {
        return fullPath;
    }
}
```

因此：

```csharp
luaState.DoFile("Main.lua")
```

会在搜索路径中找到类似：

```text
Assets/Lua/Main.lua
```

---

### 10. Lua VM 如何真正执行源码？

`LuaLoadBuffer` 底层会调用 Lua C API：

```csharp
int status = LuaDLL.luaL_loadbuffer(L, buffer, buffer.Length, chunkName);

if (status != 0)
{
    return false;
}

return LuaDLL.lua_pcall(L, 0, LuaDLL.LUA_MULTRET, 0) == 0;
```

这里分两步：

```text
luaL_loadbuffer
    ↓
把 Lua 源码编译成 chunk，并压入 Lua 栈

lua_pcall
    ↓
执行这个 chunk
```

所以执行 Lua 文件的本质是：

```text
C# 读取 Lua 文件为 byte[]
    ↓
调用 luaL_loadbuffer 编译
    ↓
调用 lua_pcall 执行
```

---

### 11. `Main.lua` 执行时发生什么？

`Main.lua` 中有：

```lua
require "Logic.AppFacade"

function Main()
    print("logic start")

    coroutine.start(function()
        coroutine.step(1)
        AppFacade.Start()
    end)
end
```

`DoFile("Main.lua")` 执行时，会先执行顶层代码：

```lua
require "Logic.AppFacade"
```

然后定义全局函数：

```lua
Main
OnLevelWasLoaded
OnApplicationQuit
```

但是，定义 `Main()` 不等于调用 `Main()`。

真正调用 `Main()` 的是 C#：

```csharp
LuaFunction main = luaState.GetFunction("Main");
main.Call();
```

---

### 12. `require` 和 `DoFile` 的区别

#### `DoFile`

```lua
DoFile("Main.lua")
```

特点：

- 直接执行指定文件。
- 通常不会按模块名缓存。
- 更像“执行一个脚本文件”。

#### `require`

```lua
require "Logic.AppFacade"
```

特点：

- 按模块名查找文件。
- `Logic.AppFacade` 会转成类似 `Logic/AppFacade.lua`。
- 执行后会缓存到 `package.loaded`。
- 下次再 `require` 同一个模块，一般不会重复执行。

---

### 13. Lua 虚拟机如何持续运行？

Lua VM 不会自己每帧运行。

本质上：

```text
C# 调一次 Lua，Lua 就执行一次。
C# 不调 Lua，Lua 就不会主动执行。
```

因此 ToLua 需要 Unity 侧的帧驱动器，也就是 `LuaLooper.cs`。

`LuaClient.StartLooper()` 中：

```csharp
protected void StartLooper()
{
    loop = gameObject.AddComponent<LuaLooper>();
    loop.luaState = luaState;
}
```

这会给当前 GameObject 添加一个 `LuaLooper` 组件。

---

### 14. `LuaLooper` 每帧做什么？

`LuaLooper.Update()`：

```csharp
void Update()
{
    if (luaState.LuaUpdate(Time.deltaTime, Time.unscaledDeltaTime) != 0)
    {
        ThrowException();
    }

    luaState.LuaPop(1);
    luaState.Collect();
}
```

`LuaLooper.LateUpdate()`：

```csharp
void LateUpdate()
{
    if (luaState.LuaLateUpdate() != 0)
    {
        ThrowException();
    }

    luaState.StepCollect();
    luaState.LuaPop(1);
}
```

`LuaLooper.FixedUpdate()`：

```csharp
void FixedUpdate()
{
    if (luaState.LuaFixedUpdate(Time.fixedDeltaTime) != 0)
    {
        ThrowException();
    }

    luaState.LuaPop(1);
}
```

底层对应：

```csharp
LuaDLL.tolua_update(L, delta, unscaled);
LuaDLL.tolua_lateupdate(L);
LuaDLL.tolua_fixedupdate(L, fixedTime);
```

所以：

```text
Unity Update
    ↓
LuaLooper.Update
    ↓
tolua_update
    ↓
Lua VM 派发 UpdateBeat / 协程

Unity LateUpdate
    ↓
LuaLooper.LateUpdate
    ↓
tolua_lateupdate
    ↓
Lua VM 派发 LateUpdateBeat / 协程

Unity FixedUpdate
    ↓
LuaLooper.FixedUpdate
    ↓
tolua_fixedupdate
    ↓
Lua VM 派发 FixedUpdateBeat
```

---

### 15. 当前项目从 Unity 启动到打开 `TestPanel` 的完整链条

```text
Unity 加载场景
    ↓
LuaClient.Awake()
    ↓
LuaClient.Init()
    ↓
new LuaState()
    ↓
LuaDLL.luaL_newstate()
    ↓
创建 Lua VM
    ↓
OpenToLuaLibs()
    ↓
OpenBaseLibs()
    ↓
InitLuaPath()
    ↓
LuaBinder.Bind(luaState)
    ↓
DelegateFactory.Init()
    ↓
LuaCoroutine.Register(luaState, this)
    ↓
LuaClient.OnLoadFinished()
    ↓
luaState.Start()
    ↓
DoFile("tolua.lua")
    ↓
StartLooper()
    ↓
给当前 GameObject 添加 LuaLooper
    ↓
StartMain()
    ↓
luaState.DoFile("Main.lua")
    ↓
执行 require "Logic.AppFacade"
    ↓
定义 Main / OnLevelWasLoaded / OnApplicationQuit
    ↓
luaState.GetFunction("Main")
    ↓
main.Call()
    ↓
进入 Lua 的 Main()
    ↓
coroutine.start(...)
    ↓
下一帧 LuaLooper 驱动协程恢复
    ↓
AppFacade.Start()
    ↓
CtrlManager.StartUp()
    ↓
UIRootCtrl.Start()
    ↓
UIPanelManager.Open("TestPanel")
    ↓
BasePanel.Show()
    ↓
TestPanel.InitUIAndMetaData()
    ↓
执行 self.ui.root:Find("Image"):GetComponent(...)
```

---

### 16. 几个核心类的职责

#### `LuaClient`

职责：**Unity 生命周期管理者。**

负责：

- 创建 `LuaState`。
- 初始化 Lua 环境。
- 注册 C# Wrap。
- 执行 `Main.lua`。
- 调用 Lua 的 `Main()`。
- 挂载 `LuaLooper`。
- 销毁 Lua 虚拟机。

#### `LuaState`

职责：**Lua 虚拟机高级封装。**

负责：

- 初始化 ToLua 环境。
- 管理 Lua 文件执行。
- 管理 Lua 函数、Lua 表、Lua 引用。
- 管理 C# 对象与 Lua userdata 的映射。
- 管理 GC。
- 管理委托和类型注册。

#### `LuaStatePtr`

职责：**底层 lua_State 指针封装。**

负责直接调用 Lua C API，例如：

```csharp
luaL_newstate
luaL_loadbuffer
lua_pcall
tolua_update
tolua_lateupdate
tolua_fixedupdate
```

#### `LuaLooper`

职责：**Lua 每帧驱动器。**

负责在 Unity 的：

```text
Update
LateUpdate
FixedUpdate
```

中驱动 Lua VM。

#### `LuaBinder`

职责：**注册 C# Wrap 类型。**

负责把生成的 `XXXWrap.cs` 注册到 Lua VM，让 Lua 能访问 C# 类型、方法和属性。

#### `LuaFileUtils`

职责：**Lua 文件查找与读取。**

负责根据搜索路径找到 `.lua` 文件，并读取成 `byte[]` 交给 Lua VM 执行。

#### `LuaDLL`

职责：**C# 到 Lua C API 的桥。**

负责通过 P/Invoke 调用底层 Lua/LuaJIT 函数。

---

### 17. 最核心理解

ToLua 架构可以总结为：

```text
Unity 是宿主程序
Lua VM 是嵌入式脚本运行时
LuaClient 负责创建和管理 VM
LuaState 包装 VM 的高级能力
LuaStatePtr 持有底层 lua_State* 指针
LuaLooper 每帧驱动 VM
LuaBinder 把 C# 类型暴露给 Lua
XXXWrap 把 Lua 调用翻译成 C# 调用
```

一句话总结：

**Lua 文件不会自己运行，Lua 虚拟机也不会自己启动。当前项目中，是 Unity 的 `LuaClient.Awake()` 创建 `lua_State*`，初始化 ToLua，注册 C# Wrap，执行 `Main.lua` 并调用 `Main()`；之后由 `LuaLooper` 在 Unity 每帧生命周期中持续驱动 Lua VM、协程和事件。**
