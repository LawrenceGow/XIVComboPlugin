using System;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Hooking;
using XIVComboPlugin.JobActions;
using Dalamud.Game.ClientState.JobGauge.Enums;
using Dalamud.Game.ClientState.JobGauge.Types;
using FFXIVClientStructs.FFXIV.Client.Game;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Gauge;
using SerpentCombo = Dalamud.Game.ClientState.JobGauge.Enums.SerpentCombo;

namespace XIVComboPlugin
{
    public class IconReplacer
    {
        public delegate ulong OnCheckIsIconReplaceableDelegate(uint actionID);

        public delegate ulong OnGetIconDelegate(byte param1, uint param2);

        private readonly IconReplacerAddressResolver Address;
        private readonly Hook<OnCheckIsIconReplaceableDelegate> checkerHook;
        private readonly IClientState clientState;

        private IntPtr comboTimer = IntPtr.Zero;
        private IntPtr lastComboMove = IntPtr.Zero;

        private readonly XIVComboConfiguration Configuration;

        private readonly Hook<OnGetIconDelegate> iconHook;

        private IGameInteropProvider HookProvider;
        private IJobGauges JobGauges;
        private IPluginLog PluginLog;

        private unsafe delegate int* getArray(long* address);

        public IconReplacer(ISigScanner scanner, IClientState clientState, IDataManager manager, XIVComboConfiguration configuration, IGameInteropProvider hookProvider, IJobGauges jobGauges, IPluginLog pluginLog)
        {
            HookProvider = hookProvider;
            Configuration = configuration;
            this.clientState = clientState;
            JobGauges = jobGauges;
            PluginLog = pluginLog;

            Address = new IconReplacerAddressResolver(scanner);

            if (!clientState.IsLoggedIn)
                clientState.Login += SetupComboData;
            else
                SetupComboData();

            PluginLog.Verbose("===== X I V C O M B O =====");
            PluginLog.Verbose("IsIconReplaceable address {IsIconReplaceable}", Address.IsIconReplaceable);
            PluginLog.Verbose("ComboTimer address {ComboTimer}", comboTimer);
            PluginLog.Verbose("LastComboMove address {LastComboMove}", lastComboMove);

            iconHook = HookProvider.HookFromAddress<OnGetIconDelegate>((nint)ActionManager.Addresses.GetAdjustedActionId.Value, GetIconDetour);
            checkerHook = HookProvider.HookFromAddress<OnCheckIsIconReplaceableDelegate>(Address.IsIconReplaceable, CheckIsIconReplaceableDetour);
            HookProvider = hookProvider;
        }

        public unsafe void SetupComboData()
        {
            var actionmanager = (byte*)ActionManager.Instance();
            comboTimer = (IntPtr)(actionmanager + 0x60);
            lastComboMove = comboTimer + 0x4;
        }

        public void Enable()
        {
            iconHook.Enable();
            checkerHook.Enable();
        }

        public void Dispose()
        {
            iconHook.Dispose();
            checkerHook.Dispose();
        }

        // I hate this function. This is the dumbest function to exist in the game. Just return 1.
        // Determines which abilities are allowed to have their icons updated.

        private ulong CheckIsIconReplaceableDetour(uint actionID)
        {
            return 1;
        }

