using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MyStupidBots.SpaceRaceBot
{
    public static class SpaceRaceBotTimeTrigger
    {
        static Microsoft.Azure.WebJobs.ExecutionContext context;
        static bool isFunSurprise;
        static bool isGameOver;
        static Random random;
        [FunctionName("SpaceRaceBotTimeTrigger")]
        public static void Run([TimerTrigger("0 0 * * * *")] TimerInfo myTimer, ILogger log, Microsoft.Azure.WebJobs.ExecutionContext _context)
        {
            log.LogInformation($"SpaceRace Bot Timer trigger function executed at: {DateTime.Now}");
            context = _context;

            List<Player> winners = new List<Player>();

            random = new Random();
            // isFunSurprise = true;
            isFunSurprise = random.Next(100) % 10 == 0;
            isGameOver = false;
            string accessToken = Environment.GetEnvironmentVariable("SpaceRaceBotToken");
            string page = Environment.GetEnvironmentVariable("SpaceRaceBotPage");

            Facebook facebook = new Facebook(accessToken, page);
            SpaceRaceState state = new SpaceRaceState();
            string spaceracestatepath = context.FunctionAppDirectory + @"..\..\spaceracestate.json";

            using (StreamReader sr = new StreamReader(spaceracestatepath))
            {
                while (!sr.EndOfStream)
                {
                    string data = sr.ReadLine();
                    if (!string.IsNullOrEmpty(data))
                    {
                        state = JsonConvert.DeserializeObject<SpaceRaceState>(data);
                    }
                }

                sr.Dispose();
            }

            string fbmessage = "";

            if (state.Tiles == null || state.Tiles.Count() == 0) //create new board
            {
                state.BoardSize = 100;
                state.Players = new List<Player>();
                state.Tiles = state.GetNewTiles();
            }
            else //play
            {
                List<CommentData> comments = facebook.GetCommentsByPostId(state.LastPostID).Result
                                    //.Where(c => c.message.ToLower().Equals("roll"))
                                    .ToList();
                foreach (var c in comments.GroupBy(x => x.from.id).Select(x => x.FirstOrDefault()))
                {
                    int nextRoll = random.Next(1, 7);
                    var reply = facebook.PostComment("You rolled " + nextRoll, c.id).Result;
                    Player player = new Player();
                    Player existing = state.Players.FirstOrDefault(x => x.FBID == c.from.id);
                    if (existing != null)
                    {
                        int landedAt = existing.BoardPosition + nextRoll;
                        Tile eventTile = state.Tiles.FirstOrDefault(x => x.TileNumber == landedAt);
                        if (eventTile != null)
                        {
                            if (eventTile.isMoveForward)
                            {
                                existing.BoardPosition += eventTile.NumberOfTilesMoved;
                            }
                            else
                            {
                                existing.BoardPosition -= eventTile.NumberOfTilesMoved;
                            }
                        }
                        else
                        {
                            existing.BoardPosition = landedAt;
                        }
                    }
                    else
                    {
                        player.FBID = c.from.id;
                        player.Name = c.from.name;
                        player.BoardPosition = 0;

                        int landedAt = player.BoardPosition + nextRoll;
                        Tile eventTile = state.Tiles.FirstOrDefault(x => x.TileNumber == landedAt);
                        if (eventTile != null)
                        {
                            if (eventTile.isMoveForward)
                            {
                                player.BoardPosition += eventTile.NumberOfTilesMoved;
                            }
                            else
                            {
                                player.BoardPosition -= eventTile.NumberOfTilesMoved;
                            }
                        }
                        else
                        {
                            player.BoardPosition = landedAt;
                        }

                        state.Players.Add(player);
                    }
                }
            }

            if (state.Players.Where(x => x.BoardPosition >= 100).Count() > 0) //announce winner and flush board
            {
                log.LogInformation("game over");
                winners.AddRange(state.Players.Where(x => x.BoardPosition >= 100));
                isGameOver = true;
            }

            Tuple<int, string> res;
            Tuple<int, string> commentid;

            if (!isGameOver)
            {
                fbmessage += JsonConvert.SerializeObject(state);
                res = facebook.PublishSimplePostAndGetFBPostID(fbmessage).Result;
            }
            else
            {
                fbmessage = "Game over\n\n";
                fbmessage += "Winners:\n";
                foreach (var w in winners)
                {
                    fbmessage += w.Name + "\n";
                }
                fbmessage += "\n";
                fbmessage += JsonConvert.SerializeObject(state);

                res = facebook.PublishSimplePostAndGetFBPostID(fbmessage).Result;
                state = new SpaceRaceState();
            }

            state.LastPostID = res.Item2;

            if (state.Players == null)
            {
                state.Players = new List<Player>();
                state.Tiles = new List<Tile>();
            }

            string stateserialized = JsonConvert.SerializeObject(state);

            log.LogInformation("space race state:\n");
            log.LogInformation(stateserialized);
            using (StreamWriter sw = new StreamWriter(spaceracestatepath))
            {
                sw.Write(stateserialized);
            }
        }

    }
}