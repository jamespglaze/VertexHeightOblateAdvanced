using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KSP.Localization;
using KSP.UI.Screens;
using ToolbarControl_NS;
using static VertexHeightOblateAdvanced.SetMaterialOverride;
using System.Reflection;
using static PropTools;

namespace VertexHeightOblateAdvanced
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    internal class VertexHeightOblateAdvancedGUI : MonoBehaviour
    {
        public static VertexHeightOblateAdvancedGUI instance;
        public static Dictionary<String, AtmosphereFromGroundCache> inputBodyCaches = new Dictionary<String, AtmosphereFromGroundCache>();

        internal const string modNAME = "VertexHeightOblateAdvanced";
        internal const string modID = "VertexHeightOblateAdvanced";

        private ToolbarControl toolbarControl;
        private bool toolbarButtonAdded = false;
        private bool GUIEnabled = false;

        private static float xpos = 100f;
        private static float ypos = 100f;
        private static float xwidth = 285f;
        private static float yheight = 60.0f;
        private Rect windowPos = new Rect(xpos, ypos, xwidth, yheight);

        public VertexHeightOblateAdvancedGUI()
        {
            if (instance == null)
            {
                LogUtils.LogInfo("Initializing GUI.");
                instance = this;
            }
            else { Destroy(this); }
        }

        public void Start()
        {
            UpdateUISizes();
            AddToolbarButton();
        }
        void OnDestroy()
        {
            instance = null;
            Destroy();
            Destroy(this);
        }

        void OnGUI()
        {
            if (GUIEnabled) { windowPos = GUILayout.Window("VHOA".GetHashCode(), windowPos, DrawWindow, "VHOA: 1.1.1"); }
        }
        void DrawWindow(int windowID)
        {
            GUILayout.BeginVertical();
            DrawElement("Test", "TestVal1");
            CelestialBody mainBody = FlightGlobals.ActiveVessel.mainBody;
            
            DrawElement("Body name", mainBody.name);
            AtmosphereFromGroundCache baseBodyCache;
            if (baseBodyCaches.TryGetValue(mainBody.name, out baseBodyCache) == true)
            {
                DrawHeader("Base values");
                foreach (FieldInfo field in typeof(AtmosphereFromGroundCache).GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    DrawElement(field.Name, field.GetValue(baseBodyCache).ToString());
                }
            }
            AtmosphereFromGroundCache overrideBodyCache;
            if (overrideBodyCaches.TryGetValue(mainBody.name, out overrideBodyCache) == true)
            {
                DrawHeader("Override values");
                foreach (FieldInfo field in typeof(AtmosphereFromGroundCache).GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    float value = (float)field.GetValue(overrideBodyCache);
                    DrawInput(field.Name, ref value);
                    field.SetValue(overrideBodyCache, value);
                }
            }
            Vector3 overrideRenderScaleCache;
            if (overrideRenderScaleCaches.TryGetValue(mainBody.name, out overrideRenderScaleCache) == true)
            {
                DrawHeader("Render scale override");
                float x = overrideRenderScaleCache.x;
                float y = overrideRenderScaleCache.y;
                float z = overrideRenderScaleCache.z;
                DrawInput("Scale X", ref x);
                DrawInput("Scale Y", ref y);
                DrawInput("Scale Z", ref z);
                overrideRenderScaleCache.x = x;
                overrideRenderScaleCache.y = y;
                overrideRenderScaleCache.z = z;
            }
            Vector3 overrideTransformScaleCache;
            if (overrideTransformScaleCaches.TryGetValue(mainBody.name, out overrideTransformScaleCache) == true)
            {
                DrawHeader("Transform scale override");
                float x = overrideTransformScaleCache.x;
                float y = overrideTransformScaleCache.y;
                float z = overrideTransformScaleCache.z;
                DrawInput("Scale X", ref x);
                DrawInput("Scale Y", ref y);
                DrawInput("Scale Z", ref z);
                overrideTransformScaleCache.x = x;
                overrideTransformScaleCache.y = y;
                overrideTransformScaleCache.z = z;

            }
            float overrideRadiusScaleCache;
            if (overrideRadiusScaleCaches.TryGetValue(mainBody.name, out overrideRadiusScaleCache) == true)
            {
                DrawHeader("Transform scale override");
                float scale = overrideRadiusScaleCache;
                DrawInput("Scale radius", ref overrideRadiusScaleCache);
                overrideRadiusScaleCache = scale;

            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        internal void UpdateUISizes()
        {
            xwidth *= Math.Min(GameSettings.UI_SCALE, 1.5f);
            yheight *= GameSettings.UI_SCALE;
            xpos *= GameSettings.UI_SCALE;
            ypos *= GameSettings.UI_SCALE;
            //titleRect = new Rect(0, 0, 10000 * (int)GameSettings.UI_SCALE, 10000 * (int)GameSettings.UI_SCALE);
            windowPos = new Rect(xpos, ypos, xwidth, yheight);
        }

        //GUILayout functions to avoid being a WET boi
        private void DrawHeader(string tag)
        {
            GUILayout.BeginHorizontal();
            GUI.skin.label.margin = new RectOffset(5, 5, 5, 5);
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUILayout.Label(tag);
            GUI.skin.label.fontStyle = FontStyle.Normal;
            GUILayout.EndHorizontal();
            GUI.skin.label.margin = new RectOffset(2, 2, 2, 2);
        }

        private void DrawElement(string tag, string value)
        {
            GUILayout.BeginHorizontal();
            GUI.skin.label.alignment = TextAnchor.MiddleLeft;
            GUILayout.Label(tag);
            GUI.skin.label.alignment = TextAnchor.MiddleRight;
            GUILayout.Label(value);
            GUILayout.EndHorizontal();
        }

        private void DrawInput(string tag, ref float value)
        {
            GUILayout.BeginHorizontal();
            GUI.skin.label.alignment = TextAnchor.MiddleLeft;
            GUILayout.Label(tag);
            GUI.skin.label.alignment = TextAnchor.MiddleRight;
            value = float.Parse(GUILayout.TextField(value.ToString()));
            GUILayout.EndHorizontal();
        }

        private void DrawCentered(string tag)
        {
            GUILayout.BeginHorizontal();
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label(tag);
            GUILayout.EndHorizontal();
        }

        private void AddToolbarButton()
        {
            ApplicationLauncher.AppScenes scenes = ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW;
            toolbarControl = gameObject.AddComponent<ToolbarControl>();
            if (!toolbarButtonAdded)
            {
                toolbarControl.AddToAllToolbars(ToolbarButtonOnTrue, ToolbarButtonOnFalse, scenes, modID, modID, "000_DuckweedUtils/VertexHeightOblateAdvanced/PluginData", "000_DuckweedUtils/VertexHeightOblateAdvanced/PluginData", Localizer.Format(modNAME));
                toolbarButtonAdded = true;
            }
        }

        private void RemoveToolbarButton()
        {
            if (toolbarButtonAdded)
            {
                toolbarControl.OnDestroy();
                Destroy(toolbarControl);
                toolbarButtonAdded = false;
            }
        }

        private void ToolbarButtonOnTrue() => GUIEnabled = true;
        private void ToolbarButtonOnFalse() => GUIEnabled = false;

        void Destroy()
        {
            RemoveToolbarButton();
            GameEvents.onGUIApplicationLauncherDestroyed.Remove(RemoveToolbarButton);
        }
    }
}
