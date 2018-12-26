﻿// Author: Joon Song
// File Name: Player.cs
// Project Name: ISU_Medieval_Odyssey
// Creation Date: 12/19/2018
// Modified Date: 12/19/2018
// Description: Class to hold Player object

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace ISU_Medieval_Odyssey
{
    public sealed class Player
    {
        /// <summary>
        /// The center of the player
        /// </summary>
        public Vector2Int Center { get; private set; }

        /// <summary>
        /// A cartesian intergral vector representing the player's current tile coordinates
        /// </summary>
        public Vector2Int CurrentTile { get; private set; }

        /// <summary>
        /// A cartesian intergral vector representing the player's current chunk coordinates
        /// </summary>
        public Vector2Int CurrentChunk { get; private set; }

        /// <summary>
        /// The name of the <see cref="Player"/>
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The current level of the <see cref="Player"/>
        /// </summary>
        public byte Level { get; private set; }

        /// <summary>
        /// The amount of experience that the <see cref="Player"/> has
        /// </summary>
        public short Experience { get; set; }
        private ProgressBar experienceBar;

        /// <summary>
        /// The health of the <see cref="Player"/>
        /// </summary>
        public short Health { get; set; }
        private ProgressBar healthBar;

        /// <summary>
        /// The amount of gold (currency) that the <see cref="Player"/> has
        /// </summary>
        public short Gold { get; set; }

        // Graphics-related data
        private Rectangle rectangle;
        private const byte PIXEL_SIZE = 100;
        private MovementType movementType = MovementType.Walk;
        private static Dictionary<MovementType, Texture2D[,]> movementImages = new Dictionary<MovementType, Texture2D[,]>();

        // Movement-related data
        private double rotation;
        private Direction direction;
        private const int SPEED = 200;
        private Vector2 nonRoundedLocation;

        // Statistics-related variables
        private Vector2[] statisticsLocs =
        {
            new Vector2(15, 15),
            new Vector2(10, 40),
            new Vector2(110, 40),
            new Vector2(60, 60),
            new Vector2(80, 115),
            new Vector2(64, 170)
        };

        /// <summary>
        /// Static constructor to setup various Player components
        /// </summary>
        static Player()
        {
            // Loading in various graphics
            string basePath = "Images/Sprites/Player/";
            string entityTypeName = "player";
            movementImages = EntityHelper.LoadMovementImages(basePath, entityTypeName);
        }

        /// <summary>
        /// Constructor for <see cref="Player"/> object
        /// </summary>
        public Player(string name)
        {
            // Setting up player rectangle and camera components
            rectangle = new Rectangle(0, 0, PIXEL_SIZE, PIXEL_SIZE);
            nonRoundedLocation = new Vector2();
            nonRoundedLocation.X = rectangle.X;
            nonRoundedLocation.Y = rectangle.Y;

            // Constructing world coordinate variables
            Center = Vector2Int.Zero;
            CurrentTile = Vector2Int.Zero;
            CurrentChunk = Vector2Int.Zero;

            // Setting up name and other attributes
            Name = name;
            Level = 1;
            statisticsLocs[0].X = 100 - SharedData.InformationFonts[0].MeasureString(name).X / 2;
            experienceBar = new ProgressBar(new Rectangle(10, 80, 200, 28), 200, 40, Color.White * 0.5f, 
                Color.Blue * 0.6f, SharedData.InformationFonts[0], Color.Black);
            healthBar = new ProgressBar(new Rectangle(10, 135, 200, 28), 200, 100, Color.White * 0.5f,
                Color.Red * 0.6f, SharedData.InformationFonts[0], Color.Black);
        }

        /// <summary>
        /// Update subprogram for <see cref="Player"/> object
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values</param>
        /// <param name="cameraCenter">The center of the camera that is currenetly pointed at the Player</param>
        public void Update(GameTime gameTime, Vector2 cameraCenter)
        {
            // Calling subprograms to update movement and direction
            UpdateMovement(gameTime);
            UpdateDirection(gameTime, cameraCenter);

            // Updating current tile and chunk coordinates
            CurrentTile.X = Center.X / Tile.HORIZONTAL_SPACING;
            CurrentTile.Y = Center.Y / Tile.VERTICAL_SPACING;
            CurrentChunk.X = CurrentTile.X / Chunk.SIZE;
            CurrentChunk.Y = CurrentTile.Y / Chunk.SIZE;

            // Updating status bars
            statisticsLocs[1].X = 60 - SharedData.InformationFonts[0].MeasureString($"Level {Level}").X / 2;
            statisticsLocs[2].X = 160 - SharedData.InformationFonts[0].MeasureString($"{Gold} Gold").X / 2;
            experienceBar.Update(gameTime);
            healthBar.Update(gameTime);
        }

        /// <summary>
        /// Subprogram to update the player's movement/location
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values</param>
        private void UpdateMovement(GameTime gameTime)
        {
            // Updating player location (non-rounded) given appropraite keystroke
            if (KeyboardHelper.IsKeyDown(Keys.W))
            {
                nonRoundedLocation.Y -= SPEED * gameTime.ElapsedGameTime.Milliseconds / 1000.0f;
            }
            if (KeyboardHelper.IsKeyDown(Keys.S))
            {
                nonRoundedLocation.Y += SPEED * gameTime.ElapsedGameTime.Milliseconds / 1000.0f;
            }
            if (KeyboardHelper.IsKeyDown(Keys.A))
            {
                nonRoundedLocation.X -= SPEED * gameTime.ElapsedGameTime.Milliseconds / 1000.0f;
            }
            if (KeyboardHelper.IsKeyDown(Keys.D))
            {
                nonRoundedLocation.X += SPEED * gameTime.ElapsedGameTime.Milliseconds / 1000.0f;
            }

            // Updating player coordinate-related variable
            rectangle.X = (int)(nonRoundedLocation.X + 0.5);
            rectangle.Y = (int)(nonRoundedLocation.Y + 0.5);
            Center.X = rectangle.X + PIXEL_SIZE / 2;
            Center.Y = rectangle.Y + PIXEL_SIZE / 2;
        }

        /// <summary>
        /// Subprogram to update the player's direction
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values</param>
        /// <param name="cameraCenter">The center of the camera that is currenetly pointed at the Player</param>
        private void UpdateDirection(GameTime gameTime, Vector2 cameraCenter)
        {
            // Updating player mouse rotation and direction
            rotation = (Math.Atan2(MouseHelper.Location.Y - (Center.Y - cameraCenter.Y), MouseHelper.Location.X - (Center.X - cameraCenter.X)) + 2.75 * Math.PI) % (2 * Math.PI);
            direction = (Direction)(2 * rotation / Math.PI);
        }

        /// <summary>
        /// Draw subprogram for <see cref="Player"/> object - draw's the player and his armour only
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch to draw sprites</param>
        public void Draw(SpriteBatch spriteBatch)
        {
            // Drawing player and its corresponding armour
            spriteBatch.Draw(movementImages[movementType][(byte)direction, 0], rectangle, Color.White);
        }

        /// <summary>
        /// Draw subprogram for the <see cref="Player"/>'s heads up display
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch to draw sprites</param>
        public void DrawHUD(SpriteBatch spriteBatch)
        {
            // Drawing primitive player properties
            spriteBatch.DrawString(SharedData.InformationFonts[1], Name, statisticsLocs[0], Color.Black);
            spriteBatch.DrawString(SharedData.InformationFonts[0], $"Level {Level}", statisticsLocs[1], Color.White);
            spriteBatch.DrawString(SharedData.InformationFonts[0], $"{Gold} Gold", statisticsLocs[2], Color.White);
            spriteBatch.DrawString(SharedData.InformationFonts[0], "Experience", statisticsLocs[3], Color.Blue);
            experienceBar.Draw(spriteBatch);
            spriteBatch.DrawString(SharedData.InformationFonts[0], "Health", statisticsLocs[4], Color.Red);
            healthBar.Draw(spriteBatch);
        }
    }
}
