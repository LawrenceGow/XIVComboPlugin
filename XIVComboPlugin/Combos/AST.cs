using System.Linq;
using Dalamud.Game.ClientState.JobGauge.Enums;
using Dalamud.Game.ClientState.JobGauge.Types;

namespace XIVCombo.Combos;

internal static class AST
{
    public const byte JobID = 33;

    public const uint
        Draw = 3590,
        Redraw = 3593,
        Combust = 3599,
        Benefic = 3594,
        Malefic = 3596,
        Malefic2 = 3598,
        Helios = 3600,
        AspectedHelios = 3601,
        Ascend = 3603,
        Lightspeed = 3606,
        Benefic2 = 3610,
        Synastry = 3612,
        CollectiveUnconscious = 3613,
        Gravity = 3615,
        Balance = 4401,
        Bole = 4404,
        Arrow = 4402,
        Spear = 4403,
        Ewer = 4405,
        Spire = 4406,
        EarthlyStar = 7439,
        Malefic3 = 7442,
        MinorArcana = 7443,
        LordOfCrowns = 7444,
        LadyofCrowns = 7445,
        SleeveDraw = 7448,
        Divination = 16552,
        CelestialOpposition = 16553,
        Malefic4 = 16555,
        Horoscope = 16557,
        NeutralSect = 16559,
        Play = 17055,
        CrownPlay = 25869,
        Astrodyne = 25870,
        FallMalefic = 25871,
        Gravity2 = 25872,
        Exaltation = 25873,
        Macrocosmos = 25874,
        UmbralDraw = 37018,
        AstralDraw = 37017,
        Play1 = 37019,
        Play2 = 37020,
        Play3 = 37021,
        MinorArcanaDT = 37022,
        CombinedHelios = 37030,
        Oracle = 37029;

    public static class Buffs
    {
        public const ushort
            Divining = 3893;
    }

    public static class Debuffs
    {
        public const ushort
            Combust = 838,
            Combust2 = 843,
            Combust3 = 1881;
    }

    public static class Levels
    {
        public const byte
            Ascend = 12,
            Benefic2 = 26,
            AstralDraw = 30,
            Redraw = 40,
            Astrodyne = 50,
            MinorArcana = 70,
            CrownPlay = 70;
    }
}


internal class AstrologianPlay : CustomCombo
{
    protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.AstrologianPlayDrawFeature;

    protected override uint Invoke(uint actionID, uint lastComboMove, float comboTime, byte level)
    {
        var gauge = GetJobGauge<ASTGauge>();

        if (OriginalHook(actionID) == AST.Play1 && IsOriginal(AST.Play1))
            return gauge.ActiveDraw == DrawType.ASTRAL ? OriginalHook(AST.AstralDraw) : OriginalHook(AST.UmbralDraw);

        if (OriginalHook(actionID) == AST.Play2 && IsOriginal(AST.Play2))
            return gauge.ActiveDraw == DrawType.ASTRAL ? OriginalHook(AST.AstralDraw) : OriginalHook(AST.UmbralDraw);

        if (OriginalHook(actionID) == AST.Play3 && IsOriginal(AST.Play3))
            return gauge.ActiveDraw == DrawType.ASTRAL ? OriginalHook(AST.AstralDraw) : OriginalHook(AST.UmbralDraw);

        if (OriginalHook(actionID) == AST.MinorArcanaDT && IsOriginal(AST.MinorArcanaDT))
            return gauge.ActiveDraw == DrawType.ASTRAL ? OriginalHook(AST.AstralDraw) : OriginalHook(AST.UmbralDraw);

        return actionID;
    }
}

