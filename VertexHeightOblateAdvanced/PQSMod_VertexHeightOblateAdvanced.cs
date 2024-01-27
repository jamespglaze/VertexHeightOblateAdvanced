using Kopernicus.Components;
using Kopernicus.Configuration;
using Kopernicus.Constants;
using System;
using System.Collections.Generic;
using Kopernicus.Configuration.Parsing;
using UnityEngine;
using UnityEngine.Rendering;
using static VertexHeightOblateAdvanced.PQSMod_VertexHeightOblateAdvanced;

namespace VertexHeightOblateAdvanced
{
    public class PQSMod_VertexHeightOblateAdvanced : PQSMod
    {
        public enum OblateModes
        {
            PointEquipotential,
            UniformEquipotential,
            Blend,
            CustomEllipsoid,
            ContactBinary,
        }
        public enum EnergyModes
        {
            Low,
            High,
        }

        public OblateModes oblateMode = OblateModes.PointEquipotential;
        public EnergyModes energyMode = EnergyModes.Low;

        public double mass = 0.0f;
        public double radius = 0.0f;
        public double geeASL = 0.0f;
        public double period = 0.0f;
        public double a = 1.0f;
        public double b = 1.0f;
        public double c = 1.0f;
        public double primaryRadius = 1.0f;
        public double secondaryRadius = 1.0f;

        private double criticality = 0.0f;
        private double aSqr = 1.0f;
        private double bSqr = 1.0f;
        private double cSqr = 1.0f;
        private double primarySlope = 0.0f;
        private double secondarySlope = 0.0f;
        private double primarySlopeXLimit = 0.0f;
        private double secondarySlopeXLimit = 0.0f;

        private void PrecalculateValues()
        {
            switch (oblateMode)
            {
                case OblateModes.CustomEllipsoid:
                    (a, b, c, aSqr, bSqr, cSqr) = DuckMathUtils.PrecalculateConstantsEllipsoid(a, b, c);
                    break;
                case OblateModes.ContactBinary:
                    (primaryRadius, secondaryRadius, primarySlope, secondarySlope, primarySlopeXLimit, secondarySlopeXLimit) = DuckMathUtils.PrecalculateConstantsContactBinary(primaryRadius, secondaryRadius);
                    break;
                default:
                    break;
            }

            //Short circuit if not enough values provided in config
            if ((radius <= 0.0f ? 1 : 0) + (mass <= 0.0f ? 1 : 0) + (geeASL <= 0.0f ? 1 : 0) > 1 || period <= 0.0f)
            {
                return;
            }

            // Set mass if geeASL and radius given
            mass = mass == 0.0f ? Math.Pow(radius, 2) * geeASL * PhysicsGlobals.GravitationalAcceleration / DuckMathUtils.G : mass;
            // Set radius if mass and geeASL given
            radius = radius == 0.0f ? Math.Sqrt(DuckMathUtils.G * mass * geeASL * PhysicsGlobals.GravitationalAcceleration) : radius;

            switch (oblateMode)
            {
                case OblateModes.PointEquipotential:
                    criticality = DuckMathUtils.PrecalculateConstantsPointEquipotential(mass, radius, period);
                    break;
                case OblateModes.UniformEquipotential:
                    if (energyMode == EnergyModes.Low)
                        (a, b, c, aSqr, bSqr, cSqr) = DuckMathUtils.PrecalculateConstantsUniformEquipotential(EnergyModes.Low, mass, radius, period);
                    if (energyMode == EnergyModes.High)
                        (a, b, c, aSqr, bSqr, cSqr) = DuckMathUtils.PrecalculateConstantsUniformEquipotential(EnergyModes.High, mass, radius, period);
                    break;
                case OblateModes.Blend:
                    criticality = DuckMathUtils.PrecalculateConstantsPointEquipotential(mass, radius, period);
                    break;
                default:
                    break;
            }
            (a, b, c, aSqr, bSqr, cSqr) = DuckMathUtils.PrecalculateConstantsEllipsoid(a, b, c);
        }

        public double GetDeformity(double vertHeight, double u, double v)
        {
            double phi = 2 * Math.PI * u;
            double theta = Math.PI * v;

            switch (oblateMode)
            {
                case OblateModes.PointEquipotential:
                    return vertHeight * DuckMathUtils.CalculateDeformityPointEquipotential(theta, criticality);
                case OblateModes.Blend:
                    return vertHeight * DuckMathUtils.CalculateDeformityPointEquipotential(theta, criticality) * DuckMathUtils.CalculateDeformityEllipsoid(phi, theta, aSqr, bSqr, cSqr);
                case OblateModes.UniformEquipotential:
                case OblateModes.CustomEllipsoid:
                    return vertHeight * DuckMathUtils.CalculateDeformityEllipsoid(phi, theta, aSqr, bSqr, cSqr);
                case OblateModes.ContactBinary:
                    return vertHeight * DuckMathUtils.CalculateDeformityContactBinary(phi, theta, primaryRadius, secondaryRadius, primarySlope, secondarySlope, primarySlopeXLimit, secondarySlopeXLimit);
                default:
                    return vertHeight;
            }
        }

        public Vector3 GetNormal(double u, double v)
        {
            double phi = 2 * Math.PI * u;
            double theta = Math.PI * v;
            switch (oblateMode)
            {
                case OblateModes.PointEquipotential:
                    return DuckMathUtils.CalculateNormalPointEquipotential(phi, theta, criticality);
                case OblateModes.Blend:
                    return DuckMathUtils.CalculateNormalBlend(phi, theta, criticality, aSqr, bSqr, cSqr);
                case OblateModes.UniformEquipotential:
                case OblateModes.CustomEllipsoid:
                    return DuckMathUtils.CalculateNormalEllipsoid(phi, theta, aSqr, bSqr, cSqr);
                case OblateModes.ContactBinary:
                    return DuckMathUtils.CalculateNormalContactBinary(phi, theta, primaryRadius, secondaryRadius, primarySlope, secondarySlope, primarySlopeXLimit, secondarySlopeXLimit);
                default:
                    return new Vector3();
            }
        }

        public double GetMaxDeformity(double vertHeight)
        {
            if (secondaryRadius < primaryRadius)
            {
                return GetDeformity(vertHeight, 0.0f, 0.5f);
            }
            if (primaryRadius < secondaryRadius)
            {
                return GetDeformity(vertHeight, 0.5f, 0.5f);
            }
            if (a < c && b < c)
            {
                return GetDeformity(vertHeight, 0.0f, 0.0f);
            }
            if(a < b && c < b)
            {
                return GetDeformity(vertHeight, 0.25f, 0.5f);
            }
            return GetDeformity(vertHeight, 0.0f, 0.5f);
        }

        public override void OnSetup()
        {
            base.OnSetup();
            PrecalculateValues();
        }

        public override void OnVertexBuildHeight(PQS.VertexBuildData data)
        {
            //Apply height
            data.vertHeight = GetDeformity(data.vertHeight, data.u, data.v);
        }
    }
}
