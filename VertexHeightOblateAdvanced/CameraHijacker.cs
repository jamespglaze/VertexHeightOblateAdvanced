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
using System.Reflection;

namespace VertexHeightOblateAdvanced
{
    internal class CustomCameraConstants
    {
        internal enum CustomModes
        {
            [Description("FREE")] FREE = 0,
            [Description("SURFACE NORMAL")] SURFNORMAL = 1,
            [Description("GRAVITY NORMAL")] GRAVNORMAL = 2,
        }
        internal static CustomModes customMode = CustomModes.FREE;
        internal static bool baseHasOrbitDriftCompensation = GameSettings.ORBIT_DRIFT_COMPENSATION;
    }

    [KSPAddon(KSPAddon.Startup.Flight, true)]
    public class CustomCameraInjector : MonoBehaviour
    {
        public void Awake()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Harmony harmony = new Harmony("CameraInjector");
            harmony.PatchAll(assembly);
        }
    }

    [HarmonyPatch(typeof(FlightCamera), nameof(FlightCamera.GetCameraFoR), new Type[] { typeof(FoRModes) })]
    public static class GetCustomCameraFoR
    {
        private static bool Prefix(ref Quaternion __result, ref FoRModes mode, ref FoRModes ___FoRMode)
        {
            Vessel activeVessel = FlightGlobals.ActiveVessel;
            CelestialBody currentMainBody = FlightGlobals.currentMainBody;
            PQSMod_VertexHeightOblateAdvanced currentMainBodyOblateMod = Kopernicus.Utility.GetMod<PQSMod_VertexHeightOblateAdvanced>(currentMainBody.pqsController);
            if (currentMainBodyOblateMod != null && CustomCameraConstants.customMode != CustomCameraConstants.CustomModes.FREE && FlightCamera.fetch.mode == FlightCamera.Modes.FREE)
            {
                Vector3 normalVector;
                Vector3 normalVectorLerped;
                Vector3 normalVectorSpherical = (Vector3)FlightGlobals.getUpAxis(currentMainBody, (Vector3d)activeVessel.ReferenceTransform.position);

                double maximumRadius = currentMainBodyOblateMod.GetMaxDeformity(currentMainBody.Radius);
                double vesselRadius = activeVessel.altitude + currentMainBody.Radius;
                double endLerpRadius = Math.Min(DuckMathUtils.GetSynchronousAltitude(currentMainBody.Radius, currentMainBody.angularV, currentMainBody.GeeASL * PhysicsGlobals.GravitationalAcceleration), maximumRadius + (currentMainBody.Radius / 10.0f));
                double frameLerp = endLerpRadius > maximumRadius ? (vesselRadius - maximumRadius) / (endLerpRadius - maximumRadius) : 1;
                if (frameLerp >= 1)
                {
                    return true;
                }

                switch (CustomCameraConstants.customMode)
                {
                    case CustomCameraConstants.CustomModes.SURFNORMAL:
                        double v = 0.5f + (currentMainBody.GetLatitude(activeVessel.GetWorldPos3D()) / 180.0f);
                        double u = 0.25f - (currentMainBody.GetLongitude(activeVessel.GetWorldPos3D()) / (2 * 180.0f));
                        normalVector = FlightGlobals.ActiveVessel.mainBody.transform.TransformVector(currentMainBodyOblateMod.GetNormal(u, v));
                        break;
                    case CustomCameraConstants.CustomModes.GRAVNORMAL:
                        normalVector = -(FlightGlobals.getGeeForceAtPosition(activeVessel.GetWorldPos3D(), currentMainBody) + FlightGlobals.getCentrifugalAcc(activeVessel.GetWorldPos3D(), currentMainBody));
                        break;
                    default:
                        normalVector = normalVectorSpherical;
                        break;
                }
                normalVectorLerped = vesselRadius < maximumRadius ? normalVector : Vector3.Lerp(normalVector, normalVectorSpherical, (float)frameLerp);

                ___FoRMode = mode;
                __result = Quaternion.LookRotation(Quaternion.AngleAxis(90f, Vector3.Cross(Vector3.up, normalVectorLerped)) * -normalVectorLerped, normalVectorLerped);

                return false;
            }
            else
            {
                CustomCameraConstants.customMode = CustomCameraConstants.CustomModes.FREE;
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
                    CustomCameraConstants.customMode = (CustomCameraConstants.CustomModes)(((int)CustomCameraConstants.customMode + 1) % 3);
                }
                if (CustomCameraConstants.customMode != CustomCameraConstants.CustomModes.FREE)
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
                CustomCameraConstants.customMode = CustomCameraConstants.CustomModes.FREE;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(FlightGlobals), nameof(FlightGlobals.getGeeForceAtPosition), new Type[] { typeof(Vector3d), typeof(CelestialBody) })]
    public static class GetGeeForceAtPositionOverride
    {
        private static bool Prefix(ref Vector3d __result, ref Vector3d pos, ref CelestialBody mainBody)
        {
            PQSMod_VertexHeightOblateAdvanced mainBodyOblateMod = Kopernicus.Utility.GetMod<PQSMod_VertexHeightOblateAdvanced>(mainBody.pqsController);
            if (mainBodyOblateMod != null && mainBodyOblateMod.oblateMode == PQSMod_VertexHeightOblateAdvanced.OblateModes.ContactBinary)
            {
                Vector3d bodyRelativePos = pos - mainBody.position;
                Vector3d pointGeeForce = bodyRelativePos * -mainBody.gMagnitudeAtCenter / (bodyRelativePos.sqrMagnitude * Math.Sqrt(bodyRelativePos.sqrMagnitude));
                Vector3d primaryPos = mainBody.bodyTransform.transform.TransformVector(new Vector3d(0, 0, mainBody.Radius * mainBodyOblateMod.primaryRadius));
                Vector3d secondaryPos = mainBody.bodyTransform.transform.TransformVector(new Vector3d(0, 0, -mainBody.Radius * mainBodyOblateMod.secondaryRadius));
                double maximumRadius = mainBodyOblateMod.GetMaxDeformity(mainBody.Radius);
                double endLerpRadius = maximumRadius + (mainBody.Radius / 10.0f);
                double frameLerp = endLerpRadius > maximumRadius ? (bodyRelativePos.magnitude - maximumRadius) / (endLerpRadius - maximumRadius) : 1;
                if (frameLerp >= 1)
                {
                    GameSettings.ORBIT_DRIFT_COMPENSATION = CustomCameraConstants.baseHasOrbitDriftCompensation;
                    return true;
                }
                GameSettings.ORBIT_DRIFT_COMPENSATION = false;
                double primaryMassRatio = Math.Pow(mainBodyOblateMod.primaryRadius, 3) / (Math.Pow(mainBodyOblateMod.primaryRadius, 3) + Math.Pow(mainBodyOblateMod.secondaryRadius, 3));
                double secondaryMassRatio = 1.0f - primaryMassRatio;
                Vector3d primaryDistance = primaryPos - bodyRelativePos;
                Vector3d secondaryDistance = secondaryPos - bodyRelativePos;
                double primaryGeeForce = (DuckMathUtils.G * mainBody.Mass * primaryMassRatio) / (primaryDistance.sqrMagnitude * PhysicsGlobals.GravitationalAcceleration);
                double secondaryGeeForce = (DuckMathUtils.G * mainBody.Mass * secondaryMassRatio) / (secondaryDistance.sqrMagnitude * PhysicsGlobals.GravitationalAcceleration);
                Vector3 resultantGeeForce = (primaryDistance.normalized * (float)primaryGeeForce) + (secondaryDistance.normalized * (float)secondaryGeeForce);

                __result = Vector3d.Lerp(resultantGeeForce, pointGeeForce, frameLerp);
                return false;
            }
            GameSettings.ORBIT_DRIFT_COMPENSATION = CustomCameraConstants.baseHasOrbitDriftCompensation;
            return true;
        }
    }

    [HarmonyPatch(typeof(FlightGlobals), nameof(FlightGlobals.getAltitudeAtPos), new Type[] { typeof(Vector3d), typeof(CelestialBody) })]
    public static class GetAltitudeAtPosOverride
    {
        private static bool Prefix(ref double __result, ref Vector3d position, ref CelestialBody body)
        {
            PQSMod_VertexHeightOblateAdvanced currentBodyOblateMod = Kopernicus.Utility.GetMod<PQSMod_VertexHeightOblateAdvanced>(body.pqsController);
            if (currentBodyOblateMod != null)
            {
                double v = 0.5f + (body.GetLatitude(position) / 180.0f);
                double u = 0.25f - (body.GetLongitude(position) / (2 * 180.0f));
                __result = Vector3d.Distance(position, body.position) - body.Radius * currentBodyOblateMod.GetDeformity(1, u, v);
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(AtmosphereFromGround), nameof(AtmosphereFromGround.SetMaterial), new Type[] { typeof(bool) })]
    public static class SetMaterialOverride
    {
        static float baseOuterRadius = 0.0f;
        static float baseOuterRadius2 = 0.0f;
        static float baseInnerRadius = 0.0f;
        static float baseInnerRadius2 = 0.0f;
        static float baseScale = 0.0f;
        static float baseScaleOverScaleDepth = 0.0f;
        private static bool Prefix(AtmosphereFromGround __instance, ref bool initialSet)
        {
            CelestialBody planet = __instance.planet;
            PQSMod_VertexHeightOblateAdvanced currentBodyOblateMod = Kopernicus.Utility.GetMod<PQSMod_VertexHeightOblateAdvanced>(planet.pqsController);
            if (currentBodyOblateMod != null)
            {
                Debug.Log("In Prefix for SetMaterial");
                Debug.Log("planet name is: " + planet.name);
                double v = 0.5f + (planet.GetLatitude(__instance.mainCamera.position) / 180.0f);
                double u = 0.25f - (planet.GetLongitude(__instance.mainCamera.position) / (2 * 180.0f));
                double offset = planet.Radius * (currentBodyOblateMod.GetDeformity(1, u, v) - 1) * ScaledSpace.InverseScaleFactor;
                baseOuterRadius = __instance.outerRadius;
                baseOuterRadius2 = __instance.outerRadius2;
                baseInnerRadius = __instance.innerRadius;
                baseInnerRadius2 = __instance.innerRadius2;
                baseScale = __instance.scale;
                baseScaleOverScaleDepth = __instance.scaleOverScaleDepth;
                Debug.Log("offset is: " + offset);
                Debug.Log("baseOuterRadius is: " + baseOuterRadius);
                Debug.Log("baseOuterRadius2 is: " + baseOuterRadius2);
                Debug.Log("baseInnerRadius is: " + baseInnerRadius);
                Debug.Log("baseInnerRadius2 is: " + baseInnerRadius2);
                Debug.Log("baseInnerRadius is: " + baseScale);
                Debug.Log("baseInnerRadius2 is: " + baseScaleOverScaleDepth);
                __instance.outerRadius += (float)offset;
                __instance.outerRadius2 = __instance.outerRadius * __instance.outerRadius;
                __instance.innerRadius += (float)offset;
                __instance.innerRadius2 = __instance.innerRadius * __instance.innerRadius;
                __instance.scale = 1.0f / (__instance.outerRadius - __instance.innerRadius);
                __instance.scaleOverScaleDepth = __instance.scale / __instance.scaleDepth;
                Debug.Log("__instance.outerRadius is: " + __instance.outerRadius);
                Debug.Log("__instance.outerRadius2 is: " + __instance.outerRadius2);
                Debug.Log("__instance.innerRadius is: " + __instance.innerRadius);
                Debug.Log("__instance.innerRadius2 is: " + __instance.innerRadius2);
                Debug.Log("__instance.scale is: " + __instance.scale);
                Debug.Log("__instance.scaleOverScaleDepth is: " + __instance.scaleOverScaleDepth);
                Debug.Log("Done with Prefix for SetMaterial");
                initialSet = true;
            }
            return true;
        }

        private static void Postfix(AtmosphereFromGround __instance)
        {
            CelestialBody planet = __instance.planet;
            PQSMod_VertexHeightOblateAdvanced currentBodyOblateMod = Kopernicus.Utility.GetMod<PQSMod_VertexHeightOblateAdvanced>(planet.pqsController);
            if (currentBodyOblateMod != null)
            {
                Debug.Log("In Postfix for SetMaterial");
                Debug.Log("__instance is: " + __instance);
                Debug.Log("__instance.outerRadius is: " + __instance.outerRadius);
                Debug.Log("__instance.outerRadius2 is: " + __instance.outerRadius2);
                Debug.Log("__instance.innerRadius is: " + __instance.innerRadius);
                Debug.Log("__instance.innerRadius2 is: " + __instance.innerRadius2);
                Debug.Log("__instance.scale is: " + __instance.scale);
                Debug.Log("__instance.scaleOverScaleDepth is: " + __instance.scaleOverScaleDepth);
                Debug.Log("baseOuterRadius is: " + baseOuterRadius);
                Debug.Log("baseOuterRadius2 is: " + baseOuterRadius2);
                Debug.Log("baseInnerRadius is: " + baseInnerRadius);
                Debug.Log("baseInnerRadius2 is: " + baseInnerRadius2);
                Debug.Log("baseInnerRadius is: " + baseScale);
                Debug.Log("baseInnerRadius2 is: " + baseScaleOverScaleDepth);
                __instance.outerRadius = baseOuterRadius;
                __instance.outerRadius2 = baseOuterRadius2;
                __instance.innerRadius = baseInnerRadius;
                __instance.innerRadius2 = baseInnerRadius2;
                __instance.scale = baseScale;
                __instance.scaleOverScaleDepth = baseScaleOverScaleDepth;
                Debug.Log("__instance.outerRadius is: " + __instance.outerRadius);
                Debug.Log("__instance.outerRadius2 is: " + __instance.outerRadius2);
                Debug.Log("__instance.innerRadius is: " + __instance.innerRadius);
                Debug.Log("__instance.innerRadius2 is: " + __instance.innerRadius2);
                Debug.Log("__instance.scale is: " + __instance.scale);
                Debug.Log("__instance.scaleOverScaleDepth is: " + __instance.scaleOverScaleDepth);
                Debug.Log("Done with Postfix for SetMaterial");
            }
        }
    }
}
