using Dalamud.Game.ClientState.JobGauge.Types;

namespace XIVCombo.Combos;

internal static class BLM
{
    public const byte ClassID = 7;
    public const byte JobID = 25;

    public const uint
        Fire = 141,
        Blizzard = 142,
        Thunder = 144,
        Fire2 = 147,
        Transpose = 149,
        Fire3 = 152,
        Thunder3 = 153,
        Blizzard3 = 154,
        Scathe = 156,
        Freeze = 159,
        Flare = 162,
        LeyLines = 3573,
        Sharpcast = 3574,
        Triplecast = 7421,
        Blizzard4 = 3576,
        Fire4 = 3577,
        BetweenTheLines = 7419,
        Thunder4 = 7420,
        Despair = 16505,
        UmbralSoul = 16506,
        Xenoglossy = 16507,
        Blizzard2 = 25793,
        HighFire2 = 25794,
        HighBlizzard2 = 25795,
        Paradox = 25797,
        Retrace = 36988,
        FlareStar = 36989;

    public static class Buffs
    {
        public const ushort
            Thundercloud = 164,
            Firestarter = 165,
            Swiftcast = 167,
            LeyLines = 737,
            Sharpcast = 867,
            Triplecast = 1211,
            EnhancedFlare = 2960;
    }

    public static class Debuffs
    {
        public const ushort
            Thunder = 161,
            Thunder3 = 163;
    }

    public static class Levels
    {
        public const byte
            Fire3 = 35,
            Blizzard3 = 35,
            Freeze = 40,
            Thunder3 = 45,
            Flare = 50,
            Sharpcast = 54,
            Blizzard4 = 58,
            Fire4 = 60,
            BetweenTheLines = 62,
            Despair = 72,
            UmbralSoul = 35,
            Xenoglossy = 80,
            HighFire2 = 82,
            HighBlizzard2 = 82,
            EnhancedSharpcast2 = 88,
            Paradox = 90,
            Retrace = 96,
            FlareStar = 100;
    }

    public static class MpCosts
    {
        public const ushort
            Fire2 = 3000,
            Flare = 800;
    }
}

internal class BlackFireBlizzard4 : CustomCombo
{
    protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.BlackEnochianFeature;

    protected override uint Invoke(uint actionID, uint lastComboMove, float comboTime, byte level)
    {
        switch (actionID)
        {
            case BLM.Fire:
            case BLM.Blizzard:
                {
                    var gauge = GetJobGauge<BLMGauge>();
                    if (gauge.InAstralFire)
                        return OriginalHook(BLM.Fire);

                    return OriginalHook(BLM.Blizzard);
                }

            case BLM.Fire4:
            case BLM.Blizzard4:
                {
                    var gauge = GetJobGauge<BLMGauge>();
                    if (gauge.InAstralFire && level >= BLM.Levels.Fire4)
                    {
                        if (gauge.AstralSoulStacks == 6)
                            return BLM.FlareStar;

                        return BLM.Fire4;
                    }

                    return BLM.Blizzard4;
                }

            case BLM.Flare:
            case BLM.Freeze:
                {
                    var gauge = GetJobGauge<BLMGauge>();

                    if (level >= BLM.Levels.Freeze && gauge.InUmbralIce)
                        return BLM.Freeze;

                    if (gauge.InAstralFire && level >= BLM.Levels.Flare)
                    {
                        if (gauge.AstralSoulStacks == 6)
                            return BLM.FlareStar;

                        return BLM.Flare;
                    }
                }
                break;
        }

        return actionID;
    }
}
