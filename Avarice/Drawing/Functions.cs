﻿using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.GameFunctions;
using ECommons.MathHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using static Avarice.Drawing.DrawFunctions;
using static Avarice.Util;

namespace Avarice.Drawing;

internal unsafe static class Functions
{
    internal static void DrawFrontalPosition(GameObject go) 
    {
        if (go is BattleNpc bnpc && bnpc.IsHostile() &&
                (!P.currentProfile.FrontStand || GetDirection(bnpc) == CardinalDirection.North))
        {
            if (P.currentProfile.VLine && P.currentProfile.FrontStand)
            {
                var angle = Get18PieForAngle(GetAngle(bnpc));
                ActorConeXZ(bnpc, bnpc.HitboxRadius + GetConfiguredRadius(), Maths.Radians(angle.min), Maths.Radians(angle.max), P.currentProfile.FrontSegmentIndicator);
            }
            else
            {
                ActorConeXZ(bnpc, bnpc.HitboxRadius + GetConfiguredRadius(), Maths.Radians(-45), Maths.Radians(45), P.currentProfile.FrontSegmentIndicator);
            }
        }
    }

    internal static void DrawAnticipatedPos(BattleNpc bnpc)
    {
        var move = *P.memory.LastComboMove;
        var mnk = Svc.ClientState.LocalPlayer.ClassJob.Id == 20 && Svc.ClientState.LocalPlayer.Level >= 30;
        var mnkRear = mnk && MnkIsRear(bnpc);
        if (move == 16473 && P.currentProfile.MnkAoEDisable) return; //mnk Four-point Fury AoE
        var drglvl = Svc.ClientState.LocalPlayer.Level >= 50;
        if (move.EqualsAny(7478u) // sam
            || Util.CanExecuteGallows()
            || (move == 2242 && Svc.Gauges.Get<NINGauge>().HutonTimer > P.currentProfile.NinHutinTh)
            || NinRearTrickAttackAvailable()
            || (mnk && mnkRear)
            || Util.CanExecuteWheelingThrust() ||  (move.EqualsAny(87u) && drglvl)
            ) //rear
        {
            ActorConeXZ(bnpc, bnpc.HitboxRadius + GetSkillRadius(), Maths.Radians(180 - 45), Maths.Radians(180 + 45), P.currentProfile.AnticipatedPieSettings);
        }
        else if (move.EqualsAny(7479u)
            || Util.CanExecuteGibbet()
            || (move == 2242 && Svc.Gauges.Get<NINGauge>().HutonTimer <= P.currentProfile.NinHutinTh)
            || (mnk && !mnkRear && move.EqualsAny(54u, 61u))
            || Util.CanExecuteFangAndClaw()
            ) //sides/flank
        {
            ActorConeXZ(bnpc, bnpc.HitboxRadius + GetSkillRadius(), Maths.Radians(270 - 45), Maths.Radians(270 + 45), P.currentProfile.AnticipatedPieSettingsFlank);
            ActorConeXZ(bnpc, bnpc.HitboxRadius + GetSkillRadius(), Maths.Radians(90 - 45), Maths.Radians(90 + 45), P.currentProfile.AnticipatedPieSettingsFlank);
        }
    }

    private static bool MnkIsRear(BattleNpc bnpc)
    {
        return Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId.EqualsAny(109u, 110u))
            && (!bnpc.StatusList.TryGetFirst(x => x.StatusId.EqualsAny(246u, 3001u), out var status) || status.RemainingTime < P.currentProfile.MnkDemolish);
    }

    static int? TrickAttackCDGroup = null;
    private static bool NinRearTrickAttackAvailable()
    {
        if (!P.currentProfile.NinRearForTrickAttack) return false;
        TrickAttackCDGroup ??= Svc.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>().GetRow(2258).CooldownGroup - 1;
        return ActionManager.Instance()->GetRecastGroupDetail(TrickAttackCDGroup.Value)->IsActive == 0
            && Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId.EqualsAny(507u, 614u)); 
    }

    internal static void DrawCurrentPos(BattleNpc bnpc)
    {
        var angle = GetAngle(bnpc);
        var direction = MathHelper.GetCardinalDirection(angle);
        if (direction == CardinalDirection.North) return;
        var angles = Is18(direction) ? Get18PieForAngle(angle) : GetAngleRangeForDirection(direction);
        ActorConeXZ(bnpc, bnpc.HitboxRadius + GetConfiguredRadius(), Maths.Radians(angles.min), Maths.Radians(angles.max),
            direction == CardinalDirection.South ? P.currentProfile.CurrentPieSettings : P.currentProfile.CurrentPieSettingsFlank);
    }

    internal static bool Is18(CardinalDirection direction)
    {
        if(direction == CardinalDirection.North || direction == CardinalDirection.South)
        {
            return P.currentProfile.VLine;
        }
        else
        {
            return P.currentProfile.HLine;
        }
    }

    internal static void DrawSegmentedCircle(BattleNpc bnpc, float addRadius, bool lines)
    {
        var radius = bnpc.HitboxRadius + addRadius;

        var nColor = P.currentProfile.SameColor ?
            P.currentProfile.MaxMeleeSettingsN with { Color = P.currentProfile.FrontSegmentIndicator.Fill with { W = 1f } } :
            P.currentProfile.MaxMeleeSettingsN;
        ActorConeXZ(bnpc, radius, Maths.Radians(-45), Maths.Radians(45), nColor, lines);

        var sColor = P.currentProfile.SameColor ?
            P.currentProfile.MaxMeleeSettingsN with { Color = P.currentProfile.CurrentPieSettings.Fill with { W = 1f } } :
            P.currentProfile.MaxMeleeSettingsN with { Color = P.currentProfile.MaxMeleeSettingsS };
        ActorConeXZ(bnpc, radius, Maths.Radians(180 - 45), Maths.Radians(180 + 45), sColor, lines);

        var eColor = P.currentProfile.SameColor ?
            P.currentProfile.MaxMeleeSettingsN with { Color = P.currentProfile.CurrentPieSettingsFlank.Fill with { W = 1f } } :
            P.currentProfile.MaxMeleeSettingsN with { Color = P.currentProfile.MaxMeleeSettingsE };
        ActorConeXZ(bnpc, radius, Maths.Radians(270 - 45), Maths.Radians(270 + 45), eColor, lines);

        var wColor = P.currentProfile.SameColor ?
            P.currentProfile.MaxMeleeSettingsN with { Color = P.currentProfile.CurrentPieSettingsFlank.Fill with { W = 1f } } :
            P.currentProfile.MaxMeleeSettingsN with { Color = P.currentProfile.MaxMeleeSettingsW };
        ActorConeXZ(bnpc, radius, Maths.Radians(90 - 45), Maths.Radians(90 + 45), wColor, lines);

        if (P.currentProfile.VLine)
        {
            ActorLineXZ(bnpc, radius, Maths.Radians(0), nColor);
            ActorLineXZ(bnpc, radius, Maths.Radians(180), sColor);
        }
        if (P.currentProfile.HLine)
        {
            ActorLineXZ(bnpc, radius, Maths.Radians(270), wColor);
            ActorLineXZ(bnpc, radius, Maths.Radians(90), eColor);
        }
    }
}
