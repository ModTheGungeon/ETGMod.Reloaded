-- this file is loaded by ETGMod
-- and it's used to setup a base environment for mods
-- editing this file has a high chance of breaking mods

-- There's a MOD global available in here
-- with the current mod's ModInfo

local _MOD = MOD -- make it an upvalue
local env = {
  Triggers = {},
  Mod = MOD,
  Logger = MOD.Logger,
  Assemblies = {}
}

local gungeon = interop.assembly "Assembly-CSharp"
local unity = interop.assembly "UnityEngine"

env.Assemblies.Gungeon = gungeon;
env.Assemblies.UnityEngine = unity;
env.Assemblies.System = interop.assembly "System";
env.Assemblies.Mscorlib = interop.assembly "mscorlib";

local ns_etgmod = interop.namespace(gungeon, 'ETGMod')
local function namespace(ass, name, tab)
  return setmetatable(tab, {
    __index = interop.namespace(ass, name)
  })
end

local _GAME = namespace(gungeon, "-", {
  ETGMod = namespace(gungeon, "ETGMod", {
    GUI = interop.namespace(gungeon, 'ETGMod.GUI'),
    Lua = interop.namespace(gungeon, 'ETGMod.Lua'),
    Console = namespace(gungeon, "ETGMod.Console", {
      Parser = interop.namespace(gungeon, 'ETGMod.Console')
    }),
    TexMod = interop.namespace(gungeon, 'ETGMod.TexMod')
  })
})
env._GAME = _GAME

function env.Hook(method, func)
  _MOD.Hooks:Add(method, func)
end

require("sandbox")(env)
require("api")(env)

return env