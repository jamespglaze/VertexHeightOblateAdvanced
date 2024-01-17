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

namespace VertexHeightOblateAdvanced
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class CameraInjector : MonoBehaviour
    {
        public void Awake()
        {
            Harmony harmony = new Harmony("CameraInjector");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(FlightCamera), nameof(FlightCamera.GetCameraFoR), new Type[] { typeof(FoRModes) })]
    public static class CameraHijacker
    {
        private static bool Prefix(FlightCamera __instance, ref Quaternion __result, ref FoRModes mode)
        {
            CelestialBody currentMainBody = FlightGlobals.currentMainBody;
            Vessel activeVessel = FlightGlobals.ActiveVessel;
            if (FlightGlobals.currentMainBody.bodyName == "Torr" && FlightCamera.fetch.mode == FlightCamera.Modes.FREE)
            {
                double latitude = currentMainBody.GetLatitude(activeVessel.GetWorldPos3D()) * Math.PI / 180.0;
                double theta = (Math.PI / 2) - latitude;
                double phi = currentMainBody.GetLongitude(activeVessel.GetWorldPos3D()) * Math.PI / 180.0;
                // Set mass if geeASL and radius given
                double mass = Math.Pow(50000, 2) * 0.1 * PhysicsGlobals.GravitationalAcceleration / DuckMathUtils.G;
                double criticality = DuckMathUtils.PrecalculateConstantsPointEquipotential(mass, 50000, 2650);
                Vector3 normalVector = FlightGlobals.ActiveVessel.mainBody.transform.TransformVector(DuckMathUtils.CalculateNormalPointEquipotential(phi, theta, criticality));
                __instance.FoRMode = mode;
                __result = Quaternion.LookRotation(Quaternion.AngleAxis(90f, Vector3.Cross(Vector3.up, normalVector)) * -normalVector, normalVector);

                return false;
            }
            return true;
        }
    }
}
