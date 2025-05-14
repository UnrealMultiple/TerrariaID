using System.IO.Compression;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using NuGet.Packaging;
using ReLogic.Content.Sources;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;
using Terraria.UI;
using TerrariaApi.Server;
using TShockAPI;
using Utils = Terraria.Utils;

namespace IDExporter;

[ApiVersion(2, 1)]
public partial class IDExporter : TerrariaPlugin
{
    public IDExporter(Main game)
        : base(game)
    {
    }

    public override string Author => "Cai";

    public override string Description => "IDExporter";

    public override string Name => "导出泰拉瑞亚ID!!!";
    public override Version Version => new(2025, 5, 10, 1);

    public override void Initialize()
    {
        Commands.ChatCommands.Add(new Command(Dump, "dump"));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Commands.ChatCommands.RemoveAll(x=> x.CommandDelegate == Dump);
        }
            
        base.Dispose(disposing);
    }

    public string ReplaceTag(string input)
    {
        input = input.Replace("<right>", "右键").Replace("<left>", "左键");
        string[] lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        // 匹配 ItemTag 的正则
        Regex itemTagRegex =
            new(@"\[i(?:tem)?(?:\/s(?<Stack>\d{1,4}))?(?:\/p(?<Prefix>\d{1,3}))?:(?<NetID>-?\d{1,4})\]");
        // 匹配 ColorTag 的正则
        Regex colorTagRegex = new(@"\[c\/(?<color>[0-9a-fA-F]{6}):(?<text>.*?)\]");

        for (int i = 0; i < lines.Length; i++)
        {
            // 先处理ItemTag
            lines[i] = itemTagRegex.Replace(lines[i], match =>
            {
                Item item = TShock.Utils.GetItemFromTag(match.Value);
                string replacement = "";

                // 检查是否在行首
                if (match.Index == 0)
                {
                    replacement = "#️⃣"; // 行首添加前缀
                }

                else
                {
                    // 当出现[i:4959]Item.Name时删除[i:4959]
                    // Check if the text after the match is the item's name
                    int nextCharPos = match.Index + match.Length;
                    if (nextCharPos < lines[i].Length &&
                        lines[i].Substring(nextCharPos).StartsWith(item.Name))
                        replacement = ""; // 完全删除这个标签
                    else
                        replacement = item.Name; // 保持原样
                }

                return replacement;
            });

            // 再处理ColorTag
            lines[i] = colorTagRegex.Replace(lines[i], match =>
            {
                string hexColor = match.Groups["color"].Value;
                string text = match.Groups["text"].Value;

                // 检查是否在行首
                bool isLineStart = match.Index == 0;

                if (hexColor.Length == 6 &&
                    int.TryParse(hexColor, System.Globalization.NumberStyles.HexNumber, null, out int rgb))
                    return isLineStart
                        ? $"🌈{text}"
                        : $"{text}";

                return text; // 无效颜色标签
            });
        }

        // 重新拼接所有行
        return string.Join(Environment.NewLine, lines);
    }

    private void LoadResources()
    {
        GameServiceContainer services = new();

        Utils.TryCreatingDirectory(@"tshock/ResourcePacks/");
        List<IContentSource> list = new();
        foreach (string directory in Directory.GetDirectories(@"tshock/ResourcePacks/"))
            try
            {
                if (directory.Contains("2440470208")) continue;
                ResourcePack pack = new(services, directory);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(
                    $"[PackLoader]{pack.Name}已经加载！ v{pack.Version.Major}.{pack.Version.Minor} by {pack.Author}");

                Console.ResetColor();

                list.Add(pack.GetContentSource());
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[PackLoader]{directory}加载失败! {ex.Message}");
            }


        LanguageManager.Instance.UseSources(list);
    }

    private void LoadStandard()
    {
        GameServiceContainer services = new();

        Utils.TryCreatingDirectory(@"tshock/ResourcePacks/");
        List<IContentSource> list = new();
        try
        {
            ResourcePack pack = new(services, @"tshock/ResourcePacks/2440470208");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(
                $"[PackLoader]{pack.Name}已经加载！ v{pack.Version.Major}.{pack.Version.Minor} by {pack.Author}");

            Console.ResetColor();

            list.Add(pack.GetContentSource());
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[PackLoader]Standard加载失败! {ex.Message}");
        }


        LanguageManager.Instance.UseSources(list);
    }

    private void Dump(CommandArgs args)
    {
        // Create dictionaries to store original names before loading resources
        Dictionary<int, List<string>> originalPrefixNames = new();
        Dictionary<int, List<string>> originalBuffNames = new();
        Dictionary<int, List<string>> originalProjectileNames = new();
        Dictionary<int, List<string>> originalItemNames = new();
        Dictionary<int, List<string>> originalNpcNames = new();

        // Record original names
        for (int i = 0; i < PrefixID.Count; i++)
        {
            originalPrefixNames[i] = new List<string>();
            originalPrefixNames[i].Add(Lang.prefix[i].Value);
        }

        foreach (KeyValuePair<int, string> i in BuffID.Search._idToName)
        {
            originalBuffNames[i.Key] = new List<string>();
            originalBuffNames[i.Key].Add(Lang.GetBuffName(i.Key));
        }

        foreach (KeyValuePair<int, string> i in ProjectileID.Search._idToName)
        {
            originalProjectileNames[i.Key] = new List<string>();
            originalProjectileNames[i.Key].Add(Lang.GetProjectileName(i.Key).Value);
        }

        foreach (KeyValuePair<int, string> i in ItemID.Search._idToName)
            if (i.Key >= 0)
            {
                originalItemNames[i.Key] = new List<string>();
                originalItemNames[i.Key].Add(Lang.GetItemNameValue(i.Key));
            }

        foreach (KeyValuePair<int, string> i in NPCID.Search._idToName)
            if (i.Key >= 0)
            {
                originalNpcNames[i.Key] = new List<string>();
                originalNpcNames[i.Key].Add(Lang.GetNPCNameValue(i.Key));
            }

        LoadStandard();

        // Record original names
        for (int i = 0; i < PrefixID.Count; i++)
            if (!originalPrefixNames[i].Contains(Lang.prefix[i].Value))
                originalPrefixNames[i].Add(Lang.prefix[i].Value);

        foreach (KeyValuePair<int, string> i in BuffID.Search._idToName)
            if (!originalBuffNames[i.Key].Contains(Lang.GetBuffName(i.Key)))
                originalBuffNames[i.Key].Add(Lang.GetBuffName(i.Key));

        foreach (KeyValuePair<int, string> i in ProjectileID.Search._idToName)
            if (!originalProjectileNames[i.Key].Contains(Lang.GetProjectileName(i.Key).Value))
                originalProjectileNames[i.Key].Add(Lang.GetProjectileName(i.Key).Value);

        foreach (KeyValuePair<int, string> i in ItemID.Search._idToName)
            if (i.Key >= 0)
                if (!originalItemNames[i.Key].Contains(Lang.GetItemNameValue(i.Key)))
                    originalItemNames[i.Key].Add(Lang.GetItemNameValue(i.Key));

        foreach (KeyValuePair<int, string> i in NPCID.Search._idToName)
            if (i.Key >= 0)
                if (!originalNpcNames[i.Key].Contains(Lang.GetNPCNameValue(i.Key)))
                    originalNpcNames[i.Key].Add(Lang.GetNPCNameValue(i.Key));

        // Now load resources
        LoadResources();
        Thread.Sleep(5000);

        string folderPath = "TerrariaID";
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        // Process prefixes with alias checking
        List<PrefixInfo> prefixInfos = new();
        for (int i = 0; i < PrefixID.Count; i++)
            try
            {
                string currentName = Lang.prefix[i].Value;
                PrefixInfo prefixInfo = new()
                {
                    PrefixId = i,
                    Name = currentName
                };

                // Check if name changed after loading resources
                if (originalPrefixNames.TryGetValue(i, out List<string>? originalName) &&
                    !originalName.Contains(currentName)) prefixInfo.Alias.AddRange(originalName);
                prefixInfos.Add(prefixInfo);
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[PrefixExporter]{i.ToString()}导出失败! {ex.Message}");
            }

        // Process buffs with alias checking
        List<BuffInfo> buffInfos = new();
        foreach (KeyValuePair<int, string> i in BuffID.Search._idToName)
            try
            {
                string currentName = Lang.GetBuffName(i.Key);
                BuffInfo buffInfo = new()
                {
                    BuffId = i.Key,
                    Name = currentName,
                    Description = ReplaceTag(Lang.GetBuffDescription(i.Key))
                };

                if (originalBuffNames.TryGetValue(i.Key, out List<string>? originalName) &&
                    !originalName.Contains(currentName)) buffInfo.Alias.AddRange(originalName);
                buffInfos.Add(buffInfo);
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[BuffExporter]{folderPath}导出失败! {ex.Message}");
            }

        // Process projectiles with alias checking
        List<ProjectInfo> projectInfos = new();
        foreach (KeyValuePair<int, string> i in ProjectileID.Search._idToName)
            try
            {
                string currentName = Lang.GetProjectileName(i.Key).Value;
                ProjectInfo projectInfo = new();
                Projectile projectile = new();
                projectile.SetDefaults(i.Key);
                projectInfo.Name = currentName;
                projectInfo.ProjId = i.Key;
                projectInfo.Friendly = projectile.friendly;
                projectInfo.AiStyle = projectile.aiStyle;

                if (originalProjectileNames.TryGetValue(i.Key, out List<string>? originalName) &&
                    !originalName.Contains(currentName)) projectInfo.Alias.AddRange(originalName);
                projectInfos.Add(projectInfo);
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[ProjectileExporter]{i.ToString()}导出失败! {ex.Message}");
            }

        // Process items with alias checking and manual overrides
        List<ItemInfo> itemInfos = new();
        foreach (KeyValuePair<int, string> i in ItemID.Search._idToName)
            try
            {
                if (i.Key < 0) continue;
                string currentName = Lang.GetItemNameValue(i.Key);
                ItemInfo itemInfo = new();
                Item item = new();
                item.SetDefaults(i.Key);

                if (Lang.GetTooltip(i.Key) != ItemTooltip.None)
                    itemInfo.Description = ReplaceTag(Lang.GetTooltip(i.Key)._text.Value);

                itemInfo.Name = currentName;

                // Then check if name changed from original
                if (originalItemNames.TryGetValue(i.Key, out List<string>? originalName) &&
                    !originalName.Contains(currentName)) itemInfo.Alias.AddRange(originalName);

                itemInfo.ItemId = i.Key;
                itemInfo.Damage = item.damage;
                itemInfo.MonetaryValue = new CoinValue(item.value);
                itemInfo.MaxStack = item.maxStack;
                itemInfos.Add(itemInfo);
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[ItemExporter]{i.ToString()}导出失败! {ex.Message}");
            }

        // Process NPCs with alias checking
        List<NpcInfo> npcs = new();
        foreach (KeyValuePair<int, string> i in NPCID.Search._idToName)
            try
            {
                if (i.Key < 0) continue;
                string currentName = Lang.GetNPCNameValue(i.Key);
                NpcInfo npc = new();
                NPCStatsReportInfoElement npcStatsReportInfoElement = new(i.Key);
                npc.Name = currentName;
                npc.NpcId = i.Key;
                npc.Damage = npcStatsReportInfoElement.Damage;
                npc.LifeMax = npcStatsReportInfoElement.LifeMax;
                npc.MonetaryValue = new CoinValue((int)npcStatsReportInfoElement.MonetaryValue);

                if (originalNpcNames.TryGetValue(i.Key, out List<string>? originalName) &&
                    !originalName.Contains(currentName)) npc.Alias.AddRange(originalName);

                string key = "Bestiary_FlavorText.npc_" + Lang.GetNPCName(i.Key).Key.Replace("NPCName.", "");
                if (Language.Exists(key))
                    npc.Description = ReplaceTag(Language.GetText(key).Value);
                npcs.Add(npc);
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"[NPCExporter]{i.ToString()}导出失败! {ex.Message}");
            }


        // Write all files as before
        Write("prefix_id.json", prefixInfos);
        Console.WriteLine($"[IdExporter]导出{prefixInfos.Count}个修饰语...");

        Write("buff_id.json", buffInfos);
        Console.WriteLine($"[IdExporter]导出{buffInfos.Count}个Buff...");

        Write("project_id.json", projectInfos);
        Console.WriteLine($"[IdExporter]导出{projectInfos.Count}个弹幕...");

        Write("item_id.json", itemInfos);
        Console.WriteLine($"[IdExporter]导出{itemInfos.Count}个物品...");

        Write("npc_id.json", npcs);
        Console.WriteLine($"[IdExporter]导出{npcs.Count}个生物...");

        Environment.Exit(0);
    }


    private static void Write(string filename, object obj)
    {
        using FileStream fileStream =
            new($"TerrariaID/{filename}", FileMode.Create, FileAccess.Write, FileShare.Write);
        string value = JsonConvert.SerializeObject(obj, Formatting.Indented);
        using StreamWriter streamWriter = new(fileStream);
        streamWriter.Write(value);
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
        public List<string> Alias = new(); // Add this
    }

    public class ItemInfo
    {
        public int Damage;
        public string Description = "";
        public int ItemId;
        public int MaxStack;
        public CoinValue MonetaryValue = new(0);
        public string Name = "";
        public List<string> Alias = new(); // Add this
    }

    public class ProjectInfo
    {
        public int AiStyle;
        public string Name = "";
        public int ProjId;
        public bool Friendly;
        public List<string> Alias = new(); // Add this
    }

    public class BuffInfo
    {
        public int BuffId;
        public string Description = "";
        public string Name = "";
        public List<string> Alias = new(); // Add this
    }

    public class PrefixInfo
    {
        public string Name = "";
        public int PrefixId;
        public List<string> Alias = new(); // Add this
    }
}