        /// <summary>
        ///     Replace an ability with another ability
        ///     actionID is the original ability to be "used"
        ///     Return either actionID (itself) or a new Action table ID as the
        ///     ability to take its place.
        ///     I tend to make the "combo chain" button be the last move in the combo
        ///     For example, Souleater combo on DRK happens by dragging Souleater
        ///     onto your bar and mashing it.
        /// </summary>
        private ulong GetIconDetour(byte self, uint actionID)
        {
            if (clientState.LocalPlayer == null) return iconHook.Original(self, actionID);
            // Last resort. For some reason GetIcon fires after leaving the lobby but before ClientState.Login
            if (lastComboMove == IntPtr.Zero)
            {
                SetupComboData();
                return iconHook.Original(self, actionID);
            }
            if (comboTimer == IntPtr.Zero)
            {
                SetupComboData();
                return iconHook.Original(self, actionID);
            }

            uint lastMove = (uint)Marshal.ReadInt32(lastComboMove);
            var comboTime = Marshal.PtrToStructure<float>(comboTimer);
            var level = clientState.LocalPlayer.Level;

            switch (actionID)
            {
                #region DRAGOON

                // Replace Coerthan Torment with Coerthan Torment combo chain
                case DRG.CTorment when Configuration.ComboPresets.HasFlag(CustomComboPreset.DragoonCoerthanTormentCombo):
                    if ((lastMove == DRG.DoomSpike || lastMove == DRG.DraconianFury) && level >= 62)
                        return DRG.SonicThrust;
                    if (lastMove == DRG.SonicThrust && level >= 72)
                        return DRG.CTorment;
                    return iconHook.Original(self, DRG.DoomSpike);

                // Replace Chaos Thrust with the Chaos Thrust combo chain
                case DRG.ChaosThrust when Configuration.ComboPresets.HasFlag(CustomComboPreset.DragoonChaosThrustCombo):
                case DRG.ChaoticSpring when Configuration.ComboPresets.HasFlag(CustomComboPreset.DragoonChaosThrustCombo):
                    if ((lastMove == DRG.TrueThrust || lastMove == DRG.RaidenThrust) && level >= 18)
                        return iconHook.Original(self, DRG.Disembowel);
                    if ((lastMove == DRG.Disembowel || lastMove == DRG.SpiralBlow) && level >= 50)
                        return iconHook.Original(self, DRG.ChaosThrust);
                    if ((lastMove == DRG.ChaosThrust || lastMove == DRG.ChaoticSpring) && level >= 58)
                        return DRG.WheelingThrust;
                    if (lastMove == DRG.WheelingThrust && level >= 64)
                        return DRG.Drakesbane;
                    return iconHook.Original(self, DRG.TrueThrust);

                // Replace Full Thrust with the Full Thrust combo chain
                case DRG.FullThrust when Configuration.ComboPresets.HasFlag(CustomComboPreset.DragoonFullThrustCombo):
                case DRG.HeavensThrust when Configuration.ComboPresets.HasFlag(CustomComboPreset.DragoonFullThrustCombo):
                    if ((lastMove == DRG.TrueThrust || lastMove == DRG.RaidenThrust) && level >= 4)
                        return iconHook.Original(self, DRG.VorpalThrust);
                    if ((lastMove == DRG.VorpalThrust || lastMove == DRG.LanceBarrage) && level >= 26)
                        return iconHook.Original(self, DRG.FullThrust);
                    if ((lastMove == DRG.FullThrust || lastMove == DRG.HeavensThrust) && level >= 56)
                        return DRG.FangAndClaw;
                    if (lastMove == DRG.FangAndClaw && level >= 64)
                        return DRG.Drakesbane;
                    return iconHook.Original(self, DRG.TrueThrust);

                #endregion

                #region DARK KNIGHT

                // Replace Souleater with Souleater combo chain
                case DRK.Souleater when Configuration.ComboPresets.HasFlag(CustomComboPreset.DarkSouleaterCombo):
                    if (lastMove == DRK.HardSlash && level >= 2)
                        return DRK.SyphonStrike;
                    if (lastMove == DRK.SyphonStrike && level >= 26)
                        return DRK.Souleater;
                    return DRK.HardSlash;

                // Replace Stalwart Soul with Stalwart Soul combo chain
                case DRK.StalwartSoul when Configuration.ComboPresets.HasFlag(CustomComboPreset.DarkStalwartSoulCombo):
                    if (lastMove == DRK.Unleash && level >= 40)
                        return DRK.StalwartSoul;
                    return DRK.Unleash;

                #endregion

                #region PALADIN

                // Replace Royal Authority with Royal Authority combo
                case PLD.RoyalAuthority when Configuration.ComboPresets.HasFlag(CustomComboPreset.PaladinRoyalAuthorityCombo):
                case PLD.RageOfHalone when Configuration.ComboPresets.HasFlag(CustomComboPreset.PaladinRoyalAuthorityCombo):
                    if (lastMove == PLD.FastBlade && level >= 4)
                        return PLD.RiotBlade;
                    if (lastMove == PLD.RiotBlade && level >= 26)
                        return iconHook.Original(self, PLD.RageOfHalone);
                    return PLD.FastBlade;

                // Replace Prominence with Prominence combo
                case PLD.Prominence when Configuration.ComboPresets.HasFlag(CustomComboPreset.PaladinProminenceCombo):
                    if (lastMove == PLD.TotalEclipse && level >= 40)
                        return PLD.Prominence;
                    return PLD.TotalEclipse;

                // Replace Requiescat/Imperator with Confiteor when under the effect of Requiescat
                case PLD.Requiescat when Configuration.ComboPresets.HasFlag(CustomComboPreset.PaladinRequiescatCombo):
                case PLD.Imperator when Configuration.ComboPresets.HasFlag(CustomComboPreset.PaladinRequiescatCombo):
                    if (SearchBuffArray(PLD.BuffRequiescat) && level >= 80)
                        return iconHook.Original(self, PLD.Confiteor);
                    return iconHook.Original(self, actionID);

                #endregion

                #region WARRIOR

                // Replace Storm's Path with Storm's Path combo
                case WAR.StormsPath when Configuration.ComboPresets.HasFlag(CustomComboPreset.WarriorStormsPathCombo):
                    if (lastMove == WAR.HeavySwing && level >= 4)
                        return WAR.Maim;
                    if (lastMove == WAR.Maim && level >= 26)
                        return WAR.StormsPath;
                    return WAR.HeavySwing;

                // Replace Storm's Eye with Storm's Eye combo
                case WAR.StormsEye when Configuration.ComboPresets.HasFlag(CustomComboPreset.WarriorStormsEyeCombo):
                    if (lastMove == WAR.HeavySwing && level >= 4)
                        return WAR.Maim;
                    if (lastMove == WAR.Maim && level >= 50)
                        return WAR.StormsEye;
                    return WAR.HeavySwing;

                // Replace Mythril Tempest with Mythril Tempest combo
                case WAR.MythrilTempest when Configuration.ComboPresets.HasFlag(CustomComboPreset.WarriorMythrilTempestCombo):
                    if (lastMove == WAR.Overpower && level >= 40)
                        return WAR.MythrilTempest;
                    return WAR.Overpower;

                #endregion

                #region SAMURAI

                // Replace Iaijutsu with Tsubame combo
                case SAM.Iaijutsu when Configuration.ComboPresets.HasFlag(CustomComboPreset.SamuraiTsubameCombo):
                    if (SearchBuffArray(SAM.BuffTsubameReady) ||
                        SearchBuffArray(SAM.BuffTsubame1) ||
                        SearchBuffArray(SAM.BuffTsubame2) ||
                        SearchBuffArray(SAM.BuffTsubame3))
                        return iconHook.Original(self, SAM.Tsubame);
                    return iconHook.Original(self, actionID);

                // Replace Yukikaze with Yukikaze combo
                case SAM.Yukikaze when Configuration.ComboPresets.HasFlag(CustomComboPreset.SamuraiYukikazeCombo):
                    if (SearchBuffArray(SAM.BuffMeikyoShisui))
                        return SAM.Yukikaze;
                    if ((lastMove == SAM.Hakaze || lastMove == SAM.Gyofu) && level >= 50)
                        return SAM.Yukikaze;
                    return iconHook.Original(self, SAM.Hakaze);

                // Replace Gekko with Gekko combo
                case SAM.Gekko when Configuration.ComboPresets.HasFlag(CustomComboPreset.SamuraiGekkoCombo):
                    if (SearchBuffArray(SAM.BuffMeikyoShisui))
                        return SAM.Gekko;
                    if ((lastMove == SAM.Hakaze || lastMove == SAM.Gyofu) && level >= 4)
                        return SAM.Jinpu;
                    if (lastMove == SAM.Jinpu && level >= 30)
                        return SAM.Gekko;
                    return iconHook.Original(self, SAM.Hakaze);

                // Replace Kasha with Kasha combo
                case SAM.Kasha when Configuration.ComboPresets.HasFlag(CustomComboPreset.SamuraiKashaCombo):
                    if (SearchBuffArray(SAM.BuffMeikyoShisui))
                        return SAM.Kasha;
                    if ((lastMove == SAM.Hakaze || lastMove == SAM.Gyofu) && level >= 18)
                        return SAM.Shifu;
                    if (lastMove == SAM.Shifu && level >= 40)
                        return SAM.Kasha;
                    return iconHook.Original(self, SAM.Hakaze);

                // Replace Mangetsu with Mangetsu combo
                case SAM.Mangetsu when Configuration.ComboPresets.HasFlag(CustomComboPreset.SamuraiMangetsuCombo):
                    if (SearchBuffArray(SAM.BuffMeikyoShisui))
                        return SAM.Mangetsu;
                    if ((lastMove == SAM.Fuga || lastMove == SAM.Fuko) && level >= 35)
                        return SAM.Mangetsu;
                    return iconHook.Original(self, SAM.Fuga);

                // Replace Oka with Oka combo
                case SAM.Oka when Configuration.ComboPresets.HasFlag(CustomComboPreset.SamuraiOkaCombo):
                    if (SearchBuffArray(SAM.BuffMeikyoShisui))
                        return SAM.Oka;
                    if ((lastMove == SAM.Fuga || lastMove == SAM.Fuko) && level >= 45)
                        return SAM.Oka;
                    return iconHook.Original(self, SAM.Fuga);

                #endregion

                #region NINJA

                // Replace Armor Crush with Armor Crush combo
                case NIN.ArmorCrush when Configuration.ComboPresets.HasFlag(CustomComboPreset.NinjaArmorCrushCombo):
                    if (lastMove == NIN.SpinningEdge && level >= 4)
                        return NIN.GustSlash;
                    if (lastMove == NIN.GustSlash && level >= 54)
                        return NIN.ArmorCrush;
                    return NIN.SpinningEdge;

                // Replace Aeolian Edge with Aeolian Edge combo
                case NIN.AeolianEdge when Configuration.ComboPresets.HasFlag(CustomComboPreset.NinjaAeolianEdgeCombo):
                    if (lastMove == NIN.SpinningEdge && level >= 4)
                        return NIN.GustSlash;
                    if (lastMove == NIN.GustSlash && level >= 26)
                        return NIN.AeolianEdge;
                    return NIN.SpinningEdge;

                // Replace Hakke Mujinsatsu with Hakke Mujinsatsu combo
                case NIN.HakkeM when Configuration.ComboPresets.HasFlag(CustomComboPreset.NinjaHakkeMujinsatsuCombo):
                    if (lastMove == NIN.DeathBlossom && level >= 52)
                        return NIN.HakkeM;
                    return NIN.DeathBlossom;

                #endregion

                #region GUNBREAKER

                // Replace Solid Barrel with Solid Barrel combo
                case GNB.SolidBarrel when Configuration.ComboPresets.HasFlag(CustomComboPreset.GunbreakerSolidBarrelCombo):
                    if (lastMove == GNB.KeenEdge && level >= 4)
                        return GNB.BrutalShell;
                    if (lastMove == GNB.BrutalShell && level >= 26)
                        return GNB.SolidBarrel;
                    return GNB.KeenEdge;

                // Replace Wicked Talon with Gnashing Fang combo
                case GNB.GnashingFang when Configuration.ComboPresets.HasFlag(CustomComboPreset.GunbreakerGnashingFangCont):
                    if (level >= GNB.LevelContinuation)
                    {
                        if (SearchBuffArray(GNB.BuffReadyToRip))
                            return GNB.JugularRip;
                        if (SearchBuffArray(GNB.BuffReadyToTear))
                            return GNB.AbdomenTear;
                        if (SearchBuffArray(GNB.BuffReadyToGouge))
                            return GNB.EyeGouge;
                    }
                    return iconHook.Original(self, GNB.GnashingFang);

                // Replace Burst Strike with Continuation
                case GNB.BurstStrike when Configuration.ComboPresets.HasFlag(CustomComboPreset.GunbreakerBurstStrikeCont):
                    if (level >= GNB.LevelEnhancedContinuation)
                        if (SearchBuffArray(GNB.BuffReadyToBlast))
                            return GNB.Hypervelocity;
                    return GNB.BurstStrike;

                // Replace Demon Slaughter with Demon Slaughter combo
                case GNB.DemonSlaughter when Configuration.ComboPresets.HasFlag(CustomComboPreset.GunbreakerDemonSlaughterCombo):
                    if (lastMove == GNB.DemonSlice && level >= 40)
                        return GNB.DemonSlaughter;
                    return GNB.DemonSlice;

                // Replace Fated Brand with Continuation
                case GNB.FatedCircle when Configuration.ComboPresets.HasFlag(CustomComboPreset.GunbreakerFatedCircleCont):
                    if (level >= GNB.LevelEnhancedContinuation2)
                        if (SearchBuffArray(GNB.BuffReadyToRaze))
                            return GNB.FatedBrand;
                    return GNB.FatedCircle;

                #endregion

                #region MACHINIST

                // Replace Clean Shot with Heated Clean Shot combo
                // Or with Heat Blast when overheated.
                case MCH.CleanShot when Configuration.ComboPresets.HasFlag(CustomComboPreset.MachinistMainCombo):
                case MCH.HeatedCleanShot when Configuration.ComboPresets.HasFlag(CustomComboPreset.MachinistMainCombo):
                    if (lastMove == MCH.SplitShot && level >= 2)
                        return iconHook.Original(self, MCH.SlugShot);
                    if (lastMove == MCH.SlugShot && level >= 26)
                        return iconHook.Original(self, MCH.CleanShot);
                    return iconHook.Original(self, MCH.SplitShot);

                // Replace Hypercharge with Heat Blast when overheated
                case MCH.Hypercharge when Configuration.ComboPresets.HasFlag(CustomComboPreset.MachinistOverheatFeature):
                    if (JobGauges.Get<MCHGauge>().IsOverheated)
                        if (level >= 35) return iconHook.Original(self, MCH.HeatBlast);
                    return MCH.Hypercharge;

                // Replace Spread Shot with Auto Crossbow when overheated.
                case MCH.SpreadShot when Configuration.ComboPresets.HasFlag(CustomComboPreset.MachinistSpreadShotFeature):
                case MCH.Scattergun when Configuration.ComboPresets.HasFlag(CustomComboPreset.MachinistSpreadShotFeature):
                    if (JobGauges.Get<MCHGauge>().IsOverheated && level >= 52)
                        return MCH.AutoCrossbow;
                    return iconHook.Original(self, MCH.SpreadShot);

                #endregion

                #region BLACK MAGE

                case BLM.Fire4 when Configuration.ComboPresets.HasFlag(CustomComboPreset.BlackEnochianFeature):
                case BLM.Blizzard4 when Configuration.ComboPresets.HasFlag(CustomComboPreset.BlackEnochianFeature):
                    if (JobGauges.Get<BLMGauge>().InUmbralIce && level >= 58)
                        return BLM.Blizzard4;
                    if (level >= 60)
                        return BLM.Fire4;
                    return iconHook.Original(self, actionID);
                case BLM.Flare when Configuration.ComboPresets.HasFlag(CustomComboPreset.BlackEnochianFeature):
                case BLM.Freeze when Configuration.ComboPresets.HasFlag(CustomComboPreset.BlackEnochianFeature):
                    if (JobGauges.Get<BLMGauge>().InAstralFire && level >= 50)
                        return BLM.Flare;
                    return BLM.Freeze;

                case BLM.UmbralSoul:
                    if (JobGauges.Get<BLMGauge>().InAstralFire || level < 35)
                        return BLM.Transpose;
                    return BLM.UmbralSoul;

                #endregion

                #region ASTROLOGIAN

                // Change Play 1/2/3 to Astral/Umbral Draw if that Play action doesn't have a card ready to be played.
                case AST.Play1 when Configuration.ComboPresets.HasFlag(CustomComboPreset.AstrologianCardsOnDrawFeature):
                case AST.Play2 when Configuration.ComboPresets.HasFlag(CustomComboPreset.AstrologianCardsOnDrawFeature):
                case AST.Play3 when Configuration.ComboPresets.HasFlag(CustomComboPreset.AstrologianCardsOnDrawFeature):
                    var x = iconHook.Original(self, actionID);
                    if (x != AST.Play1 && x != AST.Play2 && x != AST.Play3)
                        return x;
                    return iconHook.Original(self, AST.AstralDraw);

                #endregion

                #region SUMMONER

                // Change Fester/Necrotize into Energy Drain
                case SMN.Fester when Configuration.ComboPresets.HasFlag(CustomComboPreset.SummonerEDFesterCombo):
                case SMN.Necrotize when Configuration.ComboPresets.HasFlag(CustomComboPreset.SummonerEDFesterCombo):
                    if (!JobGauges.Get<SMNGauge>().HasAetherflowStacks)
                        return SMN.EnergyDrain;
                    return iconHook.Original(self, SMN.Fester);

                //Change Painflare into Energy Syphon
                case SMN.Painflare when Configuration.ComboPresets.HasFlag(CustomComboPreset.SummonerESPainflareCombo):
                    if (!JobGauges.Get<SMNGauge>().HasAetherflowStacks)
                        return SMN.EnergySyphon;
                    return SMN.Painflare;

                //Change Summon Solar Bahamut into Lux Solaris
                case SMN.SummonBahamut when Configuration.ComboPresets.HasFlag(CustomComboPreset.SummonerSolarBahamutLuxSolaris):
                    if (SearchBuffArray(SMN.Buffs.RefulgentLux))
                        return SMN.LuxSolaris;
                    return iconHook.Original(self, actionID);

                #endregion

                #region SCHOLAR

                // Change Energy Drain into Aetherflow when you have no more Aetherflow stacks.
                case SCH.EnergyDrain when Configuration.ComboPresets.HasFlag(CustomComboPreset.ScholarEnergyDrainFeature):
                    if (JobGauges.Get<SCHGauge>().Aetherflow == 0) return SCH.Aetherflow;
                    return SCH.EnergyDrain;

                #endregion

                #region DANCER

                // AoE GCDs are split into two buttons, because priority matters
                // differently in different single-target moments. Thanks yoship.
                // Replaces each GCD with its procced version.
                case DNC.Bloodshower when Configuration.ComboPresets.HasFlag(CustomComboPreset.DancerAoeGcdFeature):
                    if (SearchBuffArray(DNC.BuffFlourishingFlow) || SearchBuffArray(DNC.BuffSilkenFlow))
                        return DNC.Bloodshower;
                    return DNC.Bladeshower;
                case DNC.RisingWindmill when Configuration.ComboPresets.HasFlag(CustomComboPreset.DancerAoeGcdFeature):
                    if (SearchBuffArray(DNC.BuffFlourishingSymmetry) || SearchBuffArray(DNC.BuffSilkenSymmetry))
                        return DNC.RisingWindmill;
                    return DNC.Windmill;

                // Fan Dance changes into Fan Dance 3 while flourishing.
                case DNC.FanDance1 when Configuration.ComboPresets.HasFlag(CustomComboPreset.DancerFanDanceCombo):
                    if (SearchBuffArray(DNC.BuffThreefoldFanDance))
                        return DNC.FanDance3;
                    return DNC.FanDance1;
                // Fan Dance 2 changes into Fan Dance 3 while flourishing.
                case DNC.FanDance2 when Configuration.ComboPresets.HasFlag(CustomComboPreset.DancerFanDanceCombo):
                    if (SearchBuffArray(DNC.BuffThreefoldFanDance))
                        return DNC.FanDance3;
                    return DNC.FanDance2;

                case DNC.Flourish when Configuration.ComboPresets.HasFlag(CustomComboPreset.DancerFanDance4Combo):
                    if (SearchBuffArray(DNC.BuffFourfoldFanDance))
                        return DNC.FanDance4;
                    return DNC.Flourish;

                case DNC.Devilment when Configuration.ComboPresets.HasFlag(CustomComboPreset.DancerDevilmentCombo):
                    if (SearchBuffArray(DNC.BuffStarfallDanceReady))
                        return DNC.StarfallDance;
                    return DNC.Devilment;

                case DNC.StandardStep when Configuration.ComboPresets.HasFlag(CustomComboPreset.DancerLastDanceCombo):
                    if (SearchBuffArray(DNC.BuffLastDance))
                        return DNC.LastDance;
                    return iconHook.Original(self, actionID);

                #endregion

                #region WHITE MAGE

                // Replace Solace with Misery when full blood lily
                case WHM.Solace when Configuration.ComboPresets.HasFlag(CustomComboPreset.WhiteMageSolaceMiseryFeature):
                    if (JobGauges.Get<WHMGauge>().BloodLily == 3)
                        return WHM.Misery;
                    return WHM.Solace;

                // Replace Solace with Misery when full blood lily
                case WHM.Rapture when Configuration.ComboPresets.HasFlag(CustomComboPreset.WhiteMageRaptureMiseryFeature):
                    if (JobGauges.Get<WHMGauge>().BloodLily == 3)
                        return WHM.Misery;
                    return WHM.Rapture;

                #endregion

                #region BARD

                // Replace HS/BS with SS/RA when procced.
                case BRD.HeavyShot when Configuration.ComboPresets.HasFlag(CustomComboPreset.BardStraightShotUpgradeFeature):
                case BRD.BurstShot when Configuration.ComboPresets.HasFlag(CustomComboPreset.BardStraightShotUpgradeFeature):
                    if (SearchBuffArray(BRD.BuffHawksEye) || SearchBuffArray(BRD.BuffBarrage))
                        return iconHook.Original(self, BRD.StraightShot);
                    return iconHook.Original(self, BRD.HeavyShot);

                case BRD.QuickNock when Configuration.ComboPresets.HasFlag(CustomComboPreset.BardAoEUpgradeFeature):
                case BRD.Ladonsbite when Configuration.ComboPresets.HasFlag(CustomComboPreset.BardAoEUpgradeFeature):
                    if (SearchBuffArray(BRD.BuffHawksEye) || SearchBuffArray(BRD.BuffBarrage))
                        return iconHook.Original(self, BRD.WideVolley);
                    return iconHook.Original(self, BRD.QuickNock);

                #endregion

                #region MONK

                case MNK.Bootshine when Configuration.ComboPresets.HasFlag(CustomComboPreset.MonkFuryCombo):
                case MNK.LeapingOpo when Configuration.ComboPresets.HasFlag(CustomComboPreset.MonkFuryCombo):
                    if (JobGauges.Get<MNKGauge>().OpoOpoFury < 1 && level >= 50)
                        return MNK.DragonKick;
                    return iconHook.Original(self, actionID);
                case MNK.TrueStrike when Configuration.ComboPresets.HasFlag(CustomComboPreset.MonkFuryCombo):
                case MNK.RisingRaptor when Configuration.ComboPresets.HasFlag(CustomComboPreset.MonkFuryCombo):
                    if (JobGauges.Get<MNKGauge>().RaptorFury < 1 && level >= 18)
                        return MNK.TwinSnakes;
                    return iconHook.Original(self, actionID);
                case MNK.SnapPunch when Configuration.ComboPresets.HasFlag(CustomComboPreset.MonkFuryCombo):
                case MNK.PouncingCoeurl when Configuration.ComboPresets.HasFlag(CustomComboPreset.MonkFuryCombo):
                    if (JobGauges.Get<MNKGauge>().CoeurlFury < 1 && level >= 30)
                        return MNK.Demolish;
                    return iconHook.Original(self, actionID);

                case MNK.MasterfulBlitz when Configuration.ComboPresets.HasFlag(CustomComboPreset.MonkPerfectBlitz):
                    if (JobGauges.Get<MNKGauge>().BlitzTimeRemaining <= 0 || level < 60)
                        return MNK.PerfectBalance;
                    return iconHook.Original(self, actionID);

                #endregion

                #region RED MAGE

                // Replace Veraero/thunder 2 with Impact when Dualcast is active
                case RDM.Veraero2 when Configuration.ComboPresets.HasFlag(CustomComboPreset.RedMageAoECombo):
                    if (SearchBuffArray(RDM.BuffSwiftcast) || SearchBuffArray(RDM.BuffDualcast) ||
                        SearchBuffArray(RDM.BuffAcceleration) || SearchBuffArray(RDM.BuffChainspell))
                        return iconHook.Original(self, RDM.Scatter);
                    return iconHook.Original(self, actionID);
                case RDM.Verthunder2 when Configuration.ComboPresets.HasFlag(CustomComboPreset.RedMageAoECombo):
                    if (SearchBuffArray(RDM.BuffSwiftcast) || SearchBuffArray(RDM.BuffDualcast) ||
                        SearchBuffArray(RDM.BuffAcceleration) || SearchBuffArray(RDM.BuffChainspell))
                        return iconHook.Original(self, RDM.Scatter);
                    return iconHook.Original(self, actionID);

                // Replace Redoublement with Redoublement combo, Enchanted if possible.
                case RDM.Redoublement when Configuration.ComboPresets.HasFlag(CustomComboPreset.RedMageMeleeCombo):
                    if ((lastMove == RDM.Riposte) && level >= 35)
                        return iconHook.Original(self, RDM.Zwerchhau);
                    if (lastMove == RDM.Zwerchhau && level >= 50)
                        return iconHook.Original(self, RDM.Redoublement);
                    return iconHook.Original(self, RDM.Riposte);

                case RDM.Verstone when Configuration.ComboPresets.HasFlag(CustomComboPreset.RedMageVerprocCombo):
                    if (level >= 80 && (lastMove == RDM.Verflare || lastMove == RDM.Verholy))
                        return RDM.Scorch;
                    if (level >= 90 && lastMove == RDM.Scorch)
                        return RDM.Resolution;
                    if (SearchBuffArray(RDM.BuffVerstoneReady))
                        return RDM.Verstone;
                    return iconHook.Original(self, RDM.Jolt);
                case RDM.Verfire when Configuration.ComboPresets.HasFlag(CustomComboPreset.RedMageVerprocCombo):
                    if (level >= 80 && (lastMove == RDM.Verflare || lastMove == RDM.Verholy))
                        return RDM.Scorch;
                    if (level >= 90 && lastMove == RDM.Scorch)
                        return RDM.Resolution;
                    if (SearchBuffArray(RDM.BuffVerfireReady))
                        return RDM.Verfire;
                    return iconHook.Original(self, RDM.Jolt);

                #endregion

                #region REAPER

                case RPR.Slice when Configuration.ComboPresets.HasFlag(CustomComboPreset.ReaperSliceCombo):
                    if (lastMove == RPR.Slice && level >= RPR.Levels.WaxingSlice)
                        return RPR.WaxingSlice;
                    if (lastMove == RPR.WaxingSlice && level >= RPR.Levels.InfernalSlice)
                        return RPR.InfernalSlice;
                    return RPR.Slice;

                case RPR.SpinningScythe when Configuration.ComboPresets.HasFlag(CustomComboPreset.ReaperScytheCombo):
                    if (lastMove == RPR.SpinningScythe && level >= RPR.Levels.NightmareScythe)
                        return RPR.NightmareScythe;
                    return RPR.SpinningScythe;

                case RPR.Egress when Configuration.ComboPresets.HasFlag(CustomComboPreset.ReaperRegressFeature):
                case RPR.Ingress when Configuration.ComboPresets.HasFlag(CustomComboPreset.ReaperRegressFeature):
                    if (SearchBuffArray(RPR.Buffs.Threshold))
                        return RPR.Regress;
                    return actionID;

                case RPR.Enshroud when Configuration.ComboPresets.HasFlag(CustomComboPreset.ReaperEnshroudCombo):
                    if (SearchBuffArray(RPR.Buffs.Enshrouded))
                        return RPR.Communio;
                    if (SearchBuffArray(RPR.Buffs.PerfectioParata))
                        return RPR.Perfectio;
                    return actionID;

                case RPR.ArcaneCircle when Configuration.ComboPresets.HasFlag(CustomComboPreset.ReaperArcaneFeature):
                    if (SearchBuffArray(RPR.Buffs.ImSac1) || SearchBuffArray(RPR.Buffs.ImSac2))
                        return RPR.PlentifulHarvest;
                    return actionID;

                #endregion

                #region PICTOMANCER

                case PCT.Fire1 when Configuration.ComboPresets.HasFlag(CustomComboPreset.PictoSubtractivePallet):
                    if (SearchBuffArray(PCT.SubPallet))
                        return iconHook.Original(self, PCT.Bliz1);
                    return iconHook.Original(self, PCT.Fire1);
                case PCT.Fire2 when Configuration.ComboPresets.HasFlag(CustomComboPreset.PictoSubtractivePallet):
                    if (SearchBuffArray(PCT.SubPallet))
                        return iconHook.Original(self, PCT.Bliz2);
                    return iconHook.Original(self, PCT.Fire2);

                case PCT.HolyWhite when Configuration.ComboPresets.HasFlag(CustomComboPreset.PictoHolyWhiteCombo):
                    if (SearchBuffArray(PCT.Monochrome))
                        return PCT.CometBlack;
                    return PCT.HolyWhite;

                case PCT.CreatureMotif when Configuration.ComboPresets.HasFlag(CustomComboPreset.PictoMotifMuseFeature)
                                            && JobGauges.Get<PCTGauge>().CreatureMotifDrawn:
                    return iconHook.Original(self, PCT.LivingMuse);

                case PCT.WeaponMotif when Configuration.ComboPresets.HasFlag(CustomComboPreset.PictoMotifMuseFeature)
                                          && JobGauges.Get<PCTGauge>().WeaponMotifDrawn:
                    return iconHook.Original(self, PCT.SteelMuse);

                case PCT.WeaponMotif when Configuration.ComboPresets.HasFlag(CustomComboPreset.PictoMuseCombo)
                                          && SearchBuffArray(PCT.HammerReady):
                    return iconHook.Original(self, PCT.HammerStamp);

                case PCT.LandscapeMotif when Configuration.ComboPresets.HasFlag(CustomComboPreset.PictoMotifMuseFeature)
                                             && JobGauges.Get<PCTGauge>().LandscapeMotifDrawn:
                    return PCT.StarryMuse;

                case PCT.LandscapeMotif when Configuration.ComboPresets.HasFlag(CustomComboPreset.PictoMuseCombo)
                                             && SearchBuffArray(PCT.StarStruck):
                    return PCT.StarPrism;

                #endregion

                #region VIPER

                case VPR.SteelFangs when Configuration.ComboPresets.HasFlag(CustomComboPreset.ViperDeathRattleCombo)
                                         && JobGauges.Get<VPRGauge>().SerpentCombo == SerpentCombo.DEATHRATTLE:
                case VPR.DreadFangs when Configuration.ComboPresets.HasFlag(CustomComboPreset.ViperDeathRattleCombo)
                                         && JobGauges.Get<VPRGauge>().SerpentCombo == SerpentCombo.DEATHRATTLE:
                    return VPR.DeathRattle;

                case VPR.DreadMaw when Configuration.ComboPresets.HasFlag(CustomComboPreset.ViperLastLashCombo)
                                       && JobGauges.Get<VPRGauge>().SerpentCombo == SerpentCombo.LASTLASH:
                case VPR.SteelMaw when Configuration.ComboPresets.HasFlag(CustomComboPreset.ViperLastLashCombo)
                                       && JobGauges.Get<VPRGauge>().SerpentCombo == SerpentCombo.LASTLASH:
                    return VPR.LastLash;

                #endregion

                default:
                    return iconHook.Original(self, actionID);
            }

            //if (Configuration.ComboPresets.HasFlag(CustomComboPreset.ViperLegacyCombo))
            //{
            //    switch (actionID)
            //    {
            //        case VPR.SteelFangs:
            //        case VPR.SteelMaw:
            //            if (JobGauges.Get<VPRGauge>().SerpentCombo == SerpentCombo.FIRSTLEGACY)
            //                return VPR.FirstLegacy;
            //            break;

            //        case VPR.DreadFangs:
            //        case VPR.DreadMaw:
            //            if (JobGauges.Get<VPRGauge>().SerpentCombo == SerpentCombo.SECONDLEGACY)
            //                return VPR.SecondLegacy;
            //            break;

            //        case VPR.HuntersCoil:
            //        case VPR.HuntersDen:
            //            if (JobGauges.Get<VPRGauge>().SerpentCombo == SerpentCombo.THIRDLEGACY)
            //                return VPR.ThirdLegacy;
            //            break;

            //        case VPR.SwiftskinsCoil:
            //        case VPR.SwiftskinsDen:
            //            if (JobGauges.Get<VPRGauge>().SerpentCombo == SerpentCombo.FOURTHLEGACY)
            //                return VPR.FourthLegacy;
            //            break;
            //    }
            //}
        }

        private bool SearchBuffArray(ushort needle)
        {
            if (needle == 0) return false;
            var buffs = clientState.LocalPlayer.StatusList;
            for (var i = 0; i < buffs.Length; i++)
                if (buffs[i].StatusId == needle)
                    return true;
            return false;
        }
    }
}
