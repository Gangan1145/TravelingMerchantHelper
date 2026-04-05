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
    public override string Description => "通过指令刷新旅商商店（移除旧旅商，生成新旅商）";
    public override Version Version => new(2026, 2, 19, 4);

    public Plugin(Main game) : base(game) { }

    public override void Initialize()
    {
        Commands.ChatCommands.Add(new Command("tshock.refreshshop", RefreshShop, "refreshshop", "rs")
        {
            HelpText = "刷新旅商商店（移除旧旅商并生成新旅商）"
        });
    }

    private void RefreshShop(CommandArgs args)
    {
        if (!args.Player.HasPermission("tshock.refreshshop"))
        {
            args.Player.SendErrorMessage("你没有权限使用此指令。");
            return;
        }

        // 移除所有现有的旅商（标记为死亡并设为不活跃）
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (npc?.active == true && npc.type == NPCID.TravellingMerchant)
            {
                npc.active = false;
                npc.life = 0; // 确保不会复活
                npc.netUpdate = true;
            }
        }

        // 生成新的旅商
        WorldGen.SpawnTravelNPC();

        TSPlayer.All.SendSuccessMessage("[旅商助手] 旅商的商品已刷新！新的旅商正在到来。");
        args.Player.SendSuccessMessage("已刷新旅商商店，稍后与其对话查看新商品。");
    }

    protected override void Dispose(bool disposing)
    {
        // 无需注销钩子，因为没有注册任何钩子
        base.Dispose(disposing);
    }
}