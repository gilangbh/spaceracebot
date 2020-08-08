using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using SixLabors.Shapes;
using MyStupidBots.Helper;

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
                log.LogInformation(JsonConvert.SerializeObject(state));
                log.LogInformation("trying to get comments 1");
                List<CommentData> comments = new List<CommentData>();

                log.LogInformation("trying to get comments 2");

                var comres = facebook.GetCommentsByPostId(state.LastPostID).Result;

                if (comres != null)
                {
                    log.LogInformation("got comment 1");
                    comments = comres.ToList();

                    log.LogInformation("got comment 2");

                    foreach (var c in comments.GroupBy(x => x.from.id).Select(x => x.FirstOrDefault()))
                    {

                        log.LogInformation("processing comment");
                        int nextRoll = random.Next(1, 7);
                        string replymsg = "";
                        bool badLuckTile = false;
                        bool goodLuckTile = false;
                        int moveSpace = 0;
                        replymsg += "You rolled " + nextRoll + ".\n";
                        Player player = new Player();
                        Player existing = state.Players.FirstOrDefault(x => x.FBID == c.from.id);
                        if (existing != null)
                        {
                            int landedAt = existing.BoardPosition + nextRoll;
                            Tile eventTile = state.Tiles.FirstOrDefault(x => x.TileNumber == landedAt);
                            if (eventTile != null)
                            {
                                moveSpace = eventTile.NumberOfTilesMoved;
                                if (eventTile.isMoveForward)
                                {
                                    goodLuckTile = true;
                                    existing.BoardPosition = landedAt + eventTile.NumberOfTilesMoved;

                                }
                                else
                                {
                                    badLuckTile = true;
                                    existing.BoardPosition = landedAt - eventTile.NumberOfTilesMoved;
                                }
                                if (existing.BoardPosition > 100) existing.BoardPosition = 100;
                                if (existing.BoardPosition < 1) existing.BoardPosition = 1;
                            }
                            else
                            {
                                existing.BoardPosition = landedAt;
                                if (existing.BoardPosition > 100) existing.BoardPosition = 100;
                                if (existing.BoardPosition < 1) existing.BoardPosition = 1;
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
                                moveSpace = eventTile.NumberOfTilesMoved;
                                if (eventTile.isMoveForward)
                                {
                                    goodLuckTile = true;
                                    player.BoardPosition = landedAt + eventTile.NumberOfTilesMoved;
                                }
                                else
                                {
                                    badLuckTile = true;
                                    player.BoardPosition = landedAt - eventTile.NumberOfTilesMoved;
                                }
                                if (player.BoardPosition > 100) player.BoardPosition = 100;
                                if (player.BoardPosition < 1) player.BoardPosition = 1;
                            }
                            else
                            {
                                player.BoardPosition = landedAt;
                                if (player.BoardPosition > 100) player.BoardPosition = 100;
                                if (player.BoardPosition < 1) player.BoardPosition = 1;
                            }

                            state.Players.Add(player);
                        }
                        if (badLuckTile)
                        {
                            replymsg += "\nUnfortunately you landed at a bad luck tile!\n";
                            replymsg += "\nYou have to move back " + moveSpace + " spaces.\n";
                        }
                        if (goodLuckTile)
                        {
                            replymsg += "\nGreat! you landed at a good luck tile!";
                            replymsg += "\nYou jump forward " + moveSpace + " spaces.\n";
                        }
                        var reply = facebook.PostComment(replymsg, c.id).Result;
                    }
                }
                else
                {
                    log.LogInformation("no comment");
                }
            }

            if (state.Players.Where(x => x.BoardPosition >= 100).Count() > 0) //announce winner and flush board
            {
                log.LogInformation("game over");
                winners.AddRange(state.Players.Where(x => x.BoardPosition >= 100));
                isGameOver = true;
            }
            string imageUrl = "";

            using (Image<Rgba32> grid = (Image<Rgba32>)Image.Load(context.FunctionAppDirectory + @"..\..\img\spaceracebot\spaceboard2.png"))
            {
                foreach (var t in state.Tiles)
                {
                    Image<Rgba32> tileImage;
                    if (t.isMoveForward)
                    {
                        tileImage = (Image<Rgba32>)Image.Load(context.FunctionAppDirectory + @"..\..\img\spaceracebot\plus.png");
                    }
                    else
                    {
                        tileImage = (Image<Rgba32>)Image.Load(context.FunctionAppDirectory + @"..\..\img\spaceracebot\minus.png");
                    }
                    Tuple<int, int> tilePosition = StupidNormalizeFunction(t.TileNumber);
                    int posX = tilePosition.Item1;
                    int posY = tilePosition.Item2;

                    var textGraphicsOptions = new TextGraphicsOptions(true);
                    var boardFont = SystemFonts.CreateFont("Trebuchet MS", 36, FontStyle.Regular);
                    RendererOptions tileOptions = new RendererOptions(boardFont, textGraphicsOptions.DpiX, textGraphicsOptions.DpiY)
                    {
                        HorizontalAlignment = HorizontalAlignment.Left,
                        TabWidth = textGraphicsOptions.TabWidth,
                        VerticalAlignment = VerticalAlignment.Center,
                        WrappingWidth = textGraphicsOptions.WrapTextWidth,
                        ApplyKerning = textGraphicsOptions.ApplyKerning
                    };

                    string tiletext = "";

                    if (t.isMoveForward)
                    {
                        tiletext = "+";
                    }
                    else
                    {
                        tiletext = "-";
                    }
                    tiletext += t.NumberOfTilesMoved.ToString();

                    var tileglyph = TextBuilder.GenerateGlyphs(tiletext, new PointF(posX * 100 + 20, posY * 100 + 50), tileOptions);

                    grid.Mutate(ctx => ctx
                        .DrawImage((tileImage), new Point(posX * 100 + 3, posY * 100 + 3), 1f)
                        .Fill((GraphicsOptions)textGraphicsOptions, Rgba32.Black, tileglyph)
                        );
                }

                List<Player> currentPlayers = state.Players.OrderByDescending(x => x.BoardPosition).ToList();
                for (int i = 0; i < currentPlayers.Count(); i++)
                {
                    //fbmessage += (i + 1) + ". " + currentPlayers[i].Name + "\n";
                    string playerRanking = (i + 1).ToString();
                    Tuple<int, int> playerPosition = StupidNormalizeFunction(currentPlayers[i].BoardPosition);
                    int posX = playerPosition.Item1;
                    int posY = playerPosition.Item2;
                    int randomizerX = random.Next(30, 70);
                    int randomizerY = random.Next(30, 70);

                    var avatar = new EllipsePolygon(posX * 100 + randomizerX, posY * 100 + randomizerY, 24, 24);
                    var border = new EllipsePolygon(posX * 100 + randomizerX, posY * 100 + randomizerY, 27, 27);

                    int r = random.Next(0, 255);
                    int g = random.Next(0, 255);
                    int b = random.Next(0, 255);
                    var color = new Rgba32((byte)r, (byte)g, (byte)b);

                    System.Console.WriteLine(color.ToHex());
                    System.Console.WriteLine("r: " + r + " g: " + g + " b: " + b);
                    grid.Mutate(ctx => ctx
                        .Fill(Rgba32.White, border)
                        .Fill(color, avatar)
                    );
                }
                MemoryStream ms = new MemoryStream();

                // grid.Save(@"C:\side\bot\spaceraceconsole\out.png");
                grid.SaveAsPng(ms);
                ms.Seek(0, SeekOrigin.Begin);

                imageUrl = BlobHelper.Upload("namebot", $"spaceracebot.png", ms);
            }

            if (!isGameOver)
            {
                fbmessage += "Current Standing:\n";
                List<Player> currentPlayers = state.Players.OrderByDescending(x => x.BoardPosition).ToList();
                for (int i = 0; i < currentPlayers.Count(); i++)
                {
                    fbmessage += (i + 1) + ". " + currentPlayers[i].Name + " [" + currentPlayers[i].BoardPosition + "]\n";
                }
                fbmessage += "\n\nLeave a comment to play.";
                var uploadres = facebook.PublishToFacebook(fbmessage, imageUrl);
                state.LastPostID = uploadres;
                log.LogInformation(uploadres);
            }
            else
            {
                fbmessage = "Game over! Crew candidates have been selected:\n\n";

                foreach (var w in winners)
                {
                    fbmessage += w.Name + "\n";
                }

                fbmessage += "\nThis is the final crew lineup:\n\n";
                fbmessage += "Commander: " + winners.PickRandom().Name + "\n";
                fbmessage += "Pilot: " + winners.PickRandom().Name + "\n";
                fbmessage += "Mission Specialist: " + winners.PickRandom().Name + "\n";
                fbmessage += "Payload Specialist: " + winners.PickRandom().Name + "\n";
                fbmessage += "Landing Crew: " + winners.PickRandom().Name + "\n";

                fbmessage += "\n";

                if (winners.Count < 5)
                {
                    fbmessage += "\nHowever, since there was not enough crew members, the mission was failed and everyone died.";
                }
                else
                {
                    fbmessage += "\nThe mission was successful, and everyone made it to the moon and back.";
                }

                var uploadres = facebook.PublishToFacebook(fbmessage, imageUrl);
                log.LogInformation(uploadres);

                state = new SpaceRaceState();
            }

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

        static Tuple<int, int> StupidNormalizeFunction(int tileNumber)
        {
            int x = 0;
            int y = 0;

            switch (tileNumber)
            {
                case 1: x = 0; y = 9; break;
                case 2: x = 1; y = 9; break;
                case 3: x = 2; y = 9; break;
                case 4: x = 3; y = 9; break;
                case 5: x = 4; y = 9; break;
                case 6: x = 5; y = 9; break;
                case 7: x = 6; y = 9; break;
                case 8: x = 7; y = 9; break;
                case 9: x = 8; y = 9; break;
                case 10: x = 9; y = 9; break;

                case 11: x = 9; y = 8; break;
                case 12: x = 8; y = 8; break;
                case 13: x = 7; y = 8; break;
                case 14: x = 6; y = 8; break;
                case 15: x = 5; y = 8; break;
                case 16: x = 4; y = 8; break;
                case 17: x = 3; y = 8; break;
                case 18: x = 2; y = 8; break;
                case 19: x = 1; y = 8; break;
                case 20: x = 0; y = 8; break;

                case 21: x = 0; y = 7; break;
                case 22: x = 1; y = 7; break;
                case 23: x = 2; y = 7; break;
                case 24: x = 3; y = 7; break;
                case 25: x = 4; y = 7; break;
                case 26: x = 5; y = 7; break;
                case 27: x = 6; y = 7; break;
                case 28: x = 7; y = 7; break;
                case 29: x = 8; y = 7; break;
                case 30: x = 9; y = 7; break;

                case 31: x = 9; y = 6; break;
                case 32: x = 8; y = 6; break;
                case 33: x = 7; y = 6; break;
                case 34: x = 6; y = 6; break;
                case 35: x = 5; y = 6; break;
                case 36: x = 4; y = 6; break;
                case 37: x = 3; y = 6; break;
                case 38: x = 2; y = 6; break;
                case 39: x = 1; y = 6; break;
                case 40: x = 0; y = 6; break;

                case 41: x = 0; y = 5; break;
                case 42: x = 1; y = 5; break;
                case 43: x = 2; y = 5; break;
                case 44: x = 3; y = 5; break;
                case 45: x = 4; y = 5; break;
                case 46: x = 5; y = 5; break;
                case 47: x = 6; y = 5; break;
                case 48: x = 7; y = 5; break;
                case 49: x = 8; y = 5; break;
                case 50: x = 9; y = 5; break;

                case 51: x = 9; y = 4; break;
                case 52: x = 8; y = 4; break;
                case 53: x = 7; y = 4; break;
                case 54: x = 6; y = 4; break;
                case 55: x = 5; y = 4; break;
                case 56: x = 4; y = 4; break;
                case 57: x = 3; y = 4; break;
                case 58: x = 2; y = 4; break;
                case 59: x = 1; y = 4; break;
                case 60: x = 0; y = 4; break;

                case 61: x = 0; y = 3; break;
                case 62: x = 1; y = 3; break;
                case 63: x = 2; y = 3; break;
                case 64: x = 3; y = 3; break;
                case 65: x = 4; y = 3; break;
                case 66: x = 5; y = 3; break;
                case 67: x = 6; y = 3; break;
                case 68: x = 7; y = 3; break;
                case 69: x = 8; y = 3; break;
                case 70: x = 9; y = 3; break;

                case 71: x = 9; y = 2; break;
                case 72: x = 8; y = 2; break;
                case 73: x = 7; y = 2; break;
                case 74: x = 6; y = 2; break;
                case 75: x = 5; y = 2; break;
                case 76: x = 4; y = 2; break;
                case 77: x = 3; y = 2; break;
                case 78: x = 2; y = 2; break;
                case 79: x = 1; y = 2; break;
                case 80: x = 0; y = 2; break;

                case 81: x = 0; y = 1; break;
                case 82: x = 1; y = 1; break;
                case 83: x = 2; y = 1; break;
                case 84: x = 3; y = 1; break;
                case 85: x = 4; y = 1; break;
                case 86: x = 5; y = 1; break;
                case 87: x = 6; y = 1; break;
                case 88: x = 7; y = 1; break;
                case 89: x = 8; y = 1; break;
                case 90: x = 9; y = 1; break;

                case 91: x = 9; y = 0; break;
                case 92: x = 8; y = 0; break;
                case 93: x = 7; y = 0; break;
                case 94: x = 6; y = 0; break;
                case 95: x = 5; y = 0; break;
                case 96: x = 4; y = 0; break;
                case 97: x = 3; y = 0; break;
                case 98: x = 2; y = 0; break;
                case 99: x = 1; y = 0; break;
                case 100: x = 0; y = 0; break;
            }

            return new Tuple<int, int>(x, y);
        }
    }
}