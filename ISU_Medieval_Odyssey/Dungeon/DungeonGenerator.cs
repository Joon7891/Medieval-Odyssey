﻿/* Author: Steven Ung
// File Name: DungeonGenerator.cs
// Project Name: ISU_Medieval_Odyssey
// Creation Date: 01/15/2019
// Modified Date: 01/20/2019
// Description: Room and passage-dungeon generation using the following steps:
1) Place x rooms with random size and position - with no overlaps
2) Fill remaining space with mazes
3) Every tiles between two unconnected region is marked
4) randomly chose marked tiles and make them open
5) remove dead ends by removing every tile that touches 3 walls.
*/

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace ISU_Medieval_Odyssey
{
    class DungeonGenerator
    {
        const int ROOM_ATTEMPTS = 500;

        /// <summary>
        /// Higher = bigger rooms
        /// </summary>
        /// 
        const int SIZE_MODIFIER = 0;

        /// <summary>
        /// Max dimentions of the dungeon.
        /// </summary>
        const int MAX_WIDTH = 501;
        const int MAX_HEIGHT = 501;
        
        /// <summary>
        /// Indicates the extra chance that the maze will continue in the same direction 
        /// </summary>
        const int DIRECTION_CHANCE = 0;

        /// <summary>
        /// Indicates the extra chance that the maze will have more than one connector for any two+ regions
        /// </summary>
        const int CONNECTION_CHANCE = 30;

        /// <summary>
        /// Direction vector2ints that can be added to another vector2int to move in that direction
        /// Using same numbering as the direction enum.
        /// </summary>
        private readonly Vector2Int[] MoveDirections =
        {
            new Vector2Int(0, -1),
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(-1, 0)
        };

        Random rng;

        CollisionTree collisionTree;

        // The rooms of the dungeon
        List<Rectangle> rooms;

        // What region each tile is in, -1 means inpassable
        int[,] region;

        // The region that is currently being generated 
        int currentRegion;

        public DungeonGenerator()
        {
            rng = new Random();
            collisionTree = new CollisionTree(new Rectangle(0, 0, MAX_WIDTH, MAX_HEIGHT));
            region = new int[MAX_WIDTH, MAX_HEIGHT];
            currentRegion = -1;

            for(int i = 0; i < MAX_WIDTH; i++)
            {
                for(int j = 0; j < MAX_HEIGHT; i++)
                {
                    region[i, j] = -1;
        
                }
            }
        }

        /// <summary>
        /// Creates randomly sized and positioned rooms with no overlaps. Rooms are odd sized and odd centered.
        /// </summary>
        private void GenerateRooms()
        {
            List<CollisionRectangle> roomRectangles = new List<CollisionRectangle>();
            for(int i = 0; i < ROOM_ATTEMPTS; i++)
            {
                // ensure room dimensions are odd
                int size = rng.Next(1, 3) * 2 + 1;
                int width = size;
                int height = size;

                // make the room less perfect
                if(rng.Next(0,2) == 0)
                {
                    width += 2 * rng.Next(0, 1 + size / 2);
                }
                else
                {
                    height += 2 * rng.Next(0, 1 + size / 2);
                }

                // align room with odd coordinate
                int x = rng.Next(0, MAX_HEIGHT) * 2 + 1;
                int y = rng.Next(0, MAX_WIDTH) * 2 + 1;

                Rectangle room = new Rectangle(x, y, width, height);

                if(collisionTree.GetCollisions(room, roomRectangles).Count != 0)
                {
                    continue;
                }

                rooms.Add(room);
                roomRectangles.Add(new CollisionRectangle(room));

                currentRegion++;

                for(int j = room.X; j <= room.X + room.Width; j++)
                {
                    for(int k = room.Y; k <= room.Y + room.Height; k++)
                    {
                        region[i,j] = currentRegion;
                    }
                }
            }

        }

        /// <summary>
        /// Changes the region in the tile some offset away from the given tile to the current region
        /// </summary>
        /// <param name="cur"> the initial tile </param>
        /// <param name="offset"> an offset that determines the tile to change </param>
        private void ChangeRegion(Vector2Int cur, Vector2Int offset)
        {
            region[(offset + cur).X, (offset + cur).Y] = currentRegion;
        }

        /// <summary>
        /// Expands on a maze path
        /// </summary>
        /// <param name="current"> the tile to start on </param>
        private void ExpandMaze(Vector2Int current) // dfs style path finding to create maze
        {
            currentRegion++;

            region[current.X, current.Y] = currentRegion;

            int lastDirection = -1;
            Stack<Vector2Int> stack = new Stack<Vector2Int>();
            stack.Push(current);

            while(stack.Count > 0)
            {
                current = stack.Peek();
                List<int> openDirections = new List<int>(); 

                for(int i = 0; i < 4; i++)
                {
                    if(region[(MoveDirections[i] * 2 + current).X, (MoveDirections[i] * 2 + current).Y] == -1)
                    {
                        openDirections.Add(i);
                    }
                }

                if (openDirections.Count > 0)
                {
                    int nextDirection = -1;

                    if (openDirections.Contains(lastDirection))
                    {
                        if (rng.Next(0, 101) > DIRECTION_CHANCE)
                        {
                            nextDirection = lastDirection;
                        }
                    }
                    else
                    {
                        nextDirection = openDirections[rng.Next(0, openDirections.Count)];
                    }

                    // create path twice in the chosen direction, because rooms and initial paths can only be odd-numbered
                    ChangeRegion(current, MoveDirections[nextDirection]);
                    ChangeRegion(current, MoveDirections[nextDirection] * 2);

                    stack.Push(current + MoveDirections[nextDirection] * 2);
                    lastDirection = nextDirection;
                }
                else
                {
                    lastDirection = -1;
                    stack.Pop();
                }
            }

        }

        /// <summary>
        /// Connects all the rooms and mazes such that there are multiple paths between rooms
        /// </summary>
        private void ConnectRegions()
        {
            currentRegion++;
            // create disjoint set for spanning tree
            DisjointSet disjointSet = new DisjointSet(rooms.Count);

            // List of all tiles that are currently impassible and touch 2+ regions
            // These are stored as edges between two of the regions, and used to construct an imperfect spanning tree
            List<Tuple<Vector2Int, int, int>> connectors = new List<Tuple<Vector2Int, int, int>>();

            // the connectors that are used
            List<Tuple<Vector2Int, int, int>> connections = new List<Tuple<Vector2Int, int, int>>();

            for (int i = 1; i < MAX_WIDTH-1; i++)
            {
                for(int j = 1; j < MAX_HEIGHT-1; j++)
                {
                    if (region[i, j] == -1)
                    {
                        List<int> regions = new List<int>();
                        Vector2Int current = new Vector2Int(i, j);
                        
                        for(int k = 0; k < 4; k++)
                        {
                            Vector2Int check = current + MoveDirections[k];
                            if(!regions.Contains(region[check.X, check.Y])){
                                regions.Add(region[check.X, check.Y]);
                            }
                        }

                        if(regions.Count >= 2)
                        {
                            connectors.Add(new Tuple<Vector2Int, int, int> (current, regions[0], regions[1]));
                        }
                    }
                }
            }

            int mainRoom = rng.Next(0, rooms.Count);

            // construct connected graph from the connectors
            foreach (Tuple<Vector2Int, int, int> connector in connectors)
            {
                if (disjointSet.Find(connector.Item2) != disjointSet.Find(connector.Item3))
                {
                    disjointSet.Union(connector.Item2, connector.Item3);
                    connections.Add(connector);
                }
            }

            // carve out connectors
            foreach(Tuple<Vector2Int, int, int> connector in connections)
            {
                region[connector.Item1.X, connector.Item1.Y] = currentRegion;
            }

            // remove connectors that no longer connect
            foreach(Tuple<Vector2Int, int, int> connector in connectors)
            {
                int x = connector.Item1.X;
                int y = connector.Item1.Y;

                if (region[x, y] == -1)
                {
                    List<int> regions = new List<int>();
                    Vector2Int current = new Vector2Int(x, y);

                    for (int k = 0; k < 4; k++)
                    {
                        Vector2Int check = current + MoveDirections[k];
                        if (!regions.Contains(region[check.X, check.Y]))
                        {
                            regions.Add(region[check.X, check.Y]);
                        }
                    }

                    if (regions.Count < 2 || region[x, y] != -1)
                    {
                        connectors.Remove(connector);
                    }
                }
            }

            // add extra connectors
            foreach(Tuple<Vector2Int, int, int> connector in connectors)
            {
                if(rng.Next(0,101) <= CONNECTION_CHANCE)
                {
                    region[connector.Item1.X, connector.Item1.Y] = currentRegion;
                }
            }
        }

        /// <summary>
        /// Fills in all tiles that have walls on 3 sides
        /// </summary>
        private void FillDeadEnds()
        {
            for (int i = 1; i < MAX_WIDTH - 1; i++)
            {
                for (int j = 1; j < MAX_HEIGHT - 1; j++)
                {
                    if(region[i,j] == -1)
                    {
                        continue;
                    }

                    Queue<Vector2Int> queue = new Queue<Vector2Int>();
                    queue.Enqueue(new Vector2Int(i, j));

                    while (queue.Count > 0)
                    {
                        Vector2Int current = queue.Dequeue();
                        int walls = 0;

                        for (int k = 0; k < 4; k++)
                        {
                            Vector2Int check = current + MoveDirections[k];
                            if (region[check.X, check.Y] == -1)
                            {
                                walls++;
                            }
                        }

                        if(walls >= 3)
                        {
                            region[current.X, current.Y] = -1;
                            for (int k = 0; k < 4; k++)
                            {
                                Vector2Int check = current + MoveDirections[k];
                                queue.Enqueue(check);
                            }
                        }

                    }
                }
            }
        }

        /// <summary>
        /// Generates mazes around the entire dungeon
        /// </summary>
        private void CreateMazes()
        {
            for(int i = 1; i < MAX_WIDTH; i+=2)
            {
                for(int j = 1; j < MAX_HEIGHT; j+=2)
                {
                    if (region[i,j] == -1)  
                    {
                        ExpandMaze(new Vector2Int(i,j));
                    }
                }
            }
        }

        /// <summary>
        /// Generates a tile based dungeon
        /// </summary>
        /// <returns> Returns a 2d array that contains dungeon data. -1 indicates impassible tiles and any other number indicates a seperate room </returns>
        public int[,] GenerateDungeon()
        {
            GenerateRooms();
            CreateMazes();
            ConnectRegions();
            FillDeadEnds();
            return region;
        }

    }
}
