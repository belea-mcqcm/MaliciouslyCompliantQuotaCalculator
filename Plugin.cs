using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using TerminalApi.Classes;
using static TerminalApi.TerminalApi;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MaliciouslyCompliantQuotaCalculator
{
    class View
    {
        int start;
        int[] sequence;

        public View(int[] sequence, int start)
        {
            this.sequence = sequence;
            this.start = start;
        }

        public int this[int i]
        {
            get => sequence[start + i];
            set => sequence[start + i] = value;
        }
    }

    // TODO get fines if company catches you using this command
    [BepInPlugin(modGUID, modName, modVersion)]
    public class MCQCModBase : BaseUnityPlugin
    {
        private const string modGUID = "belea.mcqcm";
        private const string modName = "Maliciously Compliant Quota Calculator Mod";
        private const string modVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);
        private static MCQCModBase Instance;

        private const int formatSpacing = 27;

        internal static MCQCMConfig Config;
        internal ManualLogSource mls;

        void Awake()
        {
            if (Instance == null)
                Instance = this;

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            Config = new MCQCMConfig(((BaseUnityPlugin)this).Config);
            Config.RegisterOptions();

            harmony.PatchAll(typeof(MCQCModBase));

            AddCommand("mcqc", new CommandInfo
            {
                Category = "other",
                Description = "To calculate the optimal list of scrap to sell to fulfill quota.",
                DisplayTextSupplier = OnCalculateCommand
            },
            "today");
            mls.LogInfo("The Maliciously Compliant Quota Calculator(TM) add-on has been installed...");
        }

        private int GetTotalValue(List<GrabbableObject> scrapList)
        {
            return scrapList.Sum((GrabbableObject scrap) => scrap.scrapValue); ;
        }

        // Add ellipses if item name too long
        private string ShortenItemName(string itemName, int space = formatSpacing)
        {
            return (itemName.Length >= space - 2) ? itemName.Substring(0, space - 5) + "..." : itemName;
        }

        private string AddDollarSign(int value)
        {
            return "$" + value.ToString();
        }

        private string SpaceOut(string left, string right, int space = formatSpacing)
        {
            left = ShortenItemName(left, space);
            return left + new string(' ', space - left.Length) + right;
        }

        private string FormatOutput(List<GrabbableObject> scrapList, List<GrabbableObject> bestScrapList, int threshold, bool wantsQuota = true, bool wantsToday = false)
        {
            StringBuilder screen = new StringBuilder();
            if (wantsToday)
            {
                screen.AppendLine("Calculating for today's buying rate: " + Mathf.RoundToInt(StartOfRound.Instance.companyBuyingRate * 100f) + "%");
            }
            else
            {
                screen.AppendLine("Calculating for final day's buying rate: 100%");
            }
            screen.AppendLine();

            int total = GetTotalValue(scrapList);
            int subtotal = GetTotalValue(bestScrapList);
            if (bestScrapList.Count > 0)
            {
                screen.AppendLine(SpaceOut("Scrap name", "Value", Config.OutputSpacing.Value + subtotal.ToString().Length - 4));
                screen.AppendLine(new string('=', Config.OutputSpacing.Value + subtotal.ToString().Length + 1));
            }
            foreach (var scrap in SortScrapList(bestScrapList))
            {
                screen.AppendLine(SpaceOut(scrap.itemProperties.itemName, AddDollarSign(scrap.scrapValue), Config.OutputSpacing.Value));
            }

            if (bestScrapList.Count == 0)
            {
                if (wantsQuota)
                {
                    screen.Append("Quota ");
                }
                else
                    screen.Append("Credit target ");

                screen.Append("cannot be fulfilled ");
                if (wantsToday)
                    screen.Append("today ");
                screen.AppendLine("with current scrap!");
            }
            else
            {
                screen.AppendLine();
                screen.AppendLine(SpaceOut("Subtotal", AddDollarSign(subtotal), Config.OutputSpacing.Value));
                screen.AppendLine(new string('=', Config.OutputSpacing.Value + subtotal.ToString().Length + 1));
            }

            if (Config.Verbosity.Value > 0)
            {
                // Nominal value requested
                screen.AppendLine();
                if (wantsQuota)
                {
                    screen.AppendLine(SpaceOut("Quota left", AddDollarSign(threshold)));
                }
                else
                {
                    screen.AppendLine(SpaceOut("Credits needed", AddDollarSign(threshold)));
                }
            }

            if (Config.Verbosity.Value > 1)
            {
                // General info
                screen.AppendLine();
                screen.AppendLine(SpaceOut("Total value on ship", AddDollarSign(total)));
                if (bestScrapList.Count > 0)
                {
                    screen.AppendLine(SpaceOut("Total value after sale", AddDollarSign(total - subtotal)));
                }
                else
                {
                    screen.AppendLine(SpaceOut("Total value needed", AddDollarSign(ReadjustForCompanyRate(threshold - AdjustForCompanyRate(total, wantsToday), wantsToday))));
                }
            }

            return screen.ToString();
        }

        private List<GrabbableObject> SortScrapList(List<GrabbableObject> scrapList)
        {
            scrapList.Sort(new Comparison<GrabbableObject>(
                (GrabbableObject left, GrabbableObject right) =>
                {
                    if (left.itemProperties.itemName == right.itemProperties.itemName)
                        return left.scrapValue.CompareTo(right.scrapValue) * (Config.IncreasingOrder.Value ? 1 : -1);
                    return left.itemProperties.itemName.CompareTo(right.itemProperties.itemName);
                }));
            return scrapList;
        }

        private string OnCalculateCommand()
        {
            string terminalInput = GetTerminalInput();
            mls.LogInfo("MCQC input: " + terminalInput);

            var scrapList = GetScrapInShip();
            foreach (var scrap in scrapList)
                mls.LogDebug("MCQC Found scrap in ship: " + scrap.itemProperties.itemName + " - " + scrap.scrapValue + "...");

            string[] tokens = terminalInput.ToLower().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            bool wantsToday = tokens.Contains("today");
            bool wantsQuota = true;
            int threshold = TimeOfDay.Instance.profitQuota - TimeOfDay.Instance.quotaFulfilled;
            foreach (string token in tokens)
            {
                int parsed;
                if (int.TryParse(token, out parsed) && parsed > 0)
                {
                    threshold = parsed - FindObjectOfType<Terminal>().groupCredits;
                    if (threshold <= 0)
                        return "Current credits value exceeds value requested, there is no need to sell anything.\n";
                    wantsQuota = false;
                    break;
                }
            }

            return FormatOutput(scrapList, Pisinger(scrapList, threshold, wantsToday), threshold, wantsQuota, wantsToday);
        }

        private int AdjustForCompanyRate(int scrapValue, bool wantsToday)
        {
            return wantsToday ? Mathf.FloorToInt(scrapValue * StartOfRound.Instance.companyBuyingRate) : scrapValue;
        }

        private int ReadjustForCompanyRate(int scrapValue, bool wantsToday)
        {
            return wantsToday ? Mathf.CeilToInt(scrapValue / StartOfRound.Instance.companyBuyingRate) : scrapValue;
        }

        // TODO always assert stuff, better check the bookmark
        List<GrabbableObject> Pisinger(List<GrabbableObject> scrapList, int threshold, bool wantsToday)
        {
            int totalValue = GetTotalValue(scrapList);
            // Pisinger finds the biggest sum under a threshold
            // I want the smallest sum over a threshold
            // Thus what I want is Pisinger(total-threshold)
            // and to select the values NOT selected by the above
            threshold = ReadjustForCompanyRate(AdjustForCompanyRate(totalValue, wantsToday) - threshold, wantsToday);
            if (threshold < 0)
                return new List<GrabbableObject>();
            if (threshold == 0)
                return scrapList;

            int sumW = 0;
            int r = 0;
            foreach (var scrap in scrapList)
            {
                sumW += scrap.scrapValue;
                r = Math.Max(r, scrap.scrapValue);
            }

            int b;
            int wBar = 0;
            for (b = 0; wBar + scrapList[b].scrapValue <= threshold; b++)
                wBar += scrapList[b].scrapValue;

            int[][] s = new int[scrapList.Count - b + 1][];
            for (int i = 0; i < s.Length; i++)
            {
                s[i] = new int[2 * r];
                for (int j = 0; j < s[i].Length; j++)
                    s[i][j] = 0;
            }

            View s_b_1 = new View(s[0], r - 1);
            for (int mu = -r + 1; mu <= 0; mu++)
                s_b_1[mu] = -1;
            for (int mu = 1; mu <= r; mu++)
                s_b_1[mu] = 0;

            s_b_1[wBar - threshold] = b;
            for (int t = b; t < scrapList.Count; t++)
            {
                View s_t_1 = new View(s[t - b], r - 1);
                View s_t = new View(s[t - b + 1], r - 1);
                for (int mu = -r + 1; mu < r + 1; mu++)
                    s_t[mu] = s_t_1[mu];
                for (int mu = -r + 1; mu <= 0; mu++)
                {
                    int mu_prime = mu + scrapList[t].scrapValue;
                    s_t[mu_prime] = Math.Max(s_t[mu_prime], s_t_1[mu]);
                }
                for (int mu = scrapList[t].scrapValue; mu > 0; mu--)
                {
                    for (int j = s_t[mu] - 1; j > s_t_1[mu] - 1; j--)
                    {
                        int mu_prime = mu - scrapList[j].scrapValue;
                        s_t[mu_prime] = Math.Max(s_t[mu_prime], j);
                    }
                }
            }

            bool solved = false;
            int z;
            View s_n_1 = new View(s[scrapList.Count - b], r - 1);
            for (z = 0; z >= -r + 1; z--)
            {
                if (s_n_1[z] >= 0)
                {
                    solved = true;
                    break;
                }
            }
            if (solved)
            {
                bool[] x = new bool[scrapList.Count];
                for (int i = 0; i < x.Length; i++)
                    x[i] = false;
                for (int i = 0; i < b; i++)
                    x[i] = true;
                for (int t = scrapList.Count - 1; t > b - 1; t--)
                {
                    View s_t = new View(s[t - b + 1], r - 1);
                    View s_t_1 = new View(s[t - b], r - 1);
                    int z_unprime;
                    while (true)
                    {
                        int j = s_t[z];
                        z_unprime = z + scrapList[j].scrapValue;
                        if (z_unprime > r || j >= s_t[z_unprime])
                            break;
                        z = z_unprime;
                        x[j] = false;
                    }
                    z_unprime = z - scrapList[t].scrapValue;
                    if (z_unprime >= -r + 1 && s_t_1[z_unprime] >= s_t[z])
                    {
                        z = z_unprime;
                        x[t] = true;
                    }
                }
                List<GrabbableObject> bestScrapList = new List<GrabbableObject>();
                for (int i = 0; i < scrapList.Count; i++)
                    if (!x[i])
                        bestScrapList.Add(scrapList[i]);
                return bestScrapList;
            }

            return new List<GrabbableObject>();
        }

        private List<GrabbableObject> GetScrapInShip()
        {
            List<GrabbableObject> scrapList = new List<GrabbableObject>();
            foreach (var obj in FindObjectsOfType<GrabbableObject>())
                if (obj.isInShipRoom && obj.itemProperties.isScrap && !(obj is RagdollGrabbableObject))
                    scrapList.Add(obj);
            return scrapList;
        }
        internal class MCQCMConfig
        {
            private readonly ConfigFile _configFile;

            public ConfigEntry<int> OutputSpacing;
            public ConfigEntry<int> Verbosity;
            public ConfigEntry<bool> IncreasingOrder;

            public MCQCMConfig(ConfigFile configFile)
            {
                _configFile = configFile;
            }

            public void RegisterOptions()
            {
                OutputSpacing = _configFile.Bind("General", "OutputSpacing", 20, new ConfigDescription("Spacing between scrap name column and scrap value column.", new AcceptableValueRange<int>(15, 30), Array.Empty<object>()));
                Verbosity = _configFile.Bind("General", "Verbosity", 0, new ConfigDescription("Verbosity level of output.", new AcceptableValueRange<int>(0, 2), Array.Empty<object>()));
                IncreasingOrder = _configFile.Bind("General", "IncreasingOrder", true, new ConfigDescription("Ordering of items of the same type."));
            }
        }
    }
}
