using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ZstdSharp.Unsafe;


namespace TornWarTracker.Data_Structures
{
    public class tornDataStructures
    {
        public static readonly ReadOnlyCollection<string> AttackResults = new ReadOnlyCollection<string>(
       new List<string> { "Arrested", "Assist", "Attacked", "Escape", "Hospitalized", "Interrupted", "Looted", "Lost", "Mugged", "Special", "Stalemate", "Timeout" });
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
            public double respectEnemyGain { get; set; }
            public double respectEnemyLost { get; set; }
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
            public int overseas { get; set; }
            public int energyUsedOut { get; set; }
            public int energyUsedIn { get; set; }
            public int hospd { get; set; }
            public int mugged { get; set; }
            public int leave { get; set; }
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
            [JsonProperty("code")]
            public string Code { get; set; }

            [JsonProperty("timestamp_started")]
            public long TimestampStarted { get; set; }

            [JsonProperty("timestamp_ended")]
            public long TimestampEnded { get; set; }

            [JsonProperty("attacker_id")]
            public int AttackerId { get; set; }

            [JsonProperty("attacker_name")]
            public string AttackerName { get; set; }

            [JsonProperty("attacker_faction")]
            public int AttackerFaction { get; set; }

            [JsonProperty("attacker_factionname")]
            public string AttackerFactionName { get; set; }

            [JsonProperty("defender_id")]
            public int DefenderId { get; set; }

            [JsonProperty("defender_name")]
            public string DefenderName { get; set; }

            [JsonProperty("defender_faction")]
            public int DefenderFaction { get; set; }

            [JsonProperty("defender_factionname")]
            public string DefenderFactionName { get; set; }

            [JsonProperty("result")]
            public string Result { get; set; }

            [JsonProperty("stealthed")]
            public int Stealthed { get; set; }

            [JsonProperty("respect")]
            public double Respect { get; set; }

            [JsonProperty("chain")]
            public int Chain { get; set; }

            [JsonProperty("raid")]
            public int Raid { get; set; }

            [JsonProperty("ranked_war")]
            public int RankedWar { get; set; }

            [JsonProperty("respect_gain")]
            public double RespectGain { get; set; }

            [JsonProperty("respect_loss")]
            public double RespectLoss { get; set; }

            [JsonProperty("modifiers")]
            public Modifiers Modifiers { get; set; }
        }

        public class Modifiers
        {
            [JsonProperty("fair_fight")]
            public double FairFight { get; set; }

            [JsonProperty("war")]
            public int War { get; set; }

            [JsonProperty("retaliation")]
            public int Retaliation { get; set; }

            [JsonProperty("group_attack")]
            public int GroupAttack { get; set; }

            [JsonProperty("overseas")]
            public int Overseas { get; set; }

            [JsonProperty("chain_bonus")]
            public int ChainBonus { get; set; }
        }

        public class Attacks
        {
            [JsonProperty("attacks")]
            public Dictionary<long, Attack> AttackList { get; set; }
        }

        #endregion
    }
}
