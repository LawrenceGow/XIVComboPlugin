using Dalamud.Game.ClientState.JobGauge.Types;

namespace XIVCombo.Combos;

internal static class WHM
{
    public const byte ClassID = 6;
    public const byte JobID = 24;

    public const uint
        Stone = 119,
        Stone2 = 127,
        Stone3 = 3568,
        Glare = 16533,
        Glare3 = 25859,
        Glare4 = 37009,
        Aero = 121,
        Cure = 120,
        Medica = 124,
        Raise = 125,
        Medica2 = 133,
        Cure2 = 135,
        PresenceOfMind = 136,
        Holy = 139,
        Benediction = 140,
        Asylum = 3569,
        Tetragrammaton = 3570,
        Assize = 3571,
        PlenaryIndulgence = 7433,
        AfflatusSolace = 16531,
        AfflatusRapture = 16534,
        AfflatusMisery = 16535,
        Temperance = 16536,
        Holy3 = 25860,
        Aquaveil = 25861,
        LiturgyOfTheBell = 25862,
        Medica3 = 37010;

    public static class Buffs
    {
        public const ushort

            Glare4Ready = 3879;
    }

    public static class Debuffs
    {
        public const ushort
            Aero = 143,
            Aero2 = 144,
            Dia = 1871;
    }

    public static class Levels
    {
        public const byte
            Raise = 12,
            Cure2 = 30,
            AfflatusSolace = 52,
            AfflatusMisery = 74,
            AfflatusRapture = 76,
            Glare4 = 92;
    }
}

internal class WhiteMageAfflatusSolace : CustomCombo
{
    protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.WhiteMageSolaceMiseryFeature;

    protected override uint Invoke(uint actionID, uint lastComboMove, float comboTime, byte level)
    {
        if (actionID == WHM.AfflatusSolace)
        {
            var gauge = GetJobGauge<WHMGauge>();

            if (level >= WHM.Levels.AfflatusMisery && gauge.BloodLily == 3)
            {
                if (TargetIsEnemy())
                    return WHM.AfflatusMisery;
            }
        }

        return actionID;
    }
}

internal class WhiteMageAfflatusRapture : CustomCombo
{
    protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.WhiteMageRaptureMiseryFeature;

    protected override uint Invoke(uint actionID, uint lastComboMove, float comboTime, byte level)
    {
        if (actionID == WHM.AfflatusRapture)
        {
            var gauge = GetJobGauge<WHMGauge>();

            if (level >= WHM.Levels.AfflatusMisery && gauge.BloodLily == 3 && TargetIsEnemy())
                return WHM.AfflatusMisery;
        }

        return actionID;
    }
}

internal class WhiteMageMedicaToRapture : CustomCombo
{
    protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.WhmAny;

    protected override uint Invoke(uint actionID, uint lastComboMove, float comboTime, byte level)
    {
        if (actionID == WHM.Medica)
        {
            var gauge = GetJobGauge<WHMGauge>();

            if (level >= WHM.Levels.AfflatusRapture && gauge.Lily > 0)
                return WHM.AfflatusRapture;
        }

        return actionID;
    }
}

internal class WhiteMageCure2ToSolus : CustomCombo
{
    protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.WhmAny;

    protected override uint Invoke(uint actionID, uint lastComboMove, float comboTime, byte level)
    {
        if (actionID == WHM.Cure2)
        {
            var gauge = GetJobGauge<WHMGauge>();

            if (level >= WHM.Levels.AfflatusSolace && gauge.Lily > 0)
                return WHM.AfflatusSolace;
        }

        return actionID;
    }
}

internal class WhiteMageGlareToMisery : CustomCombo
{
    protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.WhmAny;

    protected override uint Invoke(uint actionID, uint lastComboMove, float comboTime, byte level)
    {
        if (actionID == WHM.Glare || actionID == WHM.Glare3)
        {
            var gauge = GetJobGauge<WHMGauge>();

            if (level >= WHM.Levels.AfflatusMisery && gauge.BloodLily == 3)
                return WHM.AfflatusMisery;
        }

        return actionID;
    }
}
