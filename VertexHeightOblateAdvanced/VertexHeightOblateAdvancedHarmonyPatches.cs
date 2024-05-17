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
    public class VertexHeightOblateAdvancedHarmonyPatches : MonoBehaviour
    {
        public void Awake()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Harmony harmony = new Harmony("VertexHeightOblateAdvanced");
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
        public class AtmosphereFromGroundCache
        {
            internal float cameraHeight = 0.0f;
            internal float cameraHeight2 = 0.0f;
            internal float camHeightUnderwater = 0.0f;
            internal float lightDot = 0.0f;
            internal float KrESun = 0.0f;

            internal float outerRadius = 0.0f;
            internal float outerRadius2 = 0.0f;
            internal float innerRadius = 0.0f;
            internal float innerRadius2 = 0.0f;
            internal float KmESun = 0.0f;
            internal float Kr4PI = 0.0f;
            internal float Km4PI = 0.0f;
            internal float scale = 0.0f;
            internal float scaleDepth = 0.0f;
            internal float scaleOverScaleDepth = 0.0f;
            internal float samples = 0.0f;
            internal float g = 0.0f;
            internal float g2 = 0.0f;
            internal float exposure = 0.0f;
            internal float underwaterOpacityAltBase = 0.0f;
            internal float underwaterOpacityAltMult = 0.0f;
            internal float scaleToApply = 0.0f;
        }

        public static Dictionary<String, Vector3> baseBodyScaleCache = new Dictionary<String, Vector3>();

        public static Dictionary<String, AtmosphereFromGroundCache> baseBodyCaches = new Dictionary<String, AtmosphereFromGroundCache>();
        public static Dictionary<String, AtmosphereFromGroundCache> overrideBodyCaches = new Dictionary<String, AtmosphereFromGroundCache>();

        public static Dictionary<String, Vector3> overrideRenderScaleCaches = new Dictionary<String, Vector3>();
        public static Dictionary<String, Vector3> overrideTransformScaleCaches = new Dictionary<String, Vector3>();
        public static Dictionary<String, float> overrideRadiusScaleCaches = new Dictionary<String, float>();

        private static bool Prefix(AtmosphereFromGround __instance, ref bool initialSet, ref Renderer ___r)
        {
            CelestialBody planet = __instance.planet;
            if (planet.pqsController != null)
            {
                PQSMod_VertexHeightOblateAdvanced currentBodyOblateMod = Kopernicus.Utility.GetMod<PQSMod_VertexHeightOblateAdvanced>(planet.pqsController);
                if (currentBodyOblateMod != null)
                {

                    AtmosphereFromGroundCache baseBodyCache;
                    if (baseBodyCaches.TryGetValue(planet.name, out baseBodyCache) == false)
                    {
                        baseBodyCache = new AtmosphereFromGroundCache();
                        foreach (FieldInfo field in typeof(AtmosphereFromGroundCache).GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                        {
                            field.SetValue(baseBodyCache, typeof(AtmosphereFromGround).GetField(field.Name).GetValue(__instance));
                        }
                        baseBodyCaches.Add(planet.name, baseBodyCache);
                        baseBodyScaleCache.Add(planet.name, __instance.transform.localScale);
                    }
                    foreach (FieldInfo field in typeof(AtmosphereFromGroundCache).GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        field.SetValue(baseBodyCache, typeof(AtmosphereFromGround).GetField(field.Name).GetValue(__instance));
                    }

                    AtmosphereFromGroundCache overrideBodyCache;
                    if (overrideBodyCaches.TryGetValue(planet.name, out overrideBodyCache) == false)
                    {
                        overrideBodyCache = new AtmosphereFromGroundCache();
                        foreach (FieldInfo field in typeof(AtmosphereFromGroundCache).GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                        {
                            field.SetValue(overrideBodyCache, typeof(AtmosphereFromGround).GetField(field.Name).GetValue(__instance));
                        }
                        overrideBodyCaches.Add(planet.name, overrideBodyCache);
                        overrideRenderScaleCaches.Add(planet.name, new Vector3(1.025f ,1.025f ,1.025f));
                        overrideTransformScaleCaches.Add(planet.name, new Vector3(1.025f, 1.025f, 1.025f));
                        overrideRadiusScaleCaches.Add(planet.name, 1f);
                    }
                    foreach (FieldInfo field in typeof(AtmosphereFromGroundCache).GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        //Debug.Log(field.Name + "is: " + field.GetValue(overrideBodyCaches));
                        typeof(AtmosphereFromGround).GetField(field.Name).SetValue(__instance, field.GetValue(overrideBodyCache));
                    }

                    //Debug.Log("In Prefix for SetMaterial");
                    double v = 0.5f + (planet.GetLatitude(__instance.mainCamera.position) / 180.0f);
                    double u = 0.25f - (planet.GetLongitude(__instance.mainCamera.position) / (2 * 180.0f));
                    double offset = planet.Radius * (currentBodyOblateMod.GetDeformity(1, u, v)) * ScaledSpace.InverseScaleFactor;
                    double innerRadus = planet.Radius * (currentBodyOblateMod.GetDeformity(1, u, v)) * ScaledSpace.InverseScaleFactor;

                    __instance.outerRadius = (float)offset * 1.025f;
                    __instance.outerRadius2 = __instance.outerRadius * __instance.outerRadius;
                    __instance.innerRadius = (float)offset * 1.025f * 0.975f;
                    __instance.innerRadius2 = __instance.innerRadius * __instance.innerRadius;
                    //overrideBodyCache.scale = __instance.scale = 1.0f / (__instance.outerRadius - __instance.innerRadius);
                    //__instance.scaleDepth = overrideBodyCache.scaleDepth;*/
                    //overrideBodyCache.scaleOverScaleDepth = __instance.scaleOverScaleDepth = __instance.scale / __instance.scaleDepth;
                    //Debug.Log("Done with Prefix for SetMaterial");

                    Vector3 baseBodyScale;
                    if (baseBodyScaleCache.TryGetValue(planet.name, out baseBodyScale) == true)
                    {
                        double atmosphereRatio = planet.atmosphereDepth / (planet.atmosphereDepth + planet.Radius);
                        Debug.Log("atmosphereRatio is: " + atmosphereRatio);
                        Vector3 overrideScale = new Vector3(baseBodyScale.x * 1.5f, baseBodyScale.y * 1.5f, baseBodyScale.z * 1.5f);
                        __instance.transform.localScale = overrideScale;
                        Shader shader = ___r.material.shader;
                        int shaderPropCount = shader.GetPropertyCount();
                        for (int i = 0; i < shaderPropCount; i++)
                        {
                            Debug.Log(shader.GetPropertyName(i) + ": " + shader.GetPropertyAttributes(i));
                        }
                    }
                    Vector3 overrideRenderScaleCache;
                    if (overrideRenderScaleCaches.TryGetValue(planet.name, out overrideRenderScaleCache) == true) {
                        ___r.transform.localScale = overrideRenderScaleCache;
                    }

                    Vector3 overrideTransformScaleCache;
                    if (overrideTransformScaleCaches.TryGetValue(planet.name, out overrideTransformScaleCache) == true) {
                        __instance.transform.localScale = overrideTransformScaleCache;
                    }

                    float overrideRadiusScaleCache;
                    if (overrideRadiusScaleCaches.TryGetValue(planet.name, out overrideRadiusScaleCache) == true) {
                        __instance.outerRadius *= overrideRadiusScaleCache;
                        __instance.outerRadius2 = __instance.outerRadius * __instance.outerRadius;
                        __instance.innerRadius *= overrideRadiusScaleCache;
                        __instance.innerRadius2 = __instance.innerRadius * __instance.innerRadius;
                    }

                    initialSet = true;
                }
            }
            return true;
        }

        private static void Postfix(AtmosphereFromGround __instance, ref Renderer ___r, ref MaterialPropertyBlock ___mpb)
        {
            CelestialBody planet = __instance.planet;
            if (planet.pqsController != null)
            {
                PQSMod_VertexHeightOblateAdvanced currentBodyOblateMod = Kopernicus.Utility.GetMod<PQSMod_VertexHeightOblateAdvanced>(planet.pqsController);
                if (currentBodyOblateMod != null)
                {
                    MaterialPropertyBlock newmbp = new MaterialPropertyBlock();
                    ___r.GetPropertyBlock(newmbp);
                    Debug.Log("___r MaterialPropertyBlock contents are: " + newmbp.ToString());
                    Debug.Log("___mpb MaterialPropertyBlock contents are: " + ___mpb.ToString());
                    foreach (Material newMaterial in ___r.sharedMaterials)
                    {
                        Debug.Log("material " + newMaterial.name + " contents are: " + newMaterial.ToString());
                    }
                    AtmosphereFromGroundCache baseBodyCache;
                    if (baseBodyCaches.TryGetValue(planet.name, out baseBodyCache) == false)
                    {
                        baseBodyCache = new AtmosphereFromGroundCache();
                        baseBodyCaches.Add(planet.name, baseBodyCache);
                    }
                    foreach (FieldInfo field in typeof(AtmosphereFromGroundCache).GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        //Debug.Log(field.Name + "is: " + field.GetValue(baseBodyCache));
                        typeof(AtmosphereFromGround).GetField(field.Name).SetValue(__instance, field.GetValue(baseBodyCache));
                    }
                    /* __instance.outerRadius = baseBodyCache.outerRadius;
                     __instance.outerRadius2 = baseBodyCache.outerRadius2;
                     __instance.innerRadius = baseBodyCache.innerRadius;
                     __instance.innerRadius2 = baseBodyCache.innerRadius2;
                     __instance.scale = baseBodyCache.scale;
                     __instance.scaleOverScaleDepth = baseBodyCache.scaleOverScaleDepth;*/
                    //Debug.Log("Done with Postfix for SetMaterial");
                }
            }
        }
    }
}
