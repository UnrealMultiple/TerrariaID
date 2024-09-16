using Newtonsoft.Json;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using TerrariaApi.Server;

namespace IDExporter;

[ApiVersion(2, 1)]
public class IDExporter : TerrariaPlugin
{
    public IDExporter(Main game)
        : base(game)
    {
    }

    public override string Author => "Cai";

    public override string Description => "IDExporter";

    public override string Name => "导出泰拉瑞亚ID!!!";
    public override Version Version => new(2024, 9, 16, 1);

    public override void Initialize()
    {
        ServerApi.Hooks.GamePostInitialize.Register(this, OnGamePostInitialize);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            ServerApi.Hooks.GamePostInitialize.Deregister(this, OnGamePostInitialize);
        base.Dispose(disposing);
    }

    private void OnGamePostInitialize(EventArgs args)
    {
        Task.Run(() =>
        {
            Thread.Sleep(5000);
            string folderPath = "TerrariaID";
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);


            List<PrefixInfo> prefixInfos = new();

            foreach (KeyValuePair<int, string> i in BuffID.Search._idToName)
                try
                {
                    PrefixInfo prefixInfo = new();
                    prefixInfo.PrefixId = i.Key;
                    prefixInfo.Name = Lang.prefix[i.Key].Value;
                    prefixInfos.Add(prefixInfo);
                }
                catch
                {
                }

            Write("Prefix_ID.json", prefixInfos);
            Console.WriteLine($"[IdExporter]导出{prefixInfos.Count}个修饰语...");

            List<BuffInfo> buffInfos = new();

            foreach (KeyValuePair<int, string> i in BuffID.Search._idToName)
                try
                {
                    BuffInfo buffInfo = new();
                    buffInfo.BuffId = i.Key;
                    buffInfo.Name = Lang.GetBuffName(i.Key);
                    buffInfo.Description = Lang.GetBuffDescription(i.Key);
                    buffInfos.Add(buffInfo);
                }
                catch
                {
                }

            Write("Buff_ID.json", buffInfos);
            Console.WriteLine($"[IdExporter]导出{buffInfos.Count}个Buff...");

            List<ProjectInfo> projectInfos = new();

            Projectile projectile = new();

            foreach (KeyValuePair<int, string> i in ProjectileID.Search._idToName)
                try
                {
                    ProjectInfo projectInfo = new();
                    projectile = new Projectile();
                    projectile.SetDefaults(i.Key);
                    projectInfo.Name = Lang.GetProjectileName(i.Key).Value;
                    projectInfo.ProjId = i.Key;
                    projectInfo.Friendly = projectile.friendly;
                    projectInfo.AiStyle = projectile.aiStyle;
                    projectInfos.Add(projectInfo);
                }
                catch
                {
                }

            Write("Project_ID.json", projectInfos);
            Console.WriteLine($"[IdExporter]导出{projectInfos.Count}个弹幕...");

            List<ItemInfo> itemInfos = new();

            Item item = new();

            foreach (KeyValuePair<int, string> i in ItemID.Search._idToName)
                try
                {
                    ItemInfo itemInfo = new();

                    item = new Item();

                    item.SetDefaults(i.Key);
                    if (Lang.GetTooltip(i.Key) != ItemTooltip.None)
                        itemInfo.Description = Lang.GetTooltip(i.Key)._text.Value;
                    itemInfo.Name = Lang.GetItemNameValue(i.Key);
                    itemInfo.ItemId = i.Key;
                    itemInfo.Damage = item.damage;
                    itemInfo.MonetaryValue = new CoinValue(item.value);
                    itemInfo.MaxStack = item.maxStack;
                    itemInfos.Add(itemInfo);
                }
                catch
                {
                }

            Write("Item_ID.json", itemInfos);
            Console.WriteLine($"[IdExporter]导出{itemInfos.Count}个弹幕...");

            List<NpcInfo> npcs = new();
            foreach (KeyValuePair<int, string> i in NPCID.Search._idToName)
                try
                {
                    NpcInfo npc = new();
                    NPCStatsReportInfoElement npcStatsReportInfoElement = new(i.Key);
                    npc.Name = Lang.GetNPCNameValue(i.Key);
                    npc.NpcId = i.Key;
                    npc.Damage = npcStatsReportInfoElement.Damage;
                    npc.LifeMax = npcStatsReportInfoElement.LifeMax;
                    npc.MonetaryValue = new CoinValue((int)npcStatsReportInfoElement.MonetaryValue);
                    string key = "Bestiary_FlavorText.npc_" + Lang.GetNPCName(i.Key).Key.Replace("NPCName.", "");
                    if (Language.Exists(key))
                        npc.Description = Language.GetText(key).Value;
                    npcs.Add(npc);
                }
                catch
                {
                }

            Write("NPC_ID.json", npcs);
            Console.WriteLine($"[IdExporter]导出{npcs.Count}个生物...");

            Environment.Exit(0);
        });
    }


    public static void Write(string filename, object obj)
    {
        using FileStream fileStream =
            new($"TerrariaID/{filename}", FileMode.Create, FileAccess.Write, FileShare.Write);
        string value = JsonConvert.SerializeObject(obj, Formatting.Indented);
        using (StreamWriter streamWriter = new(fileStream))
        {
            streamWriter.Write(value);
        }
    }


    public class CoinValue
    {
        public int Copper;

        public int Gold;

        public int Platinum;

        public int Silver;

        public CoinValue(int value)
        {
            Platinum = value / (100 * 100 * 100);
            value %= 100 * 100 * 100;
            Gold = value / (100 * 100);
            value %= 100 * 100;
            Silver = value / 100;
            Copper = value % 100;
        }
    }

    public class NpcInfo
    {
        public int Damage;
        public string Description = "";
        public int LifeMax;
        public CoinValue MonetaryValue = new(0);
        public string Name = "";
        public int NpcId;
    }

    public class ItemInfo
    {
        public int Damage;
        public string Description = "";
        public int ItemId;
        public int MaxStack;
        public CoinValue MonetaryValue = new(0);
        public string Name = "";
    }

    public class ProjectInfo
    {
        public int AiStyle;
        public string Name = "";
        public int ProjId;
        public bool Friendly;
    }

    public class BuffInfo
    {
        public int BuffId;
        public string Description = "";
        public string Name = "";
    }

    public class PrefixInfo
    {
        public string Name = "";
        public int PrefixId;
    }
}