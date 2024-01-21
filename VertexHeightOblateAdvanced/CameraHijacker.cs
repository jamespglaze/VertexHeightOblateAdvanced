using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KSP.UI.Screens;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using HarmonyLib;
using Kopernicus.Configuration;
using UnityEngine.UI;
using KSP.Localization;
using System.ComponentModel;
using static Kopernicus.Components.DrawTools;
using static KSP.UI.Screens.MessageSystem;

namespace VertexHeightOblateAdvanced
{
    internal class CustomCameraConstants
    {
        internal enum CustomModes
        {
            [Description("BASE")] BASE = 0,
            [Description("OBLATE")] OBLATE = 1,
        }
        internal static CustomModes customMode = CustomModes.BASE;
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class CustomCameraInjector : MonoBehaviour
    {
        public void Awake()
        {
            Harmony harmony = new Harmony("CameraInjector");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(FlightCamera), nameof(FlightCamera.GetCameraFoR), new Type[] { typeof(FoRModes) })]
    public static class GetCustomCameraFoR
    {
        private static bool Prefix(ref Quaternion __result, ref FoRModes mode, ref FoRModes ___FoRMode)
        {
            CelestialBody currentMainBody = FlightGlobals.currentMainBody;
            PQSMod_VertexHeightOblateAdvanced currentMainBodyOblateMod = Kopernicus.Utility.GetMod<PQSMod_VertexHeightOblateAdvanced>(currentMainBody.pqsController);
            Vessel activeVessel = FlightGlobals.ActiveVessel;
            if (currentMainBodyOblateMod != null && CustomCameraConstants.customMode != CustomCameraConstants.CustomModes.BASE && FlightCamera.fetch.mode == FlightCamera.Modes.FREE)
            {
                double v = 0.5f + (currentMainBody.GetLatitude(activeVessel.GetWorldPos3D()) / 180.0f);
                double u = 0.25f - (currentMainBody.GetLongitude(activeVessel.GetWorldPos3D()) / (2 * 180.0f));
                Vector3 normalVector = FlightGlobals.ActiveVessel.mainBody.transform.TransformVector(currentMainBodyOblateMod.CalculateNormal(u, v));
                ___FoRMode = mode;
                __result = Quaternion.LookRotation(Quaternion.AngleAxis(90f, Vector3.Cross(Vector3.up, normalVector)) * -normalVector, normalVector);
                return false;
            }
            else
            {
                CustomCameraConstants.customMode = CustomCameraConstants.CustomModes.BASE;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(FlightCamera), nameof(FlightCamera.setMode), new Type[] { typeof(FlightCamera.Modes) })]
    public static class SetCustomMode
    {
        private static bool Prefix(FlightCamera __instance, ref FlightCamera.Modes m, ref ScreenMessage ___cameraModeReadout, ref Quaternion ___frameOfReference, ref Quaternion ___lastFoR, ref float ___FoRlerp, ref float ___offsetHdg, ref float ___offsetPitch)
        {
            PQSMod_VertexHeightOblateAdvanced currentMainBodyOblateMod = Kopernicus.Utility.GetMod<PQSMod_VertexHeightOblateAdvanced>(FlightGlobals.currentMainBody.pqsController);
            if (currentMainBodyOblateMod != null && (m == FlightCamera.Modes.FREE || m == FlightCamera.Modes.ORBITAL || m == FlightCamera.Modes.CHASE) && __instance.mode == FlightCamera.Modes.FREE)
            {
                if (m != FlightCamera.Modes.FREE)
                {
                    CustomCameraConstants.customMode = (CustomCameraConstants.CustomModes)(((int)CustomCameraConstants.customMode + 1) % 2);
                }
                if (CustomCameraConstants.customMode != CustomCameraConstants.CustomModes.BASE)
                {
                    MonoBehaviour.print("Camera Mode: " + CustomCameraConstants.customMode.ToString());
                    ___cameraModeReadout.message = Localizer.Format("#autoLOC_133776", new string[1]
                    {
                        CustomCameraConstants.customMode.displayDescription()
                    });
                    ScreenMessages.PostScreenMessage(___cameraModeReadout);
                    if (__instance.mode == FlightCamera.Modes.AUTO)
                        __instance.autoMode = FlightCamera.GetAutoModeForVessel(FlightGlobals.ActiveVessel);
                    ___lastFoR = ___frameOfReference;
                    ___FoRlerp = 0.0f;
                    ___offsetHdg = 0.0f;
                    ___offsetPitch = 0.0f;
                    GameEvents.OnFlightCameraAngleChange.Fire(m);
                    return false;
                }
            }
            else
            {
                CustomCameraConstants.customMode = CustomCameraConstants.CustomModes.BASE;
            }
            return true;
        }
    }
}
