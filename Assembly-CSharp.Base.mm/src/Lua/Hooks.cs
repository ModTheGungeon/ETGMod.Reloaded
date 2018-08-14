using System;
using System.Reflection;
using System.Collections.Generic;
using MicroLua;

namespace ETGMod.Lua {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ForbidLuaHookingAttribute : Attribute {}

    public class HookManager : IDisposable {
        public Dictionary<long, int> Hooks = new Dictionary<long, int>();
        public Dictionary<long, System.Type> HookReturns = new Dictionary<long, System.Type>();

        public static List<string> ForbiddenNamespaces = new List<string> {
            "ETGMod",
            "System",
            "TexMod"
        };

        private static Logger _Logger = new Logger("HookManager");

        public void Dispose() {
            foreach (var kv in Hooks) {
                ETGMod.ModLoader.LuaState.DeleteLuaReference(kv.Value);
            }
        }

        private MethodInfo _TryFindMethod(Type type, string name, Type[] argtypes, bool instance, bool @public) {
            BindingFlags binding_flags = 0;

            if (instance) binding_flags |= BindingFlags.Instance;
            else binding_flags |= BindingFlags.Static;

            if (@public) binding_flags |= BindingFlags.Public;
            else binding_flags |= BindingFlags.NonPublic;

            // not sure if this is needed
            if (argtypes == null) return type.GetMethod(name, binding_flags);
            else return type.GetMethod(name, binding_flags, null, argtypes, null);

        }


        private void _CheckForbidden(MethodInfo method) {
            var @namespace = method.DeclaringType.Namespace;
            for (int i = 0; i < ForbiddenNamespaces.Count; i++) {
                var forbidden_namespace = ForbiddenNamespaces[i];
                if (@namespace == forbidden_namespace) {
                    throw new LuaException($"Tried to hook a method in a type in namespace '{@namespace}', but hooking methods in this namespace is forbidden");
                }
                var nsbegin = ForbiddenNamespaces[i] + '.';
                if (@namespace.StartsWithInvariant(nsbegin)) {
                    throw new LuaException($"Tried to hook a method in a type in namespace '{@namespace}', but hooking methods in sub-namespaces of '{forbidden_namespace}' is forbidden.");
                }
            }

            var attrs = method.GetCustomAttributes(typeof(ForbidLuaHookingAttribute), true);
            if (attrs.Length > 0 && attrs[0] is ForbidLuaHookingAttribute) {
                throw new LuaException($"Hooking method '{method.Name}' in type '{method.DeclaringType.FullName}' is explicitly forbidden");
            }

            var impl = method.GetMethodImplementationFlags();

            if ((impl & MethodImplAttributes.InternalCall) != 0) {
                throw new LuaException($"Hooking method '{method.Name}' in type '{method.DeclaringType.FullName}' is forbidden by default as it is an extern method");
            }

            // all good if it gets here (hopefully!)
        }

        public void Add(int details_table, int fn) {
            var lua = ETGMod.ModLoader.LuaState;
            lua.EnterArea();

            Type criteria_type = null;
            string criteria_methodname = null;
            Type[] criteria_argtypes = null;
            bool criteria_instance = true;
            bool criteria_public = true;
            bool hook_returns = false;

            lua.PushLuaReference(details_table);

            lua.GetField("type");
            if (lua.Type() == LuaType.Nil) {
                lua.LeaveAreaCleanup();
                throw new LuaException($"type: Expected Type, got null");
            } else if (!lua.IsCLRObject()) {
                lua.LeaveAreaCleanup();
                throw new LuaException($"type: Expected CLR Type object, got non-MicroLua userdata");
            }
            var obj = lua.ToCLR();
            if (!(obj is Type)) {
                lua.Pop();
                throw new LuaException($"type: Expected CLR Type object, got CLR object of type {obj.GetType()}");
            }
            criteria_type = obj as Type;
            lua.Pop();

            string method = null;

            lua.GetField("method");
            if (lua.Type() == LuaType.String) criteria_methodname = lua.ToString();
            lua.Pop();

            lua.GetField("instance");
            if (lua.Type() == LuaType.Boolean) criteria_instance = lua.ToBool();
            lua.Pop();

            lua.GetField("public");
            if (lua.Type() == LuaType.Boolean) criteria_public = lua.ToBool();
            lua.Pop();

            lua.GetField("returns");
            if (lua.Type() == LuaType.Boolean) hook_returns = lua.ToBool();
            lua.Pop();

            lua.GetField("args");
            if (lua.Type() == LuaType.Table) {
                var count = 0;
                while (true) {
                    lua.PushInt(count + 1);
                    lua.GetField();
                    if (lua.Type() == LuaType.Nil) {
                        lua.Pop();
                        break;
                    }
                    if (!lua.IsCLRObject()) {
                        lua.LeaveAreaCleanup();
                        throw new LuaException($"args: Expected entry at index {count + 1} to be a CLR Type object, got non-CLR userdata");
                    }
                    var argobj = lua.ToCLR();
                    if (!(argobj is Type)) {
                        lua.LeaveAreaCleanup();
                        throw new LuaException($"args: Expected entry at index {count + 1} to be a CLR Type object, got CLR object of type {argobj.GetType()}");
                    }
                    lua.Pop();
                    count += 1;
                }

                var argtypes = new Type[count];

                for (int i = 1; i <= count; i++) {
                    lua.PushInt(i);
                    lua.GetField();
                    argtypes[i - 1] = lua.ToCLR<Type>();
                    lua.Pop();
                }

                criteria_argtypes = argtypes;
            }
            lua.Pop();

            lua.Pop();

            var method_info = _TryFindMethod(
                criteria_type,
                criteria_methodname,
                criteria_argtypes,
                criteria_instance,
                criteria_public
            );

            if (method_info == null) {
                throw new LuaException($"Method '{criteria_methodname}' in '{criteria_type.FullName}' not found.");
            }
            _CheckForbidden(method_info);
            
            RuntimeHooks.InstallDispatchHandler(method_info);

            var token = RuntimeHooks.MethodToken(method_info);
            Hooks[token] = fn;

            if (hook_returns) {
                HookReturns[token] = method_info.ReturnType;
            }

            _Logger.Debug($"Added Lua hook for method '{criteria_methodname}' ({token})");

            lua.LeaveArea();
        }

        internal object TryRun(LuaState lua, long token, object target, object[] args, out bool returned) {
            _Logger.Debug($"Trying to run method hook (token {token})");
            lua.EnterArea();
            returned = false;

            object return_value = null;

            // TODO TODO TODO
            // CONTINUE THIS

            int fun_ref;
            if (Hooks.TryGetValue(token, out fun_ref)) {
                _Logger.Debug($"Hook found");
                // target == null --> static

                var objs_offs = 1;
                if (target == null) objs_offs = 0;

                lua.BeginProtCall();
                lua.PushLuaReference(fun_ref);
                var lua_args_len = args.Length;
                if (target != null) {
                    lua.PushCLR(target);
                    lua_args_len += 1;
                }
                for (int i = 0; i < args.Length; i++) {
                    lua.PushCLR(args[i]);
                }
                var top = lua.StackTop;
                lua.ExecProtCall(lua_args_len);
                var results_len = lua.StackTop - top;

                Type return_type;
                if (HookReturns.TryGetValue(token, out return_type)) {
                    if (results_len > 0) {
                        returned = true;
                        return_value = lua.ToCLR();

                        lua.Pop(results_len);
                    }
                }
            } else _Logger.Debug($"Hook not found");

            lua.LeaveArea();
            return return_value;
        }
    }
}
