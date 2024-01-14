﻿using Kopernicus.Components;
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

        const double G = 6.67430E-011;
        private double criticality = 0.0f;
        private double aSqr = 1.0f;
        private double bSqr = 1.0f;
        private double cSqr = 1.0f;
        private double primarySlope = 0.0f;
        private double secondarySlope = 0.0f;
        private double primarySlopeXLimit = 0.0f;
        private double secondarySlopeXLimit = 0.0f;

        private static double[][] lowEnergyLookup =
        {
            new double[] { 132859427, 1.00001, 1.00001 },
            new double[] { 74739908, 1.0000316, 1.0000316 },
            new double[] { 42015940, 1.0001, 1.0001 },
            new double[] { 23638912, 1.000316, 1.000316 },
            new double[] { 13293866, 1.001, 1.001 },
            new double[] { 7488184, 1.00316, 1.00316 },
            new double[] { 4226839, 1.01, 1.01 },
            new double[] { 3006847, 1.02, 1.02 },
            new double[] { 2469786, 1.03, 1.03 },
            new double[] { 2151625, 1.04, 1.04 },
            new double[] { 1935850, 1.05, 1.05 },
            new double[] { 1777562, 1.06, 1.06 },
            new double[] { 1655308, 1.07, 1.07 },
            new double[] { 1557380, 1.08, 1.08 },
            new double[] { 1476775, 1.09, 1.09 },
            new double[] { 1409016, 1.10, 1.10 },
            new double[] { 1351091, 1.11, 1.11 },
            new double[] { 1300889, 1.12, 1.12 },
            new double[] { 1256882, 1.13, 1.13 },
            new double[] { 1217931, 1.14, 1.14 },
            new double[] { 1183169, 1.15, 1.15 },
            new double[] { 1151925, 1.16, 1.16 },
            new double[] { 1123666, 1.17, 1.17 },
            new double[] { 1097966, 1.18, 1.18 },
            new double[] { 1074480, 1.19, 1.19 },
            new double[] { 1052922, 1.20, 1.20 },
            new double[] { 1033057, 1.21, 1.21 },
            new double[] { 1014687, 1.22, 1.22 },
            new double[] { 997645, 1.23, 1.23 },
            new double[] { 981788, 1.24, 1.24 },
            new double[] { 966995, 1.25, 1.25 },
            new double[] { 953159, 1.26, 1.26 },
            new double[] { 940190, 1.27, 1.27 },
            new double[] { 928008, 1.28, 1.28 },
            new double[] { 916543, 1.29, 1.29 },
            new double[] { 905733, 1.30, 1.30 },
            new double[] { 895524, 1.31, 1.31 },
            new double[] { 885867, 1.32, 1.32 },
            new double[] { 876719, 1.33, 1.33 },
            new double[] { 868040, 1.34, 1.34 },
            new double[] { 859798, 1.35, 1.35 },
            new double[] { 851959, 1.36, 1.36 },
            new double[] { 844496, 1.37, 1.37 },
            new double[] { 837383, 1.38, 1.38 },
            new double[] { 830596, 1.39, 1.39 },
            new double[] { 824116, 1.40, 1.40 },
            new double[] { 817921, 1.41, 1.41 },
            new double[] { 811995, 1.42, 1.42 },
        };
        private static double[][] highEnergyLookup =
        {
            new double[] { 813345, 2.89, 1.253 },
            new double[] { 810913, 2.87, 1.256 },
            new double[] { 808351, 2.85, 1.260 },
            new double[] { 805940, 2.83, 1.263 },
            new double[] { 803402, 2.81, 1.267 },
            new double[] { 800894, 2.79, 1.270 },
            new double[] { 798614, 2.77, 1.274 },
            new double[] { 796114, 2.75, 1.278 },
            new double[] { 791900, 2.71, 1.281 },
            new double[] { 791394, 2.71, 1.285 },
            new double[] { 788932, 2.69, 1.289 },
            new double[] { 786483, 2.67, 1.293 },
            new double[] { 784162, 2.65, 1.297 },
            new double[] { 781740, 2.63, 1.301 },
            new double[] { 779559, 2.61, 1.305 },
            new double[] { 777278, 2.59, 1.309 },
            new double[] { 774896, 2.57, 1.313 },
            new double[] { 772421, 2.54, 1.319 },
            new double[] { 770184, 2.52, 1.323 },
            new double[] { 767966, 2.50, 1.328 },
            new double[] { 765773, 2.48, 1.334 },
            new double[] { 763368, 2.46, 1.339 },
            new double[] { 761192, 2.44, 1.343 },
            new double[] { 758959, 2.42, 1.350 },
            new double[] { 756736, 2.40, 1.356 },
            new double[] { 754627, 2.38, 1.361 },
            new double[] { 752355, 2.36, 1.368 },
            new double[] { 750177, 2.34, 1.373 },
            new double[] { 747954, 2.31, 1.380 },
            new double[] { 745865, 2.29, 1.387 },
            new double[] { 743615, 2.27, 1.395 },
            new double[] { 741475, 2.25, 1.402 },
            new double[] { 739287, 2.23, 1.410 },
            new double[] { 737236, 2.20, 1.418 },
            new double[] { 735181, 2.18, 1.428 },
            new double[] { 732993, 2.16, 1.436 },
            new double[] { 730954, 2.13, 1.447 },
            new double[] { 728882, 2.11, 1.456 },
            new double[] { 726919, 2.08, 1.469 },
            new double[] { 724872, 2.06, 1.481 },
            new double[] { 722689, 2.03, 1.493 },
            new double[] { 720670, 2.00, 1.509 },
            new double[] { 718643, 1.97, 1.525 },
            new double[] { 716648, 1.94, 1.545 },
            new double[] { 714624, 1.90, 1.567 },
            new double[] { 712570, 1.86, 1.596 },
            new double[] { 710533, 1.80, 1.638 },
            new double[] { 709305, 1.72, 1.716 },
            new double[] { 709304, 1.72, 1.72 },
            new double[] { 710537, 1.71, 1.71 },
            new double[] { 712615, 1.70, 1.70 },
            new double[] { 714760, 1.69, 1.69 },
            new double[] { 716973, 1.68, 1.68 },
            new double[] { 719257, 1.67, 1.67 },
            new double[] { 721616, 1.66, 1.66 },
            new double[] { 724052, 1.65, 1.65 },
            new double[] { 726568, 1.64, 1.64 },
            new double[] { 729168, 1.63, 1.63 },
            new double[] { 731855, 1.62, 1.62 },
            new double[] { 734633, 1.61, 1.61 },
            new double[] { 737506, 1.60, 1.60 },
            new double[] { 740479, 1.59, 1.59 },
            new double[] { 743555, 1.58, 1.58 },
            new double[] { 746740, 1.57, 1.57 },
            new double[] { 750039, 1.56, 1.56 },
            new double[] { 753456, 1.55, 1.55 },
            new double[] { 756999, 1.54, 1.54 },
            new double[] { 760672, 1.53, 1.53 },
            new double[] { 764483, 1.52, 1.52 },
            new double[] { 768439, 1.51, 1.51 },
            new double[] { 772546, 1.50, 1.50 },
            new double[] { 776814, 1.49, 1.49 },
            new double[] { 781251, 1.48, 1.48 },
            new double[] { 785866, 1.47, 1.47 },
            new double[] { 790669, 1.46, 1.46 },
            new double[] { 795671, 1.45, 1.45 },
            new double[] { 800885, 1.44, 1.44 },
            new double[] { 806321, 1.43, 1.43 },
            new double[] { 811995, 1.42, 1.42 },
        };

        private void PrecalculateValues()
        {
            switch (oblateMode)
            {
                case OblateModes.CustomEllipsoid:
                    PrecalculateShapeEllipsoid();
                    break;
                case OblateModes.ContactBinary:
                    PrecalculateShapeContatBinary();
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
            mass = mass == 0.0f ? Math.Pow(radius, 2) * geeASL * PhysicsGlobals.GravitationalAcceleration / G : mass;
            // Set radius if mass and geeASL given
            radius = radius == 0.0f ? Math.Sqrt(G * mass * geeASL * PhysicsGlobals.GravitationalAcceleration) : radius;

            switch (oblateMode)
            {
                case OblateModes.PointEquipotential:
                    PrecalculateShapePointEquipotential();
                    break;
                case OblateModes.UniformEquipotential:
                    if (energyMode == EnergyModes.Low)
                        PrecalculateShapeUniformEquipotential(lowEnergyLookup);
                    if (energyMode == EnergyModes.High)
                        PrecalculateShapeUniformEquipotential(highEnergyLookup);
                    break;
                case OblateModes.Blend:
                    PrecalculateShapePointEquipotential();
                    break;
                default:
                    break;
            }
            PrecalculateShapeEllipsoid();
        }

        private void PrecalculateShapePointEquipotential()
        {
            double angularVelocity = 2.0 * Math.PI / period;
            double criticalAngularVelocity = Math.Sqrt(G * mass / (1.5f * radius)) / (1.5f * radius);
            criticality = angularVelocity / criticalAngularVelocity;
            // Clamp criticality to the interval [0,1]
            criticality = criticality < 0.0f ? 0.0f
                : criticality > 1.0f ? 1.0f
                : criticality;
        }

        private void PrecalculateShapeUniformEquipotential(double[][] lookup)
        {
            double density = mass / (Math.Pow(radius, 3) * Math.PI * 4 / 3);
            {
                for (int i = 0; i < lookup.Length; i++)
                {
                    if (i == lookup.Length - 1)
                    {
                        // Period is shorter than shortest possible period, clamp to values for shortest period
                        a = lookup[i][1];
                        b = lookup[i][2];
                        c = 1;
                        break;
                    }
                    if (lookup[i][0] / Math.Sqrt(density / (lookup[i][1] * lookup[i][2])) < period)
                    {
                        if (i == 0)
                        {
                            break;
                        }
                        double interval = (period * Math.Sqrt(density / (lookup[i][1] * lookup[i][2])) - lookup[i][0]) / (lookup[i - 1][0] - lookup[i][0]);
                        a = lookup[i][1] + interval * (lookup[i - 1][1] - lookup[i][1]);
                        b = lookup[i][2] + interval * (lookup[i - 1][2] - lookup[i][2]);
                        c = 1;
                        break;
                    }
                }
                aSqr = Math.Pow(a, 2);
                bSqr = Math.Pow(b, 2);
            }
        }
        private void PrecalculateShapeEllipsoid()
        {
            // Clamp a, b, and c to one or greater
            a = a < 1 ? 1 : a;
            b = b < 1 ? 1 : b;
            c = c < 1 ? 1 : c;

            // Precompute squares of each axis
            aSqr = Math.Pow(a, 2);
            bSqr = Math.Pow(b, 2);
            cSqr = Math.Pow(c, 2);
        }

        private void PrecalculateShapeContatBinary()
        {
            Debug.Log("PrecalculateShapeContatBinary: Running");
            // Clamp primaryRadius to the interval [1,2]
            primaryRadius = primaryRadius < 1.0f ? 1.0f
                : primaryRadius > 2.0f ? 2.0f
                : primaryRadius;
            Debug.Log("primaryRadius: " + primaryRadius.ToString());
            // Clamp secondaryRadius to the interval [1,primaryRadius]
            secondaryRadius = secondaryRadius < 1.0f ? 1.0f
                : secondaryRadius > primaryRadius ? primaryRadius
                : secondaryRadius;
            Debug.Log("secondaryRadius: " + secondaryRadius.ToString());

            primarySlope = Math.Pow(primaryRadius, 2) - 1;
            secondarySlope = Math.Pow(secondaryRadius, 2) - 1;
            Debug.Log("primarySlope: " + primarySlope.ToString());
            Debug.Log("secondarySlope: " + secondarySlope.ToString());

            primarySlopeXLimit = primaryRadius / (1 + primarySlope);
            secondarySlopeXLimit = -secondaryRadius / (1 + secondarySlope);
            Debug.Log("primarySlopeXLimit: " + primarySlopeXLimit.ToString());
            Debug.Log("secondarySlopeXLimit: " + secondarySlopeXLimit.ToString());
        }

        private double CalculateDeformityPointEquipotential(double theta)
        {
            if (theta <= 0.0f || theta >= Math.PI * 1.0f || criticality == 0.0f)
            {
                return 1;
            }
            try
            {
                double heightScaleFactor = 3 * Math.Cos((Math.PI + Math.Acos(criticality * Math.Sin(theta))) / 3) / (criticality * Math.Sin(theta));
                if (heightScaleFactor > 1.5f)
                {
                    return 1.5f;
                }
                if (heightScaleFactor < 1.0f)
                {
                    return 1;
                }
                return heightScaleFactor;
            }
            catch (Exception e)
            {
                return 1;
            }
        }

        private double CalculateDeformityEllipsoid(double phi, double theta)
        {
            try
            {
                double term1 = Math.Pow(Math.Sin(theta), 2) * Math.Pow(Math.Cos(phi), 2) / aSqr;
                double term2 = Math.Pow(Math.Sin(theta), 2) * Math.Pow(Math.Sin(phi), 2) / bSqr;
                double term3 = Math.Pow(Math.Cos(theta), 2) / cSqr;
                return 1 / Math.Sqrt(term1 + term2 + term3);
            }
            catch (Exception e)
            {
                return 1;
            }
        }

        private double CalculateDeformityContactBinary(double phi, double theta)
        {
            try
            {
                double denominator = 1.0f;
                double xValue = 0.0f;
                if ((-Math.PI / 2 < phi && phi < Math.PI / 2) || (Math.PI * 1.5f < phi && phi < Math.PI * 2f))
                {
                    denominator = 1 - ((1 + primarySlope) * Math.Pow(Math.Sin(theta) * Math.Cos(phi), 2));
                    xValue = Math.Sin(theta) * Math.Cos(phi) / Math.Sqrt(denominator);
                    if (xValue < primarySlopeXLimit)
                    {
                        return Math.Sqrt(1 / denominator);
                    }
                    return 2 * primaryRadius * Math.Sin(theta) * Math.Cos(phi);
                }
                if ((Math.PI / 2 < phi && phi < Math.PI * 1.5f) || (-Math.PI < phi && phi < -Math.PI / 2))
                {
                    denominator = 1 - ((1 + secondarySlope) * Math.Pow(Math.Sin(theta) * Math.Cos(phi), 2));
                    xValue = Math.Sin(theta) * Math.Cos(phi) / Math.Sqrt(denominator);
                    if (xValue > secondarySlopeXLimit)
                    {
                        return Math.Sqrt(1 / denominator);
                    }
                    return -2 * secondaryRadius * Math.Sin(theta) * Math.Cos(phi);
                }
                return 1;
            }
            catch (Exception e)
            {
                return 1;
            }
        }

        private double CalculateDeformity(double vertHeight, double u, double v)
        {
            double phi = 2 * Math.PI * u;
            double theta = Math.PI * v;

            switch (oblateMode)
            {
                case OblateModes.PointEquipotential:
                    return vertHeight * CalculateDeformityPointEquipotential(theta);
                case OblateModes.Blend:
                    return vertHeight * CalculateDeformityPointEquipotential(theta) * CalculateDeformityEllipsoid(phi, theta);
                case OblateModes.UniformEquipotential:
                case OblateModes.CustomEllipsoid:
                    return vertHeight * CalculateDeformityEllipsoid(phi, theta);
                case OblateModes.ContactBinary:
                    return vertHeight * CalculateDeformityContactBinary(phi, theta);
                default:
                    return vertHeight;
            }
        }

        public override void OnSetup()
        {
            base.OnSetup();
            PrecalculateValues();
        }

        public override void OnVertexBuildHeight(PQS.VertexBuildData data)
        {
            //Apply height
            data.vertHeight = CalculateDeformity(data.vertHeight, data.u, data.v);
        }
    }
}
