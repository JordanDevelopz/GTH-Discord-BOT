using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace TornWarTracker.Data_Structures
{
    public class tornDataStructures
    {
        public class warDataStructures
        {
            public static long WarIncome { get; set; }
            public static long XanaxCost { get; set; }
            public static long MedsCost { get; set; }
            public static long RevivesCost { get; set; }
            public static long SpiesCost { get; set; }
            public static long BountiesCost { get; set; }
            public static long WarPayout { get; set; }
            public static int OutsideHitPrice { get; set; }
            public static int AssistPrice { get; set; }
            public static int PricePerHit { get; set; }

            public class FactionRankedWars
            {
                public class RankedWars
                {
                    [JsonProperty("rankedwars")]
                    public Dictionary<string, RankedWar> RankedWarsData { get; set; }

                    public RankedWars()
                    {
                        RankedWarsData = new Dictionary<string, RankedWar>();
                    }
                }

                public class RankedWar
                {
                    [JsonProperty("factions")]
                    public Dictionary<string, Faction> Factions { get; set; }

                    [JsonProperty("war")]
                    public WarInfo War { get; set; }

                    public RankedWar()
                    {
                        Factions = new Dictionary<string, Faction>();
                    }
                }

                public class Faction
                {
                    [JsonProperty("name")]
                    public string Name { get; set; }

                    [JsonProperty("score")]
                    public int Score { get; set; }

                    [JsonProperty("chain")]
                    public int Chain { get; set; }
                }

                public class WarInfo
                {
                    [JsonProperty("start")]
                    public long Start { get; set; }

                    [JsonProperty("end")]
                    public long End { get; set; }

                    [JsonProperty("target")]
                    public int Target { get; set; }

                    [JsonProperty("winner")]
                    public int Winner { get; set; }
                }
            }
        }




    }
}
