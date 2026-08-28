//this source code was manually added for tolua# binding, can be regenerated later
using System;
using LuaInterface;

public class ResourceUpdateBridgeWrap
{
    public static void Register(LuaState L)
    {
        L.BeginClass(typeof(ResourceUpdateBridge), typeof(UnityEngine.MonoBehaviour));
        L.RegFunction("EnsureInstance", EnsureInstance);
        L.RegFunction("IsRemoteUpdateInProgress", IsRemoteUpdateInProgress);
        L.RegFunction("StartUpdateByLabelString", StartUpdateByLabelString);
        L.RegFunction("__eq", op_Equality);
        L.RegFunction("__tostring", ToLua.op_ToString);
        L.RegVar("Instance", get_Instance, null);
        L.RegVar("IsUpdating", get_IsUpdating, null);
        L.EndClass();
    }

    [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
    static int EnsureInstance(IntPtr L)
    {
        try
        {
            ToLua.CheckArgsCount(L, 0);
            ResourceUpdateBridge o = ResourceUpdateBridge.EnsureInstance();
            ToLua.Push(L, o);
            return 1;
        }
        catch (Exception e)
        {
            return LuaDLL.toluaL_exception(L, e);
        }
    }

    [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
    static int IsRemoteUpdateInProgress(IntPtr L)
    {
        try
        {
            ToLua.CheckArgsCount(L, 1);
            ResourceUpdateBridge obj = (ResourceUpdateBridge)ToLua.CheckObject<ResourceUpdateBridge>(L, 1);
            bool o = obj.IsRemoteUpdateInProgress();
            LuaDLL.lua_pushboolean(L, o);
            return 1;
        }
        catch (Exception e)
        {
            return LuaDLL.toluaL_exception(L, e);
        }
    }

    [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
    static int StartUpdateByLabelString(IntPtr L)
    {
        try
        {
            ToLua.CheckArgsCount(L, 5);
            ResourceUpdateBridge obj = (ResourceUpdateBridge)ToLua.CheckObject<ResourceUpdateBridge>(L, 1);
            string arg0 = ToLua.CheckString(L, 2);
            System.Action<ResourceDownloadStatus> arg1 = (System.Action<ResourceDownloadStatus>)ToLua.CheckDelegate<System.Action<ResourceDownloadStatus>>(L, 3);
            System.Action arg2 = (System.Action)ToLua.CheckDelegate<System.Action>(L, 4);
            System.Action<string> arg3 = (System.Action<string>)ToLua.CheckDelegate<System.Action<string>>(L, 5);
            obj.StartUpdateByLabelString(arg0, arg1, arg2, arg3);
            return 0;
        }
        catch (Exception e)
        {
            return LuaDLL.toluaL_exception(L, e);
        }
    }

    [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
    static int op_Equality(IntPtr L)
    {
        try
        {
            ToLua.CheckArgsCount(L, 2);
            UnityEngine.Object arg0 = (UnityEngine.Object)ToLua.ToObject(L, 1);
            UnityEngine.Object arg1 = (UnityEngine.Object)ToLua.ToObject(L, 2);
            bool o = arg0 == arg1;
            LuaDLL.lua_pushboolean(L, o);
            return 1;
        }
        catch (Exception e)
        {
            return LuaDLL.toluaL_exception(L, e);
        }
    }

    [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
    static int get_Instance(IntPtr L)
    {
        try
        {
            ToLua.Push(L, ResourceUpdateBridge.Instance);
            return 1;
        }
        catch (Exception e)
        {
            return LuaDLL.toluaL_exception(L, e);
        }
    }

    [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
    static int get_IsUpdating(IntPtr L)
    {
        object o = null;
        try
        {
            o = ToLua.ToObject(L, 1);
            ResourceUpdateBridge obj = (ResourceUpdateBridge)o;
            bool ret = obj.IsUpdating;
            LuaDLL.lua_pushboolean(L, ret);
            return 1;
        }
        catch (Exception e)
        {
            return LuaDLL.toluaL_exception(L, e, o, "attempt to index IsUpdating on a nil value");
        }
    }
}
