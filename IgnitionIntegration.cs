using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Net;
using System.Globalization;
using Oxide;
using Oxide.Core;
using UnityEngine;
using System.Reflection;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using Rust;
using Facepunch;
using System.Linq;
using Oxide.Core.Libraries.Covalence;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core.Database;
using Oxide.Core.Configuration;
using MySql.Data.MySqlClient;
using System.Timers;

namespace Oxide.Plugins
{
    [Info("TXSN Ignition Integration", "Stuart R. Durning", "0.1.0")]
    [Description("Integrations")]
    class IgnitionIntegration : CovalencePlugin
    {
        string prefix = "<size=18><color=#ff5300>[Ignition OXRI]:#</color>";
        private void Init()
        {
            Puts("Ignition Integration: Init()");
            broadcastToAll($"{prefix} <color=red>Recompile Complete</color>");
            xpGrantTimerInit();
            LotteryTimerInit();
        } 
        private void broadcastToAll(string message)
        {
            foreach (var p in players.Connected)
            {
                p.Message($"{message}");
            }
        }
        object OnPlayerSleepEnded(BasePlayer player)
        {
            string discordId = getDiscord(player.IPlayer);
            int memberLevel = 0;
            
            if (Member(discordId))
            {
                memberLevel = 1;
            }
            if (Moderator(discordId))
            {
                memberLevel = 2;
            }
            if (CoreMember(discordId))
            {
                memberLevel = 3;
            }

            if (memberLevel > 0)
            {
                string nucName = MySQLSingle($"SELECT name FROM txsn.members WHERE did = {discordId}");
                string txsnLevel = MySQLSingle($"SELECT level FROM txsn.discorduser WHERE userid = {discordId}");
                switch (memberLevel)
                {
                    case 1: 
                        player.IPlayer.Rename($"[LVL{txsnLevel}] tS.M | {nucName}");
                        break;
                    case 2:
                        player.IPlayer.Rename($"[LVL{txsnLevel}] tS.MOD | {nucName}");
                        break;
                    case 3:
                        player.IPlayer.Rename($"[LVL{txsnLevel}] tS.COR | {nucName}");
                        break;
                }
            }
            return player;
        }
        public void sendToDiscord(string name, string message)
        {
            webrequest.Enqueue("https://discord.com/api/webhooks/797906599041302568/VvVjEwM2wBHQVDaU1RvV_Vl_k656WzOUDNJtPOcib4rW0tlwGooJGWUKxWgpbhOXEOCH", $"username={name}&content={message}", (code, response) =>
            {
            }, this, RequestMethod.POST);
        }
        public void sendToGeneralDiscord(string name, string message)
        {
            webrequest.Enqueue("https://discord.com/api/webhooks/797589551480307754/qm9NS94sdULSE_BfCsvwCp2lOWnuqm3OAiTEQmQp6DtiMRuWJUZbhA_JDJp46XyC6iIl", $"username={name}&content={message}", (code, response) =>
            {
            }, this, RequestMethod.POST);
        }
        object OnUserChat(IPlayer player, string message)
        {
            sendToDiscord(player.Name, message);
            return null;
        }
        private void OnPlayerDeath(BasePlayer victim, HitInfo hitInfo)
        {
            if (victim == null) return; //If Entity Died.

            var attacker = hitInfo.InitiatorPlayer;

            if(victim == attacker)
            {
                return;
            }

            var distance = !hitInfo.IsProjectile() ? (int)Vector3.Distance(hitInfo.PointStart, hitInfo.HitPositionWorld) : (int)hitInfo.ProjectileDistance;

            sendToGeneralDiscord("RUST Kill", $"***RUST KILL: {attacker.IPlayer.Name} killed {victim.IPlayer.Name} from {distance}m away.***");
            if (getDiscord(attacker.IPlayer) != "0")
            {
                string aDID = getDiscord(attacker.IPlayer);
                grantXP(aDID, 300, $"Rust Kill on {victim.IPlayer.Name}");
                string exists = MySQLSingle($"SELECT COUNT(*) from txsn.rustleaderboard WHERE did = {aDID}");
                if (exists == "0")
                {
                    MySQLCommand($"INSERT INTO `txsn`.`rustleaderboard` (`did`, `kills`) VALUES('{aDID}', '0')");
                }
                int numKills = Convert.ToInt32(MySQLSingle($"SELECT kills from txsn.rustleaderboard WHERE did = {aDID}"));
                numKills++;
                MySQLCommand($"UPDATE `txsn`.`rustleaderboard` SET `kills` = '{numKills}' WHERE (`did` = '{aDID}');");
                attacker.IPlayer.Message($"{prefix} 200xp added for kill, you have {numKills} kills total.");
            } else
            {
                attacker.IPlayer.Message($"{prefix} As you've not linked your STEAMID64, you've lost out on XP.");
            }
        }

