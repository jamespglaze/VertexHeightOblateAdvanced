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
        // UniformEquipotential: Either Maclaurin (2-Axis oblate spheroid solution) or Jacobi (3-Axis ellipsoid solution)
        // Blend: PointEquipotential multiplied by CustomEllipsoid
        // CustomEllipsoid: Generate ellipsoid from provided 3 axis a, b, c
        // ContactBinary: God has abandoned us
        [ParserTarget("oblateMode")]
        public EnumParser<OblateModes> OblateMode
        {
            get { return Mod.oblateMode; }
            set { Mod.oblateMode = value.Value; }
        }

        // Energy level of the body, needed because there is more than one possible oblate value for many periods
        // Low: MacLaurin up to a polar to equatorial ratio of 1.42
        // High: MacLaurin between polar to equatorial ratios of 1.42 to 1.716, Jacobian between polar to major equatorial ratios of 1.716 to 2.850
        [ParserTarget("energyMode")]
        public EnumParser<EnergyModes> EnergyMode
        {
            get { return Mod.energyMode; }
            set { Mod.energyMode = value.Value; }
        }

        // Mass of the body
        [ParserTarget("mass")]
        public NumericParser<double> Mass
        {
            get { return Mod.mass; }
            set { Mod.mass = value; }
        }

        // Reference radius of the body
        [ParserTarget("radius")]
        public NumericParser<double> Radius
        {
            get { return Mod.radius; }
            set { Mod.radius = value; }
        }

        // Reference surface gravity of the body
        [ParserTarget("geeASL")]
        public NumericParser<double> GeeASL
        {
            get { return Mod.geeASL; }
            set { Mod.geeASL = value; }
        }

        // Rotational period of the body
        [ParserTarget("period")]
        public NumericParser<double> Period
        {
            get { return Mod.period; }
            set { Mod.period = value; }
        }

        // Primary equatorial axis as ratio of reference radius
        [ParserTarget("a")]
        public NumericParser<double> A
        {
            get { return Mod.a; }
            set { Mod.a = value; }
        }

        // Secondary equatorial axis as ratio of reference radius
        [ParserTarget("b")]
        public NumericParser<double> B
        {
            get { return Mod.b; }
            set { Mod.b = value; }
        }

        // Polar axis as ratio of reference radius
        [ParserTarget("c")]
        public NumericParser<double> C
        {
            get { return Mod.c; }
            set { Mod.c = value; }
        }

        // Radius of larger body as a ratio of neck radius
        //[ParserTarget("primaryRadius")]
        //public NumericParser<double> PrimaryRadius
        //{
        //    get { return Mod.primaryRadius; }
        //    set { Mod.primaryRadius = value; }
        //}

        // Radius of smaller body as a ratio of neck radius
        //[ParserTarget("secondaryRadius")]
        //public NumericParser<double> SecondaryRadius
        //{
        //    get { return Mod.secondaryRadius; }
        //    set { Mod.secondaryRadius = value; }
        //}
    }
}
