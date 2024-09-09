using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;


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


        public class Attack
        {
            [JsonPropertyName("code")]
            public string Code { get; set; }

            [JsonPropertyName("timestamp_started")]
            public long TimestampStarted { get; set; }

            [JsonPropertyName("timestamp_ended")]
            public long TimestampEnded { get; set; }

            [JsonPropertyName("attacker_id")]
            public int AttackerId { get; set; }

            [JsonPropertyName("attacker_name")]
            public string AttackerName { get; set; }

            [JsonPropertyName("attacker_faction")]
            public int AttackerFaction { get; set; }

            [JsonPropertyName("attacker_factionname")]
            public string AttackerFactionName { get; set; }

            [JsonPropertyName("defender_id")]
            public int DefenderId { get; set; }

            [JsonPropertyName("defender_name")]
            public string DefenderName { get; set; }

            [JsonPropertyName("defender_faction")]
            public int DefenderFaction { get; set; }

            [JsonPropertyName("defender_factionname")]
            public string DefenderFactionName { get; set; }

            [JsonPropertyName("result")]
            public string Result { get; set; }

            [JsonPropertyName("stealthed")]
            public int Stealthed { get; set; }

            [JsonPropertyName("respect")]
            public double Respect { get; set; }

            [JsonPropertyName("chain")]
            public int Chain { get; set; }

            [JsonPropertyName("raid")]
            public int Raid { get; set; }

            [JsonPropertyName("ranked_war")]
            public int RankedWar { get; set; }

            [JsonPropertyName("respect_gain")]
            public double RespectGain { get; set; }

            [JsonPropertyName("respect_loss")]
            public double RespectLoss { get; set; }

            [JsonPropertyName("modifiers")]
            public Modifiers Modifiers { get; set; }
        }

        public class Modifiers
        {
            [JsonPropertyName("fair_fight")]
            public double FairFight { get; set; }

            [JsonPropertyName("war")]
            public int War { get; set; }

            [JsonPropertyName("retaliation")]
            public int Retaliation { get; set; }

            [JsonPropertyName("group_attack")]
            public int GroupAttack { get; set; }

            [JsonPropertyName("overseas")]
            public int Overseas { get; set; }

            [JsonPropertyName("chain_bonus")]
            public int ChainBonus { get; set; }
        }

        public class Attacks
        {
            [JsonPropertyName("attacks")]
            public Dictionary<string, Attack> AttackList { get; set; }
        }

    }
}
