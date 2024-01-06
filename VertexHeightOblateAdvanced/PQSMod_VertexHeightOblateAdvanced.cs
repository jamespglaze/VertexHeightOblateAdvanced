using Kopernicus.Components;
using Kopernicus.Configuration;
using Kopernicus.Constants;
using System;
using System.Collections.Generic;
using Kopernicus.Configuration.Parsing;
using UnityEngine;
using UnityEngine.Rendering;

namespace VertexHeightOblateAdvanced
{
    public class PQSMod_VertexHeightOblateAdvanced : PQSMod
    {
        public enum OblateModes
        {
            PointEquipotential,
            Blend,
            //Maclaurin,
            //Jacobi,
            CustomEllipsoid,
        }

        public OblateModes mode = OblateModes.PointEquipotential;
        public double criticality = 0.0f;
        //public double period = 3600f;
        public double a = 1.0f;
        public double b = 1.0f;
        public double c = 1.0f;

        private double aSqr = 1.0f;
        private double bSqr = 1.0f;
        private double cSqr = 1.0f;

        private void PrecalculateValues()
        {
            // Clamp criticality to the interval [0,1]
            if (criticality < 0.0f)
            {
                criticality = 0.0f;
            }
            if (criticality > 1.0f)
            {
                criticality = 1.0f;
            }
            // Precompute squares of each axis
            aSqr = Math.Pow(a, 2);
            bSqr = Math.Pow(b, 2);
            cSqr = Math.Pow(c, 2);

            /*switch (mode)
            {

                case OblateModes.PointEquipotential:
                    break;
                case OblateModes.Blend:
                    break;
                case OblateModes.CustomEllipsoid:
                    break;
                default:
                    break; ;
            }*/
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

        private double CalculateDeformityCustomEllipsoid(double phi, double theta)
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

        private double CalculateDeformity(double vertHeight, double u, double v)
        {
            double phi = 2* Math.PI * u;
            double theta = Math.PI * v;

            switch (mode)
            {
                case OblateModes.PointEquipotential:
                    return vertHeight * CalculateDeformityPointEquipotential(theta);
                case OblateModes.Blend:
                    return vertHeight * CalculateDeformityPointEquipotential(theta) * CalculateDeformityCustomEllipsoid(phi, theta);
                case OblateModes.CustomEllipsoid:
                    return vertHeight * CalculateDeformityCustomEllipsoid(phi, theta);
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
