using BepInEx;
using BepInEx.Configuration;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace BluePrinceLuckHUD
{
    public class ModInstance : MonoBehaviour
    {

        private const string PreferencesCategoryName = "BluePrinceLuckHud";

        public ConfigEntry<float> _luckHudPositionX;
        public ConfigEntry<float> _luckHudPositionY;
        public ConfigEntry<float> _luckHudScale;

        private GameObject _hud;
        private GameObject _luckHudObject;
        private AssetBundle _luckHudBundle;
        private TextMeshPro _luckText;
        private GameObject _cullingReference;

        private PlayMakerFSM _luckFsm;

        private const string HudGameObjectPath = "__SYSTEM/HUD";
        private const string CullingReferenceRelativePath = "Steps/Steps Icon";
        private const string FontReferenceRelativePath = "Steps/Steps Icon/step rotator/Steps #";
        private const string LuckCalculatorPath = "__SYSTEM/Luck Calculator";
        private const string LuckHudBundlePathSuffix = "BluePrinceLuckHud/assets/luck_hud.bundle";
        private const string LuckHudPrefabName = "Luck HUD";
        private const string LuckTextName = "Luck #";

        private const float baseScale = 0.3f;
        private const float LuckHudZPosition = 27.46f;
        private ConfigFile Config = new("config.cfg", true);
        public static ModInstance Instance;
        public ModInstance(IntPtr ptr) : base(ptr)
        {
            Instance = this; //Set the modInstance for easy access.
        }
        private void Start()
        {
            InitPreferences();
            Plugin.BepinLogger.LogMessage("Initialized Blue Prince Luck Hud Mod.");
            SceneManager.sceneLoaded += (Action<Scene, LoadSceneMode>)OnSceneLoaded;
        }

        private void InitPreferences()
        {
            _luckHudPositionX = Config.Bind(PreferencesCategoryName, "LuckHudPositionX", 18.5982f, "The x position of the luck hud relative to the HUD GameObject.");
            _luckHudPositionY = Config.Bind(PreferencesCategoryName, "LuckHudPositionY", -987.43f, "The y position of the luck hud relative to the HUD GameObject.");
            _luckHudScale = Config.Bind(PreferencesCategoryName, "LuckHudScale", 1.0f, "The size of the luck hud relative to its default size.");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _hud = GameObject.Find(HudGameObjectPath);
            if (_hud == null)
            {
                return;
            }

            _cullingReference = GetCullingReference();
            LoadPrefab();
            
        }

        // Retrieves the culling reference GameObject from the HUD GameObject using the
        // specified relative path.
        private GameObject GetCullingReference()
        {
            Transform cullingReference = _hud.transform.Find(CullingReferenceRelativePath);
            if (cullingReference == null)
            {
                Plugin.BepinLogger.LogWarning($"Could not find culling reference Transform \"{HudGameObjectPath}/{CullingReferenceRelativePath}\"");
                return null;
            }
            return cullingReference.gameObject;
        }

        // Initializes the luck hud by loading the prefab from the asset bundle, instantiating
        // it, and setting it up for culling.
        private void InitLuckHud()
        {
            GameObject luckHudObject = _luckHudObject;
            if (luckHudObject == null)
            {
                Plugin.BepinLogger.LogWarning($"Could not load the luck hud prefab from the asset bundle. Please ensure the bundle is correctly placed in the mods directory at BluePrinceLuckHud\\assets\\luckhud.bundle.");
                return;
            }
            // Initially disable the luck hud so it doesn't appear in the opening cutscene.
            luckHudObject.SetActive(false);
            InitCulling(luckHudObject);

            Plugin.BepinLogger.LogMessage($"Luck Hud Object instantiated and parented to \"{HudGameObjectPath}\".");
            _luckText = InitLuckText();
            _luckFsm = FetchLuckFsm();
        }


        private TextMeshPro GetFontReference()
        {
            return _hud.transform.Find(FontReferenceRelativePath)?.GetComponent<TextMeshPro>();
        }

        private TextMeshPro InitLuckText()
        {
            TextMeshPro luckText = FetchLuckText();
            if (luckText == null)
            {
                Plugin.BepinLogger.LogWarning("Failed to fetch Luck Text.");
                return null;
            }
            InitFont(luckText);
            return luckText;
        }

        private void InitFont(TextMeshPro luckText)
        {
            TextMeshPro fontReference = GetFontReference();
            luckText.font = fontReference.font; 
        }

        private TextMeshPro FetchLuckText()
        {
            if (_luckHudObject == null)
            {
                Plugin.BepinLogger.LogWarning("Luck HUD object is null, cannot find Luck Text.");
                return null;
            }
            Transform luckTextTransform = _luckHudObject.transform.Find(LuckTextName);
            if (luckTextTransform == null)
            {
                Plugin.BepinLogger.LogWarning($"Could not find Luck Text \"{LuckTextName}\" in Luck HUD object.");
                return null;
            }
            return luckTextTransform.GetComponent<TextMeshPro>();
        }
        // Loads the luck hud prefab from the asset bundle and instantiates it at the specified
        // position and scale.
        private void LoadPrefab()
        {
            string bundlePath = Path.Combine(Paths.PluginPath, LuckHudBundlePathSuffix);
            Plugin.BepinLogger.LogMessage($"Bundle Path: {bundlePath}");
            _luckHudBundle = AssetBundle.LoadFromFile(bundlePath);
            if (_luckHudBundle == null)
            {
                Plugin.BepinLogger.LogWarning($"Failed to load asset bundle from path: {bundlePath}");
                return;
            }
            GameObject prefab = _luckHudBundle.LoadAsset(LuckHudPrefabName).TryCast<GameObject>(); //This took way to long to figure out.
            if (prefab == null)
            {
                Plugin.BepinLogger.LogWarning($"Failed to load prefab from path: {LuckHudPrefabName}");
                return;
            }
            Vector3 luckHudPosition = new(_luckHudPositionX.Value, _luckHudPositionY.Value, LuckHudZPosition);
            GameObject luckHudObject = Instantiate(prefab, luckHudPosition, Quaternion.identity, _hud.transform);
            float scale = baseScale * _luckHudScale.Value;
            luckHudObject.transform.localScale = new Vector3(scale, scale, 1);

            _luckHudBundle.Unload(false);
            _luckHudObject = luckHudObject;
            InitLuckHud();
        }
        

        // Sets up luck hud culling so it will only be rendered when steps, gems and keys are
        // rendered.
        private void InitCulling(GameObject obj)
        {
            Culler hudCuller = _hud.GetComponent<Culler>();
            if (hudCuller == null)
            {
                Plugin.BepinLogger.LogWarning($"HUD GameObject with name \"{HudGameObjectPath}\" does not have a Culler component. The luck hud will not be added to the culler.");
                return;
            }

            // This (like this whole codebase) is incredibly overengineered, but I have an
            // irrational fear of breaking forward compatibility if I use hardcoded values.
            bool referenceInEnabledList = true;
            if (_cullingReference == null)
            {
                Plugin.BepinLogger.LogWarning("The culling reference is null, so the rendering layer couldn't be determined.");
            }
            else
            {
                SetLayerForAllDescendants(obj, _cullingReference.gameObject.layer);
                Renderer referenceRenderer = _cullingReference.GetComponent<Renderer>();
                referenceInEnabledList = (referenceRenderer != null &&
                    hudCuller._childRenderersEnabled.Contains(referenceRenderer));
            }

            MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>();
            // I wasn't sure how to convert from the array to an Il2CPP enumerable, so I just
            // add the MeshRenderers one by one.
            foreach (MeshRenderer renderer in renderers)
            {
                // I don't like using these private fields, but I don't know how to add the
                // luck hud to the culler otherwise.
                hudCuller._childRenderers.Add(renderer);
                if (referenceInEnabledList)
                {
                    hudCuller._childRenderersEnabled.Add(renderer);
                }
                else
                {
                    hudCuller._childRenderersDisabled.Add(renderer);
                }
            }
        }

        private static PlayMakerFSM FetchLuckFsm()
        {
            GameObject luckObject = GameObject.Find(LuckCalculatorPath);
            if (luckObject == null)
            {
                Plugin.BepinLogger.LogWarning($"Could not find Luck Calculator GameObject at path \"{LuckCalculatorPath}\".");
                return null;
            }
            return luckObject?.GetComponent<PlayMakerFSM>();
        }

        private int GetCurrentLuck()
        {
            if (_luckFsm == null)
            {
                Plugin.BepinLogger.LogWarning("Luck Calculator FSM was not set.");
                return 0;
            }
            return _luckFsm.FsmVariables.GetFsmInt("LUCK").Value;
        }

        // Sets the layer for all descendants of the given GameObject to the specified layer.
        private static void SetLayerForAllDescendants(GameObject obj, int layer)
        {
            foreach (Transform child in obj.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = layer;
            }
        }

        public void Update()
        {
            SetHudActive();
            UpdateLuckText();
        }

        private void SetHudActive()
        {
            if (_luckHudObject == null)
            {
                return;
            }
            // I'm not sure how the HUD objects get enabled after the opening cutscene, so I
            // just enable the luck hud if the culling reference is active or if there is no
            // culling reference.
            _luckHudObject.SetActive(_cullingReference == null || _cullingReference.activeInHierarchy);
        }

        private void UpdateLuckText()
        {
            if (_luckText == null)
            {
                return;
            }
            _luckText.text = GetCurrentLuck().ToString();
        }
    }
}