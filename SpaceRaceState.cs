using System;
using System.Collections.Generic;
using System.Linq;

namespace MyStupidBots.SpaceRaceBot
{
    public class SpaceRaceState
    {
        public int BoardSize { get; set; }
        public string LastPostID { get; set; }
        public List<Player> Players { get; set; }
        public List<Tile> Tiles { get; set; }

        public List<Tile> GetNewTiles()
        {
            Random rnd = new Random();
            List<Tile> tiles = new List<Tile>();
            while (tiles.Count < 25)
            {
                Tile tile = new Tile();
                int tileNumber = rnd.Next(5, 99);
                while (tiles.FirstOrDefault(x => x.TileNumber == tileNumber) != null)
                {
                    tileNumber = rnd.Next(5, 99);
                }
                tile.TileNumber = tileNumber;
                tile.NumberOfTilesMoved = rnd.Next(2, 15);
                tile.isMoveForward = rnd.Next() % 2 == 0;
                tile.AffectPlayer = true;
                tiles.Add(tile);
            }
            return tiles;
        }
    }

    public class Player
    {
        public string FBID { get; set; }
        public string Name { get; set; }
        public int BoardPosition { get; set; }
    }

    public class Tile
    {
        public int TileNumber { get; set; }
        public int NumberOfTilesMoved { get; set; }
        public bool isMoveForward { get; set; }
        public bool AffectPlayer { get; set; }
    }
}