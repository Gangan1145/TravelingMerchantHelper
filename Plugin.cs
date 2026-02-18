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
    public override string Description => "旅商不再自动离开，可通过指令刷新商店";
    public override Version Version => new(2026, 2, 19, 1);

    public Plugin(Main game) : base(game) { }

    public override void Initialize()
    {
        // 使用 GameUpdate 钩子（兼容所有 TShock 版本）
        ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
        
        Commands.ChatCommands.Add(new Command("tshock.refreshshop", RefreshShop, "refreshshop", "rs")
        {
            HelpText = "刷新旅商的当前商品列表"
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
        }
        base.Dispose(disposing);
    }

    // 每帧检查所有 NPC，重置旅商的离开计时器
    private void OnGameUpdate(EventArgs args)
    {
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (npc != null && npc.active && npc.type == NPCID.TravellingMerchant)
            {
                npc.ai[0] = 0f; // 重置 ai[0] 阻止离开
            }
        }
    }

    // 指令处理：刷新旅商商店（使用反射调用）
    private void RefreshShop(CommandArgs args)
    {
        if (!args.Player.HasPermission("tshock.refreshshop"))
        {
            args.Player.SendErrorMessage("你没有权限使用此指令。");
            return;
        }

        try
        {
            // 反射调用 WorldGen.TravelShop，避免编译时符号错误
            MethodInfo travelShopMethod = typeof(WorldGen).GetMethod("TravelShop", BindingFlags.Public | BindingFlags.Static);
            if (travelShopMethod != null)
            {
                travelShopMethod.Invoke(null, null);
                TSPlayer.All.SendSuccessMessage("[旅商助手] 旅商的商品已刷新！");
                args.Player.SendSuccessMessage("已刷新旅商商店，与旅商对话查看新商品。");
            }
            else
            {
                args.Player.SendErrorMessage("刷新商店失败：找不到 TravelShop 方法，请确认 Terraria 版本。");
            }
        }
        catch (Exception ex)
        {
            args.Player.SendErrorMessage($"刷新商店时发生错误：{ex.Message}");
        }
    }
}