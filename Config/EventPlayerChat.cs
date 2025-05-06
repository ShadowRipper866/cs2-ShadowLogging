using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Commands;
using ShadowLogger.Config;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;

namespace ShadowLogger;

public class PlayerChat
{
    public HookResult OnPlayerChat(CCSPlayerController? player, CommandInfo info, bool TeamChat)
	{
        var g_Main = ShadowChatLogger.Instance.g_Main;
        if (!player.IsValid())return HookResult.Continue;

        Helper.AddPlayerInGlobals(player);

        var eventmessage = info.ArgString;
        eventmessage = eventmessage.TrimStart('"');
        eventmessage = eventmessage.TrimEnd('"');
        if (string.IsNullOrWhiteSpace(eventmessage)) return HookResult.Continue;

        string trimmedMessageStart = eventmessage.TrimStart();
        string message = trimmedMessageStart.TrimEnd();
        
        Helper.LogLocally(player, message, TeamChat);
        Helper.LogMySql(player, message, TeamChat);
        Helper.LogDiscord(player, message, TeamChat);

        return HookResult.Continue;
    }
}