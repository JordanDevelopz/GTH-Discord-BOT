using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ZstdSharp.Unsafe;


namespace TornWarTracker.Data_Structures
{
    public class tornDataStructures
    {

        public class WarTally
        {
            public long warTallyID {  get; set; } // this is the ID of the war            
            public int FactionID { get; set; }
            public long TornID { get; set; }
            public int Hits { get; set; }
            public int Assists { get; set; }
            public int Interupts { get; set; }
            public double respectBest { get; set; }
            public double respectBonus { get; set; }
            public double respectGained { get; set; }
            public double respectLost { get; set; }
            public double respectNet{ get; set; }
            public double fairFight { get; set; }
            public int retalsOut { get; set; }
            public int retalsIn{ get; set; }
            public int defendsWon { get; set; }
            public int defendsInterupt { get; set; }
            public int defendsLost { get; set; }
            public int Assist { get; set; }
            public int outsideHits { get; set; }
            public int outsideRespect { get; set; }
            public int outsideLost { get; set; }
            public int outsideDefendsWon { get; set; }
            public int outsideDefendsLost { get; set; }
            public int energyUsedOut { get; set; }
            public int energyUsedIn { get; set; }
        }

        public class warDataStructures
        {
            public long WarIncome { get; set; }
            public long XanaxCost { get; set; }
            public long MedsCost { get; set; }
            public long RevivesCost { get; set; }
            public long SpiesCost { get; set; }
            public long BountiesCost { get; set; }
            public long WarPayout { get; set; }
            public int OutsideHitPrice { get; set; }
            public int AssistPrice { get; set; }
            public int PricePerHit { get; set; }

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

        #region Attacks
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

        #endregion
    }
}
