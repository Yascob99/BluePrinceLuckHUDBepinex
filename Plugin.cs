using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace BluePrinceLuckHUD
{

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Plugin : BasePlugin
    {
        public const string PluginGUID = "com.ComplexSimple.BluePrinceLuckHud";
        public const string PluginName = "BluePrinceLuckHud";
        public const string PluginVersion = "0.1.0";

        private static Plugin _instance;
        public static Plugin Instance => _instance;

        public const string ModDisplayInfo = $"{PluginName} v{PluginVersion}";
        public static ManualLogSource BepinLogger;
        public static GameObject ModObject;
        public override void Load()
        {
            // Plugin startup logic
            BepinLogger = Log;
            _instance = this;
            Log.LogInfo($"Plugin {PluginGUID} is loaded!");
            //Inject custom Object for Mod Handling
            ClassInjector.RegisterTypeInIl2Cpp<ModInstance>();
            ModObject = new GameObject("LuckHUD");
            GameObject.DontDestroyOnLoad(ModObject);
            ModObject.hideFlags = HideFlags.HideAndDontSave; //The mod breaks if this is removed. Unsure if different flags could be used to make this more visible.
            ModObject.AddComponent<ModInstance>();
        }
    }

}