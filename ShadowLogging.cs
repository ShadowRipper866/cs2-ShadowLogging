using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using ShadowLogger.Config;

namespace ShadowLogger;

public class ShadowChatLogger : BasePlugin
{
    public override string ModuleName => "Shadow Chat Logging";
    public override string ModuleVersion => "1.0";
    public override string ModuleAuthor => "ShadowRipper";
    public override string ModuleDescription => "https://github.com/ShadowRipper866";
    public static ShadowChatLogger Instance { get; set; } = new();
    private readonly PlayerChat _PlayerChat = new();
    public Globals g_Main = new();
    public override void Load(bool hotReload)
    {
        Instance = this;
        Configs.Load(ModuleDirectory);
        Configs.Shared.CookiesModule = ModuleDirectory;

        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterEventHandler<EventPlayerConnectFull>(OnEventPlayerConnectFull);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        RegisterEventHandler<EventRoundEnd>(OnEventRoundEnd);
        RegisterListener<Listeners.OnMapEnd>(OnMapEnd);

        AddCommandListener("say", OnPlayerChat, HookMode.Post);
		AddCommandListener("say_team", OnPlayerChatTeam, HookMode.Post);
        
        if(hotReload)
        {
            g_Main.ServerPublicIpAdress = ConVar.Find("ip")?.StringValue!;
            g_Main.ServerPort = ConVar.Find("hostport")?.GetPrimitiveValue<int>().ToString()!;

            if(Configs.GetConfigData().Locally_AutoDeleteLogsMoreThanXdaysOld > 0)
            {
                string Fpath = Path.Combine(ModuleDirectory, "../../plugins/ShadowLogging/logs/");
                Helper.DeleteOldFiles(Fpath, "*" + ".txt", TimeSpan.FromDays(Configs.GetConfigData().Locally_AutoDeleteLogsMoreThanXdaysOld));
            }

            _ = Task.Run(async () => 
            {
                string ip = await Helper.GetPublicIp();
                if(!string.IsNullOrEmpty(ip))
                {
                    g_Main.ServerPublicIpAdress = ip;
                }                
            });
        }
    }

    public void OnMapStart(string Map)
    {
        g_Main.ServerPublicIpAdress = ConVar.Find("ip")?.StringValue!;
        g_Main.ServerPort = ConVar.Find("hostport")?.GetPrimitiveValue<int>().ToString()!;

        if(Configs.GetConfigData().Locally_AutoDeleteLogsMoreThanXdaysOld > 0)
        {
            string Fpath = Path.Combine(ModuleDirectory, "../../plugins/ShadowLogging/logs/");
            Helper.DeleteOldFiles(Fpath, "*" + ".txt", TimeSpan.FromDays(Configs.GetConfigData().Locally_AutoDeleteLogsMoreThanXdaysOld));
        }

        _ = Task.Run(async () => 
        {
            string ip = await Helper.GetPublicIp();
            if(!string.IsNullOrEmpty(ip))
            {
                g_Main.ServerPublicIpAdress = ip;
            }                
        });
    }

    public HookResult OnEventPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        if (@event == null)return HookResult.Continue;

        var player = @event.Userid;
        if (!player.IsValid())return HookResult.Continue;

        Helper.AddPlayerInGlobals(player);

        return HookResult.Continue;
    }
    
    private HookResult OnPlayerChat(CCSPlayerController? player, CommandInfo info)
	{
        if (!player.IsValid())return HookResult.Continue;

        _PlayerChat.OnPlayerChat(player, info, false);

        return HookResult.Continue;
    }
    private HookResult OnPlayerChatTeam(CCSPlayerController? player, CommandInfo info)
	{
        if (!player.IsValid())return HookResult.Continue;

        _PlayerChat.OnPlayerChat(player, info, true);

        return HookResult.Continue;
    }

    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        if (@event == null) return HookResult.Continue;

        var player = @event.Userid;
        if (!player.IsValid())return HookResult.Continue;

        if (g_Main.Player_Data.ContainsKey(player))g_Main.Player_Data.Remove(player);

        return HookResult.Continue;
    }

    public HookResult OnEventRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        if (@event == null)return HookResult.Continue;

        if (Configs.GetConfigData().Locally_Enable == 2)
        {
            Helper.DelayLogLocally();
        }
        return HookResult.Continue;
    }

    public void OnMapEnd()
    {
        Helper.ClearVariables();

        if (Configs.GetConfigData().Locally_Enable == 3)
        {
            Helper.DelayLogLocally();
        }
    }
    public override void Unload(bool hotReload)
    {
        Helper.ClearVariables();
    }


    /* [ConsoleCommand("css_test", "test")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void test(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!player.IsValid())return;

        
    } */
}