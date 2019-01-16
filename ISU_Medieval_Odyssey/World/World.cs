﻿// Author: Joon Song
// File Name: World.cs
// Project Name: ISU_Medieval_Odyssey
// Creation Date: 12/20/2018
// Modified Date: 12/24/2018
// Description: Class to hold World object

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ISU_Medieval_Odyssey
{
    public sealed class World
    {
        /// <summary>
        /// Static instance of <see cref="World"/> - see singleton
        /// </summary>
        public static World Instance { get; set; }

        // The world's loaded chunks and loaded chunks
        private readonly TerrainGenerator terrainGenerator;
        private const int LOADED_CHUNK_COUNT = 5;
        private Dictionary<Vector2Int, Chunk> loadedChunks = new Dictionary<Vector2Int, Chunk>();
        private static Vector2Int[] borderChunkCoordinate =
        {
            new Vector2Int(2, 2),
            new Vector2Int(2, -2),
            new Vector2Int(-2, 2),
            new Vector2Int(-2, -2)
        };

        private List<Projectile> projectiles = new List<Projectile>();

        /// <summary>
        /// Constructor for <see cref="World"/> object
        /// </summary>
        /// <param name="seed">The seed of this <see cref="World"/></param>
        public World(int? seed = null)
        {
            // Vector to hold the current chunk location
            Vector2Int chunkLocation;
            
            // If seed was not provided, generate new seed
            terrainGenerator = new TerrainGenerator(seed);

            // Generating chunks around world and adding them to file
            for (int i = 0; i < 5; ++i)
            {
                for (int j = 0; j < 5; ++j)
                {
                    chunkLocation = new Vector2Int(i - 2, j - 2);
                    loadedChunks.Add(chunkLocation, new Chunk(chunkLocation, terrainGenerator));
                    IO.SaveChunk(loadedChunks[chunkLocation]);
                }
            }

            // Setting up singleton
            Instance = this;
        }

        public void Update(GameTime gameTime)
        {
            // Shifting chunk and loading chunks if needed, if current chunk is not centered
            foreach (Vector2Int adjustmentVector in borderChunkCoordinate)
            {
                if (!loadedChunks.ContainsKey(GameScreen.Instance.Player.CurrentChunk + adjustmentVector))
                {
                    AdjustLoadedChunks(GameScreen.Instance.Player.CurrentChunk);
                    break;
                }
            }

            // Updating the projectiles in the world
            for (int i = 0; i < projectiles.Count; ++i)
            {
                projectiles[i].Update(gameTime);

                // Removing projectiles if they are not active
                if (!projectiles[i].Active)
                {
                    projectiles.RemoveAt(i);
                    --i;
                }
            }
        }

        /// <summary>
        /// Subprogram to adjust the loaded chunk
        /// </summary>
        /// <param name="centerChunk">A <see cref="Vector2Int"/> representing the center of the loaded chunk</param>
        public void AdjustLoadedChunks(Vector2Int centerChunk)
        {
            // Vector to hold chunk location
            Vector2Int newChunkLocation;
            Vector2Int[] currentChunkLocations = loadedChunks.Keys.ToArray();
            
            foreach (Vector2Int chunkLocation in currentChunkLocations)
            {
                if (!(centerChunk.X - 2 <= chunkLocation.X && chunkLocation.X <= centerChunk.X + 2 &&
                      centerChunk.Y - 2 <= chunkLocation.Y && chunkLocation.Y <= centerChunk.Y + 2))
                {
                    IO.SaveChunk(loadedChunks[chunkLocation]);
                    loadedChunks.Remove(chunkLocation);
                }
            }

            // Iterating through the chunk locations of the chunks that should be loaded
            for (int x = centerChunk.X - 2; x <= centerChunk.X + 2; ++x)
            {
                for (int y = centerChunk.Y - 2; y <= centerChunk.Y + 2; ++y)
                {
                    newChunkLocation = new Vector2Int(x, y);
                   
                    // If this chunk isn't loaded, load it
                    if (!loadedChunks.ContainsKey(newChunkLocation))
                    {
                        // If it exist in file, load it, otherwise construct it
                        if (IO.ChunkExists(newChunkLocation))
                        {
                            loadedChunks.Add(newChunkLocation, IO.LoadChunk(newChunkLocation));
                        }
                        else
                        {
                            loadedChunks.Add(newChunkLocation, new Chunk(newChunkLocation, terrainGenerator));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Subprogram to 
        /// </summary>
        /// <param name="spriteBatch"></param>
        /// <param name="camera"></param>
        public void Draw(SpriteBatch spriteBatch, Camera camera)
        {
            // Beginning spritebatch in adjusted camera
            spriteBatch.Begin(transformMatrix: camera.ViewMatrix, samplerState: SamplerState.PointClamp);

            // Drawing the various loaded chunks
            foreach (Chunk chunk in loadedChunks.Values)
            {
                chunk.Draw(spriteBatch);
            }

            // Drawing projectiles
            for (int i = 0; i < projectiles.Count; ++i)
            {
                projectiles[i].Draw(spriteBatch);
            }

            // Ending spriteBatch
            spriteBatch.End();
        }

        /// <summary>
        /// Subprogram to add a projectile into this <see cref="World"/>
        /// </summary>
        /// <param name="projectile">The <see cref="Projectile"/> to be added</param>
        public void AddProjectile(Projectile projectile)
        {
            // Adding projectile
            projectiles.Add(projectile);
        }

    }
}