        protected void xpGrantTimerInit()
        {
            string dID = "";
            timer.Repeat(900, 0, () =>
            {
                foreach (var p in players.Connected)
                {
                    dID = getDiscord(p);
                    if(Member(dID))
                    {
                        grantXP(dID, 50, "RUST Server Activity");
                        p.Message($"{prefix} <color=#fab81a>50XP has been added to your TXSN account! (1.8x if Nitro Booster)</color>");
                    } else
                    {
                        p.Message($"{prefix} <color=red>You've not linked your account and missed out on free XP!</color>");
                    }
                    
                }
            });
        }
        protected void LotteryTimerInit()
        {
            string dID = "";
            
            timer.Repeat(3600, 0, () =>
            {
                var validPlayers = new List<IPlayer>();
                int pos = 0;
                if (players.Connected.Count() < 2) {
                    return; 
                }
                System.Random rnd = new System.Random();
                int tX = rnd.Next(1, 40);
                int XP = rnd.Next(1, 950);
                broadcastToAll($"{prefix} Lottery Time! The Reward is: {tX}tX and {XP}XP");
                timer.Once(10, () =>
                {
                    
                    foreach (var p in players.Connected)
                    {
                        try
                        {
                            dID = getDiscord(p);
                            if (dID != "NOT FOUND")
                            {
                                validPlayers.Add(p);
                                pos++; 
                            }
                        } 
                        catch 
                        {
                            continue;
                        }
                    }
                    broadcastToAll($"{prefix} All connected TXSN members have a chance to win. The odds are 1 out of {validPlayers.Count}!");
                    timer.Once(10, () =>
                    {
                        broadcastToAll($"{prefix} And the winner is...");
                        timer.Once(10, () =>
                        {
                            int randomPicker = rnd.Next(0, validPlayers.Count);

                            broadcastToAll($"{prefix} {validPlayers[randomPicker].Name}!");
                            string winnerID = getDiscord(validPlayers[randomPicker]);
                            grantXP(winnerID, XP, "RUST Lottery Win");
                            GiveTXSNCoins(winnerID, tX);
                        });
                    });
                });
                
            });
        }

