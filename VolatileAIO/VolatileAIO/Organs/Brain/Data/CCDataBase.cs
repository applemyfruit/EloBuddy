using System.Linq;

namespace VolatileAIO.Organs.Brain.Data
{
    public class CCDataBase
    {
        static readonly string[] _nonskillshots =
           {
                "ShenShadowDash", "Pulverize", "GragasE", "FizzPiercingStrike", "reksaiqburrowed",
                "RunePrison", "Fling", "NocturneUnspeakableHorror", "SejuaniArcticAssault", "ShyvanaTransformCast",
                "PantheonW", "Ice Blast", "Terrify", "GalioIdolOfDurand", "GnarR", "JaxCounterStrike", "BlindMonkRKick",
                "UFSlash", "JayceThunderingBlow", "ZacR", "StaticField", "GarenQ", "TalonCutthroat", "ViR"
            };

        static readonly string[] _skillShots =
        {
                "AhriSeduce", "AhriOrbofDeception", "SwainShadowGrasp", "syndrae5", "SyndraE", "TahmKenchQ", "VarusE",
                "VeigarBalefulStrike", "VeigarDarkMatter", "VeigarEventHorizon", "VelkozQ", "VelkozQSplit", "VelkozE",
                "Laser", "Vi-q", "xeratharcanopulse2", "XerathArcaneBarrage2", "XerathMageSpear",
                "xerathrmissilewrapper", "BraumQ", "RocketGrab", "JavelinToss", "BrandBlazeMissile", "Heimerdingerwm",
                "JannaQ", "JarvanIVEQ", "BandageToss", "CaitlynEntrapment", "PhosphorusBomb", "MissileBarrage2",
                "DariusAxeGrabCone", "DianaArc", "DianaArcArc", "InfectedCleaverMissileCast", "DravenDoubleShot",
                "EkkoQ", "EkkoW", "EkkoR", "EliseHumanE", "GalioResoluteSmite", "GalioRighteousGust",
                "GalioIdolOfDurand", "CurseoftheSadMummy", "FlashFrost", "EvelynnR", "QuinnQ", "yasuoq3w",
                "RengarEFinal", "ZiggsW", "ZyraGraspingRoots", "ZyraBrambleZone", "Dazzle", "FiddlesticksDarkWind",
                "FeralScream", "ZiggsW", "ViktorChaosStorm", "AlZaharCalloftheVoid",
                "RumbleCarpetBombMissile", "ThreshQ", "ThreshE", "NamiQ", "DarkBindingMissile", "OrianaDetonateCommand",
                "NautilusAnchorDrag",
                "SejuaniGlacialPrisonCast", "SonaR", "VarusR", "rivenizunablade", "EnchantedCrystalArrow", "BardR",
                "InfernalGuardian",
                "CassiopeiaPetrifyingGaze",
                "BraumRWrapper", "FizzMarinerDoomMissile", "ViktorDeathRay", "ViktorDeathRay3", "XerathMageSpear",
                "GragasR", "HecarimUlt", "LeonaSolarFlare", "LissandraR", "LuxLightBinding", "LuxMaliceCannon", "JinxW",
                "LuxLightStrikeKugel"
            };

        public static bool IsCC(string spell)
        {
            return _skillShots.Contains(spell) || _nonskillshots.Contains(spell);
        }

        public static bool IsCC_SkillShot(string spell)
        {
            return _skillShots.Contains(spell);
        }

        public static bool IsCC_NonSkillShot(string spell)
        {
            return _nonskillshots.Contains(spell);
        }
    }
}