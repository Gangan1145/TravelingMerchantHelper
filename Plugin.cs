using System.Reflection;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;

namespace TravelingMerchantHelper;

[ApiVersion(2, 1)]
public class Plugin : TerrariaPlugin
{
    public override string Name => "TravelingMerchantHelper";
    public override string Author => "淦";
    public override string Description => "旅商永不离开（终极复活版），可指令刷新商店";
    public override Version Version => new(2026, 2, 19, 2);

    public Plugin(Main game) : base(game) { }

    private bool _merchantKilled = false; // 标记旅商是否被玩家击杀

    public override void Initialize()
    {
        ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
        ServerApi.Hooks.NpcKilled.Register(this, OnNpcKilled); // 监听击杀事件

        Commands.ChatCommands.Add(new Command("tshock.refreshshop", RefreshShop, "refreshshop", "rs")
        {
            HelpText = "刷新旅商商店（移除旧旅商，生成新旅商）"
        });
    }

    private void OnNpcKilled(NpcKilledEventArgs args)
    {
        if (args.npc?.type == NPCID.TravellingMerchant)
            _merchantKilled = true; // 记录被击杀
    }

    private void OnGameUpdate(EventArgs args)
    {
        bool hasActiveMerchant = false;

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (npc?.type != NPCID.TravellingMerchant)
                continue;

            if (npc.active)
            {
                hasActiveMerchant = true;
                // 重置所有AI值，强制标记为城镇NPC
                for (int j = 0; j < npc.ai.Length; j++)
                    npc.ai[j] = 0f;
                npc.townNPC = true;
                npc.netAlways = true;
                npc.netUpdate = true;
            }
            else if (npc.life > 0) // 非死亡消失（游戏主动移除）
            {
                if (_merchantKilled) continue; // 被击杀的不复活

                // 复活旅商
                npc.active = true;
                npc.life = npc.lifeMax;
                for (int j = 0; j < npc.ai.Length; j++)
                    npc.ai[j] = 0f;
                npc.townNPC = true;
                npc.netAlways = true;
                npc.netUpdate = true;

                // 传送到最近玩家身边
                TeleportToNearestPlayer(npc);
                hasActiveMerchant = true;
            }
        }

        // 如果没有活跃旅商且未被击杀，尝试生成
        if (!hasActiveMerchant && !_merchantKilled)
            WorldGen.SpawnTravelNPC();
    }

    private void TeleportToNearestPlayer(NPC npc)
    {
        TSPlayer nearest = null;
        double nearestDist = double.MaxValue;
        foreach (TSPlayer plr in TShock.Players)
        {
            if (plr?.Active != true) continue;
            double dist = Math.Abs(plr.X - npc.position.X) + Math.Abs(plr.Y - npc.position.Y);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = plr;
            }
        }

        if (nearest != null)
        {
            npc.position.X = nearest.X;
            npc.position.Y = nearest.Y - 3 * 16;
            npc.netUpdate = true;
        }
    }

    private void RefreshShop(CommandArgs args)
    {
        if (!args.Player.HasPermission("tshock.refreshshop"))
        {
            args.Player.SendErrorMessage("你没有权限使用此指令。");
            return;
        }

        // 移除现有旅商
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (npc?.active == true && npc.type == NPCID.TravellingMerchant)
            {
                npc.active = false;
                npc.netUpdate = true;
                break;
            }
        }

        _merchantKilled = false; // 重置击杀标记，允许新旅商受保护
        WorldGen.SpawnTravelNPC(); // 生成新旅商

        TSPlayer.All.SendSuccessMessage("[旅商助手] 旅商的商品已刷新！新的旅商正在到来。");
        args.Player.SendSuccessMessage("已刷新旅商商店，稍后与其对话查看新商品。");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
            ServerApi.Hooks.NpcKilled.Deregister(this, OnNpcKilled);
        }
        base.Dispose(disposing);
    }
}