        string getDiscord(IPlayer player)
        {
            try
            {
                string did = MySQLSingle($"SELECT did FROM txsn.members WHERE steamid64 = '{player.Id}'");
                if(did == "")
                {
                    return "0";
                }
                return did;
            }
            catch (Exception ex)
            {
                broadcastToAll($"{ex.Message}");
                return "0";
            }
        }
        bool grantXP(string did, int amount, string reason)
        {
            try
            {
                MySQLCommand($"INSERT INTO `txsn`.`tasks` (`taskname`, `assocuser`, `arg2`, `arg3`, `timeaction`) VALUES ('grantxp', '{did}', '{amount}', '{reason}', '0');");
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }
        void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        { 
            if (hitInfo == null) return;
            if (hitInfo.Initiator == null) return;
            if (entity == null) return;

            var attacker = hitInfo.InitiatorPlayer;

            System.Random rnd = new System.Random();
            int Chance = rnd.Next(1, 100);
            int Chance2 = rnd.Next(1, 15);
            int Chance3 = rnd.Next(1, 100);

            Puts(entity.GetType().ToString());

            if (entity.GetType().ToString() == "LootContainer")
            {
                if (getDiscord(attacker.IPlayer) != "0")
                {
                    if (Chance > 95)
                    {
                        GiveTXSNCoins(getDiscord(attacker.IPlayer), Chance2);
                        attacker.IPlayer.Message($"{prefix} Congratulations! You found {Chance2} TXSNCoins and {Chance3} XP in the container.");
                        string aDID = getDiscord(attacker.IPlayer);
                        grantXP(aDID, Chance3, $"Rust Barrel Find");
                    }
                }
                else
                { 
                    attacker.IPlayer.Message($"{prefix} As you've not linked your STEAMID64, you've lost out on XP.");
                }
            }
            if (entity.GetType().ToString() == "Boar") 
            {
                if (getDiscord(attacker.IPlayer) != "0")
                {
                    if (Chance > 70)
                    {
                        GiveTXSNCoins(getDiscord(attacker.IPlayer), 5);
                        attacker.IPlayer.Message($"{prefix} Congratulations! You got 5 TXSNCoins for your entity kill.");
                    }
                    string aDID = getDiscord(attacker.IPlayer);
                    grantXP(aDID, 5, $"Rust Ent Kill on Boar");
                    string exists = MySQLSingle($"SELECT COUNT(*) from txsn.rustentleaderboard WHERE did = {aDID}");
                    if (exists == "0")
                    {
                        MySQLCommand($"INSERT INTO `txsn`.`rustentleaderboard` (`did`, `kills`) VALUES('{aDID}', '0')");
                    }
                    int numKills = Convert.ToInt32(MySQLSingle($"SELECT kills from txsn.rustentleaderboard WHERE did = {aDID}"));
                    numKills++; 
                    MySQLCommand($"UPDATE `txsn`.`rustentleaderboard` SET `kills` = '{numKills}' WHERE (`did` = '{aDID}');");
                    attacker.IPlayer.Message($"{prefix} 5xp added for boar kill, you have {numKills} entity kills total.");
                }
                else
                {
                    attacker.IPlayer.Message($"{prefix} As you've not linked your STEAMID64, you've lost out on XP.");
                }
            }
            if (entity.GetType().ToString() == "Wolf")
            {
                if (getDiscord(attacker.IPlayer) != "0")
                {
                    if (Chance > 70)
                    {
                        GiveTXSNCoins(getDiscord(attacker.IPlayer), 5);
                        attacker.IPlayer.Message($"{prefix} Congratulations! You got 5 TXSNCoins for your entity kill.");
                    }
                    string aDID = getDiscord(attacker.IPlayer);
                    grantXP(aDID, 5, $"Rust Ent Kill on Wolf");
                    string exists = MySQLSingle($"SELECT COUNT(*) from txsn.rustentleaderboard WHERE did = {aDID}");
                    if (exists == "0")
                    {
                        MySQLCommand($"INSERT INTO `txsn`.`rustentleaderboard` (`did`, `kills`) VALUES('{aDID}', '0')");
                    }
                    int numKills = Convert.ToInt32(MySQLSingle($"SELECT kills from txsn.rustentleaderboard WHERE did = {aDID}"));
                    numKills++;
                    MySQLCommand($"UPDATE `txsn`.`rustentleaderboard` SET `kills` = '{numKills}' WHERE (`did` = '{aDID}');");
                    attacker.IPlayer.Message($"{prefix} 5xp added for Wolf kill, you have {numKills} entity kills total.");
                }
                else
                {
                    attacker.IPlayer.Message($"{prefix} As you've not linked your STEAMID64, you've lost out on XP.");
                }
            }
            if (entity.GetType().ToString() == "Stag")
            {
                if (getDiscord(attacker.IPlayer) != "0")
                {
                    if (Chance > 70)
                    {
                        GiveTXSNCoins(getDiscord(attacker.IPlayer), 5);
                        attacker.IPlayer.Message($"{prefix} Congratulations! You got 5 TXSNCoins for your entity kill.");
                    }
                    string aDID = getDiscord(attacker.IPlayer);
                    grantXP(aDID, 5, $"Rust Ent Kill on Stag");
                    string exists = MySQLSingle($"SELECT COUNT(*) from txsn.rustentleaderboard WHERE did = {aDID}");
                    if (exists == "0")
                    {
                        MySQLCommand($"INSERT INTO `txsn`.`rustentleaderboard` (`did`, `kills`) VALUES('{aDID}', '0')");
                    }
                    int numKills = Convert.ToInt32(MySQLSingle($"SELECT kills from txsn.rustentleaderboard WHERE did = {aDID}"));
                    numKills++;
                    MySQLCommand($"UPDATE `txsn`.`rustentleaderboard` SET `kills` = '{numKills}' WHERE (`did` = '{aDID}');");
                    attacker.IPlayer.Message($"{prefix} 5xp added for Stag kill, you have {numKills} entity kills total.");
                }
                else
                {
                    attacker.IPlayer.Message($"{prefix} As you've not linked your STEAMID64, you've lost out on XP.");
                }
            }
            if (entity.GetType().ToString() == "Horse")
            {
                if (getDiscord(attacker.IPlayer) != "0")
                {
                    if (Chance > 70)
                    {
                        GiveTXSNCoins(getDiscord(attacker.IPlayer), 5);
                        attacker.IPlayer.Message($"{prefix} Congratulations! You got 5 TXSNCoins for your entity kill.");
                    }
                    string aDID = getDiscord(attacker.IPlayer);
                    grantXP(aDID, 5, $"Rust Ent Kill on Horse");
                    string exists = MySQLSingle($"SELECT COUNT(*) from txsn.rustentleaderboard WHERE did = {aDID}");
                    if (exists == "0")
                    {
                        MySQLCommand($"INSERT INTO `txsn`.`rustentleaderboard` (`did`, `kills`) VALUES('{aDID}', '0')");
                    }
                    int numKills = Convert.ToInt32(MySQLSingle($"SELECT kills from txsn.rustentleaderboard WHERE did = {aDID}"));
                    numKills++;
                    MySQLCommand($"UPDATE `txsn`.`rustentleaderboard` SET `kills` = '{numKills}' WHERE (`did` = '{aDID}');");
                    attacker.IPlayer.Message($"{prefix} 5xp added for Horse kill, you have {numKills} entity kills total.");
                }
                else
                {
                    attacker.IPlayer.Message($"{prefix} As you've not linked your STEAMID64, you've lost out on XP.");
                }
            }
            if (entity.GetType().ToString() == "Bear")
            {
                if (getDiscord(attacker.IPlayer) != "0")
                {
                    if (Chance > 70)
                    {
                        GiveTXSNCoins(getDiscord(attacker.IPlayer), 5);
                        attacker.IPlayer.Message($"{prefix} Congratulations! You got 5 TXSNCoins for your entity kill.");
                    }
                    string aDID = getDiscord(attacker.IPlayer);
                    grantXP(aDID, 12, $"Rust Ent Kill on Bear");
                    string exists = MySQLSingle($"SELECT COUNT(*) from txsn.rustentleaderboard WHERE did = {aDID}");
                    if (exists == "0")
                    {
                        MySQLCommand($"INSERT INTO `txsn`.`rustentleaderboard` (`did`, `kills`) VALUES('{aDID}', '0')");
                    }
                    int numKills = Convert.ToInt32(MySQLSingle($"SELECT kills from txsn.rustentleaderboard WHERE did = {aDID}"));
                    numKills++;
                    MySQLCommand($"UPDATE `txsn`.`rustentleaderboard` SET `kills` = '{numKills}' WHERE (`did` = '{aDID}');");
                    attacker.IPlayer.Message($"{prefix} 12xp added for Bear kill, you have {numKills} entity kills total.");
                }
                else
                {
                    attacker.IPlayer.Message($"{prefix} As you've not linked your STEAMID64, you've lost out on XP.");
                }
            }
            if (entity.GetType().ToString() == "Scientist")
            {
                if (getDiscord(attacker.IPlayer) != "0")
                {
                    if (Chance > 90)
                    {
                        GiveTXSNCoins(getDiscord(attacker.IPlayer), 5);
                        attacker.IPlayer.Message($"{prefix} Congratulations! You got 5 TXSNCoins for your entity kill.");
                    }
                    string aDID = getDiscord(attacker.IPlayer);
                    grantXP(aDID, 18, $"Rust Ent Kill on Scientist");
                    string exists = MySQLSingle($"SELECT COUNT(*) from txsn.rustentleaderboard WHERE did = {aDID}");
                    if (exists == "0")
                    {
                        MySQLCommand($"INSERT INTO `txsn`.`rustentleaderboard` (`did`, `kills`) VALUES('{aDID}', '0')");
                    }
                    int numKills = Convert.ToInt32(MySQLSingle($"SELECT kills from txsn.rustentleaderboard WHERE did = {aDID}"));
                    numKills++;
                    MySQLCommand($"UPDATE `txsn`.`rustentleaderboard` SET `kills` = '{numKills}' WHERE (`did` = '{aDID}');");
                    attacker.IPlayer.Message($"{prefix} 18xp added for Scientist kill, you have {numKills} entity kills total.");
                }
                else
                {
                    attacker.IPlayer.Message($"{prefix} As you've not linked your STEAMID64, you've lost out on XP.");
                }
            }
            if (entity.GetType().ToString() == "bradleyapc")
            {
                if (getDiscord(attacker.IPlayer) != "0")
                {
                    attacker.IPlayer.Message($"{prefix} Congratulations! You got 15 TXSNCoins for your Bradley kill.");
                    string aDID = getDiscord(attacker.IPlayer);
                    grantXP(aDID, 100, $"Rust Kill on Bradley APC");
                    GiveTXSNCoins(aDID, 15);
                    string exists = MySQLSingle($"SELECT COUNT(*) from txsn.rustentleaderboard WHERE did = {aDID}");
                    if (exists == "0")
                    {
                        MySQLCommand($"INSERT INTO `txsn`.`rustentleaderboard` (`did`, `kills`) VALUES('{aDID}', '0')");
                    }
                    int numKills = Convert.ToInt32(MySQLSingle($"SELECT kills from txsn.rustentleaderboard WHERE did = {aDID}"));
                    numKills++;
                    MySQLCommand($"UPDATE `txsn`.`rustentleaderboard` SET `kills` = '{numKills}' WHERE (`did` = '{aDID}');");
                    attacker.IPlayer.Message($"{prefix} 100xp added for Scientist kill, you have {numKills} entity kills total.");
                }
                else
                {
                    attacker.IPlayer.Message($"{prefix} As you've not linked your STEAMID64, you've lost out on XP.");
                }
            }

            
            return;
        }
        static public string MySQLSingle(string query)
        {
            string Single = "NOT FOUND";
            query = query.Replace("'", "\'");
            string Query = query;
            string ConnString = "datasource=localhost;port=3306;username=1313;password=1313";
            MySqlConnection Conn = new MySqlConnection(ConnString);
            MySqlCommand cmd = new MySqlCommand(Query, Conn);

            Conn.Open();
            object result = cmd.ExecuteScalar();

                if (result != null)
                {
                    return Single = Convert.ToString(result);
                }

            Conn.Close();
            return Single;
        }
        static public bool Member(string did)
        {
            MySqlConnection Conn = new MySqlConnection("datasource=localhost;port=3306;username=1313;password=1313");
            string Query = $"SELECT * FROM txsn.memberlist WHERE userid = '{did}' ";
            MySqlCommand WarnReader = new MySqlCommand(Query, Conn);
            MySqlDataReader reader;
            Conn.Open();
            reader = WarnReader.ExecuteReader();
            while (reader.Read())
            {
                if (reader["userid"].ToString() == did.ToString())
                {
                    Conn.Close();
                    return true;
                }
            }
            Conn.Close();
            return false;
        }
        static public bool CoreMember(string did)
        {
            MySqlConnection Conn = new MySqlConnection("datasource=localhost;port=3306;username=1313;password=1313");
            string Query = $"SELECT * FROM txsn.corememberlist WHERE userid = '{did}' ";
            MySqlCommand WarnReader = new MySqlCommand(Query, Conn);
            MySqlDataReader reader;
            Conn.Open();
            reader = WarnReader.ExecuteReader();
            while (reader.Read())
            {
                if (reader["userid"].ToString() == did.ToString())
                {
                    Conn.Close();
                    return true;
                }
            }
            Conn.Close();
            return false;
        }
        static public bool Moderator(string did)
        {
            MySqlConnection Conn = new MySqlConnection("datasource=localhost;port=3306;username=1313;password=1313");
            string Query = $"SELECT * FROM txsn.modlist WHERE userid = '{did}' ";
            MySqlCommand WarnReader = new MySqlCommand(Query, Conn);
            MySqlDataReader reader;
            Conn.Open();
            reader = WarnReader.ExecuteReader();
            while (reader.Read())
            {
                if (reader["userid"].ToString() == did.ToString())
                {
                    Conn.Close();
                    return true;
                }
            }
            Conn.Close();
            return false;
        }
        static public bool MySQLCommand(string query)
        {
            try
            {
                query = query.Replace("'", "\'");
                string Query1 = query;
                string ConnString1 = "datasource=localhost;port=3306;username=1313;password=1313";
                MySqlConnection Conn1 = new MySqlConnection(ConnString1);
                MySqlCommand cmd1 = new MySqlCommand(Query1, Conn1);

                Conn1.Open();
                object result1 = cmd1.ExecuteScalar();
                if (result1 != null)
                {
                    Conn1.Close();
                    return true;

                }
                else
                {
                    Conn1.Close();
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        static public bool GiveTXSNCoins(string dID, int amount)
        {
            int currenttxsncoin = Convert.ToInt32(MySQLSingle($"SELECT txsncoin FROM txsn.discorduser WHERE userid = {dID}"));
            currenttxsncoin = currenttxsncoin + amount;
            MySQLCommand($"UPDATE `txsn`.`discorduser` SET `txsncoin` = '{currenttxsncoin}' WHERE (`userid` = '{dID}');");
            return true;
        }
    }
}