using System;
using Kopernicus.ConfigParser.Attributes;
using Kopernicus.ConfigParser.BuiltinTypeParsers;
using Kopernicus.Configuration.ModLoader;
using Kopernicus.Configuration.Parsing;
using Kopernicus.ConfigParser.Enumerations;
using UnityEngine;
using Kopernicus.Configuration.Enumerations;
using LibNoise;
using static VertexHeightOblateAdvanced.PQSMod_VertexHeightOblateAdvanced;

namespace VertexHeightOblateAdvanced
{
    [RequireConfigType(ConfigType.Node)]
    public class VertexHeightOblateAdvanced : ModLoader<PQSMod_VertexHeightOblateAdvanced>
    {
        // What oblate mode to use for this mod
        // PointEquipotential: Follow gravitational equipotential of a rotating point mass
        // Blend: PointEquipotential multiplied by CustomEllipsoid
        // Maclaurin: 2-Axis oblate spheroid solution for gravitational equipotential of rotating body of uniform density
        // Jacobi: 3-Axis ellipsoid solution for gravitational equipotential of rotating body of uniform density
        // CustomEllipsoid: Generate ellipsoid from provided 3 axis a, b, c
        [ParserTarget("mode")]
        public EnumParser<OblateModes> Mode
        {
            get { return Mod.mode; }
            set { Mod.mode = value.Value; }
        }

        // Angular velocity as a fraction of breakup angular velocity
        [ParserTarget("criticality")]
        public NumericParser<Double> Criticality
        {
            get { return Mod.criticality; }
            set { Mod.criticality = value; }
        }

        // Rotational period of the body
        /*[ParserTarget("period")]
        public NumericParser<Double> Period
        {
            get { return Mod.period; }
            set { Mod.period = value; }
        }*/

        // Primary equatorial axis as ratio of nominal body radius
        [ParserTarget("a")]
        public NumericParser<Double> A
        {
            get { return Mod.a; }
            set { Mod.a = value; }
        }

        // Secondary equatorial axis as ratio of nominal body radius
        [ParserTarget("b")]
        public NumericParser<Double> B
        {
            get { return Mod.b; }
            set { Mod.b = value; }
        }

        // Polar axis as ratio of nominal body radius
        [ParserTarget("c")]
        public NumericParser<Double> C
        {
            get { return Mod.c; }
            set { Mod.c = value; }
        }
    }
}
