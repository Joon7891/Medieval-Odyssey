﻿// Author: Joon Song
// File Name: Player.cs
// Project Name: ISU_Medieval_Odyssey
// Creation Date: 12/19/2018
// Modified Date: 12/19/2018
// Description: Class to hold Player object

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Newtonsoft.Json;

namespace ISU_Medieval_Odyssey
{
    public sealed class Player : Entity
    {
        /// <summary>
        /// Static instance of <see cref="Player"/> - singleton
        /// </summary>
        [JsonIgnore]
        public static Player Instance { get; set; }

        /// <summary>
        /// The size of the player, in pixels
        /// </summary>
        [JsonIgnore]
        public static byte PIXEL_SIZE = 100;

        /// <summary>
        /// The number of frames in player walk animation
        /// </summary>
        [JsonIgnore]
        public static int NUM_WALK_FRAMES = 9;

        /// <summary>
        /// Whether the <see cref="Player"/> is moving
        /// </summary>
        [JsonIgnore]
        public bool IsMoving => KeyboardHelper.IsAnyKeyDown(SettingsScreen.Instance.Up, 
            SettingsScreen.Instance.Right, SettingsScreen.Instance.Down, SettingsScreen.Instance.Left);

        /// <summary>
        /// The name of the <see cref="Player"/>
        /// </summary>
        [JsonProperty]
        public string Name { get; private set; }

        /// <summary>
        /// The current level of the <see cref="Player"/>
        /// </summary>
        [JsonProperty]
        public byte Level { get; private set; }

        /// <summary>
        /// The amount of experience that the <see cref="Player"/> has
        /// </summary>
        [JsonProperty]
        public short Experience
        {
            get => experienceBar.CurrentValue;
            set => SetExperience(value);
        }
        private NumberBar experienceBar;

        /// <summary>
        /// The speed boost time of <see cref="Player"/>
        /// </summary>
        [JsonProperty]
        public double SpeedBoostTime { get; set; }

        /// <summary>
        /// The attack boost time of <see cref="Player"/>
        /// </summary>
        [JsonProperty]
        public double AttackBoostTime { get; set; }

        /// <summary>
        /// The defense boost time of <see cref="Player"/>
        /// </summary>
        [JsonProperty]
        public double DefenseBoostTime { get; set; }

        /// <summary>
        /// The amount of attributes this <see cref="Player"/> has
        /// </summary>
        [JsonProperty]
        public int AttributePoints { get; set; } = 0;

        /// <summary>
        /// The level of the attack attribute
        /// </summary>
        [JsonIgnore]
        public int AttackLevel => attributes[1].Level;

        // Attributes of this player
        [JsonProperty]
        private Attribute[] attributes = new Attribute[4];

        /// <summary>
        /// The amount of <see cref="ItemSlot"/>s available for this <see cref="Player"/>
        /// </summary>
        [JsonIgnore]
        public int ItemSlotsAvailable => inventory.SubArray(ARMOUR_SIZE, 3 * ROW_SIZE).Count(itemSlot => itemSlot.Item == null);

        // Movement-related data
        private bool multiDirection;
        private float diagonalFactor = (float)(Math.Sqrt(2) / 2);
        private MovementType movementType = MovementType.Walk;
        private static MovementSpriteSheet movementSpriteSheet;

        // Various player sound effects
        private static SoundEffect errorSoundEffect;
        private static SoundEffect levelUpSoundEffect;
        private static SoundEffect dropItemSoundEffect;
        private static SoundEffect pickupItemSoundEffect;

        // Animation & movement related data
        private float rotation;
        private int frameNumber = 0;
        private int animationCounter = 0;
        private Queue<MovementImageData> imagesToAnimate = new Queue<MovementImageData>();

        // Player inventory
        private Weapon currentWeapon;
        private Armour hair = new Hair();
        private const int ROW_SIZE = 9;
        private const int ARMOUR_SIZE = 6;
        [JsonProperty]
        private ItemSlot[] inventory = new ItemSlot[ARMOUR_SIZE + 3 * ROW_SIZE];

        // Variables for player inventory interaction
        private Item tempSwapItem = null;
        private int hotbarSelectionIndex = 0;
        private Rectangle itemInHandRect = new Rectangle(0, 0, ItemSlot.SIZE, ItemSlot.SIZE);

        /// <summary>
        /// The <see cref="Item"/> in this <see cref="Player"/>'s hand
        /// </summary>
        [JsonIgnore]
        public Item ItemInHand { get; set; } = null;

        /// <summary>
        /// Whether or whether not this <see cref="Player"/> is undergoing a transaction
        /// </summary>
        [JsonIgnore]
        public bool InTransaction { get; set; } = false;

        /// <summary>
        /// Whether or whether not this <see cref="Player"/> has a transaction
        /// </summary>
        [JsonIgnore]
        public bool IsInventoryOpen { get; set; } = false;

        // Statistics-related variables
        private readonly Vector2[] statisticsLocs =
        {
            new Vector2(15, 15),
            new Vector2(10, 40),
            new Vector2(110, 40),
            new Vector2(60, 60),
            new Vector2(80, 115),
            new Vector2(60, 180),
            new Vector2(72, 205)
        };
        private readonly Vector2[] boostsTextLocs =
        {
            new Vector2(299, 15),
            new Vector2(246, 45),
            new Vector2(230, 75),
            new Vector2(247, 105)
        };
        private readonly Vector2 boostTextBuffer = new Vector2(0, 30);

        /// <summary>
        /// Static constructor to setup various Player components
        /// </summary>
        static Player()
        {
            // Loading in various graphics and audio
            movementSpriteSheet = new MovementSpriteSheet("Images/Sprites/Player/", "player");
            errorSoundEffect = Main.Content.Load<SoundEffect>("Audio/SoundEffects/errorSoundEffect");
            levelUpSoundEffect = Main.Content.Load<SoundEffect>("Audio/SoundEffects/levelUpSoundEffect");
            dropItemSoundEffect = Main.Content.Load<SoundEffect>("Audio/SoundEffects/dropItemSoundEffect");
            pickupItemSoundEffect = Main.Content.Load<SoundEffect>("Audio/SoundEffects/pickupItemSoundEffect");
        }

        /// <summary>
        /// Constructor for <see cref="Player"/> object
        /// </summary>
        public Player(string name)
        {
            // Setting up singleton
            Instance = this;

            // Setting up player speed, rectangle, and camera components
            Speed = 4;
            center = Vector2Int.Zero;
            rectangle = new Rectangle(0, 0, PIXEL_SIZE, PIXEL_SIZE);
            unroundedLocation = rectangle.Location.ToVector2();
            hitBox = new Rectangle(PIXEL_SIZE >> 2, PIXEL_SIZE / 5, PIXEL_SIZE >> 1, 4 * PIXEL_SIZE / 5);
            miniIcon = new Circle(new Vector2Int(), MINI_ICON_RADIUS, Color.Red);

            // Setting up name and other attributes
            Level = 1;
            Name = name;
            statisticsLocs[0].X = 100 - SharedData.InformationFonts[0].MeasureString(name).X / 2;
            experienceBar = new NumberBar(new Rectangle(10, 80, 200, 28), LevelUpRequirement(), 0, Color.White * 0.5f, 
                Color.Blue * 0.6f, SharedData.InformationFonts[0], Color.Black);
            healthBar = new NumberBar(new Rectangle(10, 135, 200, 28), 200, 200, Color.White * 0.5f,
                Color.Red * 0.6f, SharedData.InformationFonts[0], Color.Black);

            // Constructing player inventory
            for (int i = 0; i < ARMOUR_SIZE; ++i)
            {
                inventory[i] = new ItemSlot(SharedData.SCREEN_WIDTH - 70, 630 - 70 * i);
            }
            for (int i = 0; i < 3; ++i)
            {
                for (int j = 0; j < ROW_SIZE; ++j)
                {
                    inventory[i * ROW_SIZE + j + ARMOUR_SIZE] = new ItemSlot((int)(SharedData.SCREEN_HEIGHT / 2 - 5 + (j - 3.5) * 70), 700 - 70 * i);
                }
            }

            // Setting up basic inventory
            Gold = 50;
            inventory[0].Item = new LeatherShoes();
            inventory[1].Item = new LeatherPants();
            inventory[2].Item = new LeatherBelt();
            inventory[3].Item = new LeatherTorso();
            inventory[4].Item = new LeatherShoulders();
            inventory[5].Item = new LeatherHat();
            inventory[6].Item = new Sword();
            inventory[7].Item = new Bow();

            // Setting up player attributes
            attributes[0] = new Attribute(new Rectangle(108, 230, 100, 30), "Health", () =>
            {
                healthBar.MaxValue += 20;
                healthBar.CurrentValue += 20;
            });
            attributes[1] = new Attribute(new Rectangle(108, 295, 100, 30), "Attack");
            attributes[2] = new Attribute(new Rectangle(108, 360, 100, 30), "Defense");
            attributes[3] = new Attribute(new Rectangle(108, 425, 100, 30), "Speed");

            // Serializing player data
            IO.SavePlayer(this);
        }

        /// <summary>
        /// Update subprogram for <see cref="Player"/> object
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values</param>
        /// <param name="cameraCenter">The center of the camera that is currenetly pointed at the Player</param>
        public void Update(GameTime gameTime, Vector2 cameraCenter)
        {
            // Updating attributes bar
            for (byte i = 0; i < attributes.Length; ++i)
            {
                attributes[i].Update(gameTime);

                // Returning out of subprogram of the button is pressed - prevents double presses
                if (MouseHelper.IsRectangleLeftClicked(attributes[i].Rectangle))
                {
                    return;
                }
            }
            
            // Calling subprograms to update movement if player is not in a shop
            if (!InTransaction)
            {
                UpdateMovement(gameTime);
                UpdateDirection(gameTime, cameraCenter);
            }
            else
            {
                movementType = MovementType.Walk;
                frameNumber = 0;
            }

            // Calling subprograms to update inventory
            UpdateInventory(gameTime);

            // Updating status bars
            statisticsLocs[1].X = 60 - SharedData.InformationFonts[0].MeasureString($"Level {Level}").X / 2;
            statisticsLocs[2].X = 160 - SharedData.InformationFonts[0].MeasureString($"{Gold} Gold").X / 2;

            // Updating boost times - if applicable
            SpeedBoostTime = Math.Max(0, SpeedBoostTime - gameTime.ElapsedGameTime.Milliseconds / 1000.0);
            AttackBoostTime = Math.Max(0, AttackBoostTime - gameTime.ElapsedGameTime.Milliseconds / 1000.0);
            DefenseBoostTime = Math.Max(0, DefenseBoostTime - gameTime.ElapsedGameTime.Milliseconds / 1000.0);

            // Invoking callback procedure on current tile if player chooses to interact with the tile 
            if (KeyboardHelper.NewKeyStroke(SettingsScreen.Instance.Interact) && World.Instance.GetTileAt(CurrentTile).OnInteractProcedure != null)
            {
                World.Instance.GetTileAt(CurrentTile).OnInteractProcedure.Invoke(this);
            }
        }

        /// <summary>
        /// Subprogram to update the <see cref="Player"/>'s inventory
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values</param>
        private void UpdateInventory(GameTime gameTime)
        {
            // Opening/closing inventory
            if (KeyboardHelper.NewKeyStroke(SettingsScreen.Instance.Inventory))
            {
                IsInventoryOpen = !IsInventoryOpen;
            }

            // Picking up item when pick up button is pressed and there is room in inventory
            if (KeyboardHelper.NewKeyStroke(SettingsScreen.Instance.Pickup) && ItemSlotsAvailable > 0)
            {
                // Retrieving item from world
                tempSwapItem = World.Instance.RetrieveItem(this);

                // Placing item in first open inventory slot and playing pick up item sound effect
                if (tempSwapItem != null)
                {
                    AddToInventory(tempSwapItem);
                }

                // Setting item back to null
                tempSwapItem = null;
            }

            // Updating hotbar selection if user clicks a hotbar item wit h right click
            for (int i = ARMOUR_SIZE; i < ARMOUR_SIZE + ROW_SIZE; ++i)
            {
                if (MouseHelper.IsRectangleRightClicked(inventory[i].Rectangle) || KeyboardHelper.NewKeyStroke(SettingsScreen.Instance.HotbarShortcut[i - ARMOUR_SIZE]))
                {
                    hotbarSelectionIndex = i;
                }
            }

            // Looping through inventory
            for (int i = 0; i < ARMOUR_SIZE + ROW_SIZE * (IsInventoryOpen ? 3 : 1); ++i)
            {
                // Putting item in "hand" if user left clicks the item
                if (MouseHelper.NewLeftClick() && CollisionHelper.PointToRect(MouseHelper.Location, inventory[i].Rectangle))
                {
                    if ((i < ARMOUR_SIZE && (ItemInHand == null || (ItemInHand != null && ValidArmourFit(i) && ItemInHand.IsPlayerItem)) || 
                        (i >= ARMOUR_SIZE && (ItemInHand == null || ItemInHand.IsPlayerItem))))
                    {
                        tempSwapItem = inventory[i].Item;
                        inventory[i].Item = ItemInHand;
                        ItemInHand = tempSwapItem;
                    }
                    else
                    {
                        errorSoundEffect.CreateInstance().Play();
                    }

                    return;
                }                
            }

            // Using item if user left clicks on the world to use it
            if (MouseHelper.NewLeftClick() && inventory[hotbarSelectionIndex].HasItem && imagesToAnimate.Count == 0 && currentWeapon == null && ItemInHand == null && !InTransaction)
            {
                UseItem(inventory[hotbarSelectionIndex].Item, gameTime);
                IsInventoryOpen = false;
            }            

            // Update selected item in hand
            itemInHandRect.X = (int)(MouseHelper.Location.X - ItemSlot.SIZE / 2 + 0.5);
            itemInHandRect.Y = (int)(MouseHelper.Location.Y - ItemSlot.SIZE / 2 + 0.5);

            // Updating hotbar selection via scroll
            hotbarSelectionIndex -= (MouseHelper.ScrollAmount() + ARMOUR_SIZE);
            hotbarSelectionIndex = (hotbarSelectionIndex + ROW_SIZE) % ROW_SIZE + ARMOUR_SIZE;

            // Dropping item in hand into world and playing sound effect if player clicks on the world
            if ((MouseHelper.NewLeftClick() || MouseHelper.NewLeftClick()) && ItemInHand != null && !InTransaction)
            {
                World.Instance.AddItem(ItemInHand, rectangle);
                dropItemSoundEffect.CreateInstance().Play();
                ItemInHand = null;
            }

            // Removing broken or used items from inventory
            for (int i = 0; i < inventory.Length; ++i)
            {
                if (inventory[i].Item != null && !inventory[i].Item.Valid)
                {
                    inventory[i].Item = null;
                }
            }
        }

        /// <summary>
        /// Subprogram to determine if the <see cref="Item"/> in hand is a valid fit for the <see cref="Armour"/> bar
        /// </summary>
        /// <param name="armourIndex">The index <see cref="Armour"/> slot</param>
        /// <returns>Whether the <see cref="Item"/> is a valid fit for the <see cref="Armour"/> slot</returns>
        private bool ValidArmourFit(int armourIndex)
        {
            // Returning if the armour is a valid fit given the armour index
            switch (armourIndex)
            {
                case 0:
                    return ItemInHand is Shoes;

                case 1:
                    return ItemInHand is Pants;

                case 2:
                    return ItemInHand is Belt;

                case 3:
                    return ItemInHand is Torso;

                case 4:
                    return ItemInHand is Shoulders;

                default:
                    return ItemInHand is Head;
            }
        }

        /// <summary>
        /// Subprogram to set the experience amount for this <see cref="Player"/>
        /// </summary>
        /// <param name="newExperience">The new experience amount</param>
        private void SetExperience(short newExperience)
        {
            // Leveling up player if experience overflows
            while (newExperience >= experienceBar.MaxValue)
            {
                ++Level;
                AttributePoints += 5;
                newExperience -= experienceBar.MaxValue;
                experienceBar.CurrentValue = newExperience;
                experienceBar.MaxValue = LevelUpRequirement();
                levelUpSoundEffect.CreateInstance().Play();
            }

            // Adding experience
            experienceBar.CurrentValue = newExperience;
        }

        /// <summary>
        /// Subprogram to determine the level up requirement for the <see cref="Player"/>
        /// </summary>
        /// <returns>The level up requirement for the <see cref="Player"/></returns>
        private short LevelUpRequirement() => (short)(50 + 50 * Level);

        /// <summary>
        /// Subprogram to determine the amount of attribute points the <see cref="Player"/> needs to level up a certain attribute
        /// </summary>
        /// <param name="level">The current level of the attribute</param>
        /// <returns>The amount of attrbiute points required to level up the attribute</returns>
        private int AttributeLevelUpRequirement(int currentLevel) => currentLevel + 1;

        /// <summary>
        /// Subprogram to "use" a certain item
        /// </summary>
        /// <param name="item">Item to be used</param>
        private void UseItem(Item item, GameTime gameTime)
        {
            // Adding appropraite animation frames if player is using a weapon
            if (item is Weapon)
            {
                if (item is SlashWeapon)
                {
                    // Adding slash movement images
                    for (byte i = 0; i < SlashWeapon.NUM_FRAMES; ++i)
                    {
                        imagesToAnimate.Enqueue(new MovementImageData(MovementType.Slash, i));
                    }
                }
                else if (item is ThrustWeapon)
                {
                    // Adding thrust movement iamges
                    for (byte i = 0; i < ThrustWeapon.NUM_FRAMES; ++i)
                    {
                        imagesToAnimate.Enqueue(new MovementImageData(MovementType.Thrust, i));
                    }
                }
                else
                {
                    // Adding shooting movement images
                    for (byte i = 0; i < Bow.NUM_FRAMES; ++i)
                    {
                        imagesToAnimate.Enqueue(new MovementImageData(MovementType.Shoot, i));
                    }                       
                }
                currentWeapon = (Weapon)item;
                frameNumber = 0;
                animationCounter = 0;
            }

            // Using item if it is not a weapon
            if (!(item is Weapon))
            {
                item.Use(this);
            }
        }

        /// <summary>
        /// Subprogram to update the player's movement/location
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values</param>
        private void UpdateMovement(GameTime gameTime)
        {
            // If weapon is still being used, proceed with weapon animation logic
            if (imagesToAnimate.Count != 0 || currentWeapon != null)
            {
                animationCounter = (animationCounter == 2) ? 0 : (animationCounter + 1);
                IsInventoryOpen = false;

                // Moving onto next frame every 2 updates
                if (animationCounter == 2)
                {
                    // If there are no more images left to animate, switch to walking graphics, otherwise animate weapon
                    if (imagesToAnimate.Count == 0)
                    {
                        // Adding arrow if the animation was a shooting animation 
                        if (currentWeapon is Bow)
                        {
                            currentWeapon.Use(this);
                        }

                        // Switching to movement graphics
                        movementType = MovementType.Walk;
                        currentWeapon = null;
                        frameNumber = 0;
                    }
                    else
                    {
                        movementType = imagesToAnimate.Peek().MovementType;
                        frameNumber = imagesToAnimate.Dequeue().FrameNumber;
                    }

                    // Using melee weapon in the middle of its animation
                    if (currentWeapon is SlashWeapon && frameNumber == (SlashWeapon.NUM_FRAMES - 1) / 2)
                    {
                        currentWeapon.Use(this);
                    }
                    else if (currentWeapon is ThrustWeapon && frameNumber == (ThrustWeapon.NUM_FRAMES - 1) / 2)
                    {
                        currentWeapon.Use(this);
                    }
                }
            }

            // Closing inventory and proceeding with movement logic if player is moving
            if (IsMoving && currentWeapon == null)
            {
                IsInventoryOpen = false;

                // Moving player in appropraite direction given movement keystroke
                if (KeyboardHelper.IsKeyDown(SettingsScreen.Instance.Up))
                {
                    Direction = Direction.Up;

                    // Only moving if there is no obstruction in the way
                    if (!IsObstructed(Direction.Up, gameTime))
                    {
                        unroundedLocation.Y -= GetPixelSpeed(gameTime);
                    }
                }
                if (KeyboardHelper.IsKeyDown(SettingsScreen.Instance.Down))
                {
                    Direction = Direction.Down;

                    // Only moving if there is no obstruction in the way
                    if (!IsObstructed(Direction.Down, gameTime))
                    {
                        unroundedLocation.Y += GetPixelSpeed(gameTime);
                    }
                }
                if (KeyboardHelper.IsKeyDown(SettingsScreen.Instance.Left))
                {
                    Direction = Direction.Left;

                    // Only moving if there is no obstruction in the way
                    if (!IsObstructed(Direction.Left, gameTime))
                    {
                        unroundedLocation.X -= GetPixelSpeed(gameTime);
                    }
                }
                if (KeyboardHelper.IsKeyDown(SettingsScreen.Instance.Right))
                {
                    Direction = Direction.Right;

                    // Only moving if there is no obstruction in the way
                    if (!IsObstructed(Direction.Right, gameTime))
                    {
                        unroundedLocation.X += GetPixelSpeed(gameTime);
                    }
                }
                
                // Animating movement frames if weapon is not being used
                if (imagesToAnimate.Count == 0 && currentWeapon == null)
                {
                    ++animationCounter;
                    if ((animationCounter == 3) || (animationCounter == 2 && SpeedBoostTime > 0))
                    {
                        animationCounter = 0;
                        frameNumber = (frameNumber + 1) % NUM_WALK_FRAMES;
                    }
                }
            }
            else if (imagesToAnimate.Count == 0 && currentWeapon == null)
            {
                // Resetting animation and frame counters
                animationCounter = 0;
                frameNumber = 0;
            }

            // Updating player coordinate-related variables
            multiDirection = (KeyboardHelper.IsKeyDown(SettingsScreen.Instance.Up) || KeyboardHelper.IsKeyDown(SettingsScreen.Instance.Down)) &&
                (KeyboardHelper.IsKeyDown(SettingsScreen.Instance.Left) || KeyboardHelper.IsKeyDown(SettingsScreen.Instance.Right));
            rectangle.X = (int)(unroundedLocation.X + 0.5);
            rectangle.Y = (int)(unroundedLocation.Y + 0.5);
            hitBox.X = rectangle.X + (PIXEL_SIZE >> 2);
            hitBox.Y = rectangle.Y + PIXEL_SIZE / 5;
            center.X = rectangle.X + (PIXEL_SIZE >> 1);
            center.Y = rectangle.Y + (PIXEL_SIZE >> 1);
            groundCoordinate.X = center.X;
            groundCoordinate.Y = rectangle.Bottom - 1;
            miniIcon.X = X;
            miniIcon.Y = Y;
        }

        /// <summary>
        /// Subprogram to get the pixel speed of the player
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values</param>
        /// <returns>The <see cref="Player"/>'s speed in pixels</returns>
        private float GetPixelSpeed(GameTime gameTime) => (multiDirection ? diagonalFactor : 1) * (1 + 0.05f * attributes[3].Level) * (SpeedBoostTime > 0 ? 1.5f : 1.0f) * (Tile.SPACING * Speed * gameTime.ElapsedGameTime.Milliseconds / 1000.0f);

        /// <summary>
        /// Subprogram to update the player's direction
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values</param>
        /// <param name="cameraCenter">The center of the camera that is currenetly pointed at the Player</param>
        private void UpdateDirection(GameTime gameTime, Vector2 cameraCenter)
        {
            // Updating player mouse rotation and direction
            rotation = (float)((Math.Atan2(MouseHelper.Location.Y - (Center.Y - cameraCenter.Y), MouseHelper.Location.X - (Center.X - cameraCenter.X)) + 2.75 * Math.PI) % (2 * Math.PI));
            if (!IsMoving && currentWeapon == null)
            {
                Direction = (Direction)(2 * rotation / Math.PI % 4);
            }
        }

        /// <summary>
        /// Subprogram to determine if the <see cref="Player"/> is moving towards an obstructed object
        /// </summary>
        /// <param name="direction">The <see cref="Direction"/> the player is moving in</param>
        /// <param name="gameTime">Provides a snapshot of timing values</param>
        /// <returns>Whether the <see cref="Player"/> is moving towards an obstruction</returns>
        private bool IsObstructed(Direction direction, GameTime gameTime)
        {
            // Vectors representing the player's future location
            Vector2Int[] newPixelLocations = { Vector2Int.Zero, Vector2Int.Zero };
            Vector2Int[] newTileLocations = new Vector2Int[2];

            // Switch statement to quickly lookup each direction
            switch (direction)
            {
                case Direction.Up:
                    
                    // Calculating the player's future location from moving up
                    newPixelLocations[0].Y = (int)(Center.Y + 25 - GetPixelSpeed(gameTime) + 1);
                    newPixelLocations[1].Y = newPixelLocations[0].Y;
                    newPixelLocations[0].X = HitBox.Left;
                    newPixelLocations[1].X = HitBox.Right;

                    // Converting pixel coordinate to tile coordinate and returning if the player can move up
                    newTileLocations[0] = World.PixelToTileCoordinate(newPixelLocations[0]);
                    newTileLocations[1] = World.PixelToTileCoordinate(newPixelLocations[1]);
                    return World.Instance.IsTileObstructed(newTileLocations[0]) || World.Instance.IsTileObstructed(newTileLocations[1]);

                case Direction.Down:

                    // Calculating the player's future location from moving down
                    newPixelLocations[0].Y = (int)(HitBox.Bottom + GetPixelSpeed(gameTime) - 0.5);
                    newPixelLocations[1].Y = newPixelLocations[0].Y;
                    newPixelLocations[0].X = HitBox.Left;
                    newPixelLocations[1].X = HitBox.Right;

                    // Converting pixel coordinate to tile coordinate and returning if the player can move down
                    newTileLocations[0] = World.PixelToTileCoordinate(newPixelLocations[0]);
                    newTileLocations[1] = World.PixelToTileCoordinate(newPixelLocations[1]);
                    return World.Instance.IsTileObstructed(newTileLocations[0]) || World.Instance.IsTileObstructed(newTileLocations[1]);

                case Direction.Left:

                    // Calculating the player's future location from moving left
                    newPixelLocations[0].X = (int)(HitBox.Left - GetPixelSpeed(gameTime) - 0.5);
                    newPixelLocations[1].X = newPixelLocations[0].X;
                    newPixelLocations[0].Y = Center.Y + 25;
                    newPixelLocations[1].Y = HitBox.Bottom;

                    // Converting pixel coordinate to tile coordinate and returning if the player can move left
                    newTileLocations[0] = World.PixelToTileCoordinate(newPixelLocations[0]);
                    newTileLocations[1] = World.PixelToTileCoordinate(newPixelLocations[1]);
                    return World.Instance.IsTileObstructed(newTileLocations[0]) || World.Instance.IsTileObstructed(newTileLocations[1]);

                default:

                    // Calculating the player's future location from moving right
                    newPixelLocations[0].X = (int)(HitBox.Right + GetPixelSpeed(gameTime) + 1);
                    newPixelLocations[1].X = newPixelLocations[0].X;
                    newPixelLocations[0].Y = Center.Y + 25;
                    newPixelLocations[1].Y = HitBox.Bottom;

                    // Converting pixel coordinate to tile coordinate and returning if the player can move right
                    newTileLocations[0] = World.PixelToTileCoordinate(newPixelLocations[0]);
                    newTileLocations[1] = World.PixelToTileCoordinate(newPixelLocations[1]);
                    return World.Instance.IsTileObstructed(newTileLocations[0]) || World.Instance.IsTileObstructed(newTileLocations[1]);
            }
        }

        /// <summary>
        /// Draw subprogram for <see cref="Player"/> object
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch to draw sprites</param>
        public void Draw(SpriteBatch spriteBatch)
        {
            // Drawing player and its corresponding armour and weapon
            movementSpriteSheet.Draw(spriteBatch, movementType, Direction, frameNumber, rectangle);
            for (byte i = 0; i < ARMOUR_SIZE; ++i)
            {
                ((Armour)inventory[i].Item)?.Draw(spriteBatch, movementType, Direction, frameNumber, rectangle);
            }
            if (inventory[ARMOUR_SIZE - 1].Item == null)
            {
                hair.Draw(spriteBatch, movementType, Direction, frameNumber, rectangle);
            }
            currentWeapon?.Draw(spriteBatch, rectangle, Direction, frameNumber);
        }

        /// <summary>
        /// Subprogram to inflict damage to the player
        /// </summary>
        /// <param name="damageAmount">The amount of damage to inflict</param>
        public void InflictDamage(int damageAmount)
        {
            // Calculating final damage amount and inflicting it on user
            int finalDamageAmount = damageAmount;
            for (int i = 0; i < ARMOUR_SIZE; ++i)
            {
                if (inventory[i].Item != null)
                {
                    finalDamageAmount = ((Armour)inventory[i].Item).Defend(finalDamageAmount);

                    if (((Armour)inventory[i].Item).IsBroken)
                    {
                        inventory[i].Item = null;
                    }
                }
            }
            finalDamageAmount = (int)(finalDamageAmount / (1 + 0.05 * attributes[2].Level) + 0.5);
            Health -= (int)(finalDamageAmount * (DefenseBoostTime > 0 ? 1.0f - DefensePotion.BOOST_AMOUNT : 1) + 0.5);
        }

        /// <summary>
        /// Subprogram to add an <see cref="Item"/> to this <see cref="Player"/>'s inventory
        /// </summary>
        /// <param name="item">The <see cref="Item"/> to be added</param>
        public void AddToInventory(Item item)
        {
            // Adding the item where there is an opening
            for (int i = ARMOUR_SIZE; i < 3 * ROW_SIZE + ARMOUR_SIZE; ++i)
            {
                if (inventory[i].Item == null)
                {
                    inventory[i].Item = item;
                    pickupItemSoundEffect.CreateInstance().Play();
                    break;
                }
            }
        }

        /// <summary>
        /// Draw subprogram for the <see cref="Player"/>'s heads up display
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch to draw sprites</param>
        public void DrawHUD(SpriteBatch spriteBatch)
        {
            // Adjustment rectangle for HUD
            Rectangle adjustmentRectangle;
            
            // Drawing the hotbar
            for (int i = 0; i < ARMOUR_SIZE + (IsInventoryOpen ? 3 : 1) * ROW_SIZE; ++i)
            {
                inventory[i].Draw(spriteBatch, i == hotbarSelectionIndex);
            }

            // Drawing item in "hand" - if applicable
            ItemInHand?.DrawIcon(spriteBatch, itemInHandRect);

            // Drawing primitive player properties
            spriteBatch.DrawString(SharedData.InformationFonts[1], Name, statisticsLocs[0], Color.White);
            spriteBatch.DrawString(SharedData.InformationFonts[0], $"Level {Level}", statisticsLocs[1], Color.White);
            spriteBatch.DrawString(SharedData.InformationFonts[0], $"{Gold} Gold", statisticsLocs[2], Color.White);
            spriteBatch.DrawString(SharedData.InformationFonts[0], "Experience", statisticsLocs[3], Color.Blue);
            experienceBar.Draw(spriteBatch);
            spriteBatch.DrawString(SharedData.InformationFonts[0], "Health", statisticsLocs[4], Color.Red);
            healthBar.Draw(spriteBatch);

            // Drawing attributes
            spriteBatch.DrawString(SharedData.InformationFonts[1], "Attributes", statisticsLocs[5], Color.White);
            spriteBatch.DrawString(SharedData.InformationFonts[0], $"Points: {AttributePoints}", statisticsLocs[6], Color.White);
            for (byte i = 0; i < attributes.Length; ++i)
            {
                attributes[i].Draw(spriteBatch);
            }

            // Drawing various boosts, if applicable
            if (AttackBoostTime > 0 || DefenseBoostTime > 0 || SpeedBoostTime > 0)
            {
                spriteBatch.DrawString(SharedData.InformationFonts[1], "Boosts", boostsTextLocs[0], Color.White);
            }
            if (AttackBoostTime > 0)
            {
                spriteBatch.DrawString(SharedData.InformationFonts[0], $"Attack (+30%): {Math.Round(AttackBoostTime, 2)}s", boostsTextLocs[1], Color.Blue);
            }
            if (DefenseBoostTime > 0)
            {
                spriteBatch.DrawString(SharedData.InformationFonts[0], $"Defense (+40%): {Math.Round(DefenseBoostTime, 2)}s", boostsTextLocs[2] - (AttackBoostTime > 0 ? 0 : 1) * boostTextBuffer, Color.Blue);
            }
            if (SpeedBoostTime > 0)
            {
                spriteBatch.DrawString(SharedData.InformationFonts[0], $"Speed (+50%): {Math.Round(SpeedBoostTime, 2)}s", boostsTextLocs[3] - ((AttackBoostTime > 0 ? 0 : 1) + (DefenseBoostTime > 0 ? 0 : 1)) * boostTextBuffer, Color.Blue);
            }

            // Drawing item information if player is hovering over it
            for (int i = 0; i < ARMOUR_SIZE + (IsInventoryOpen ? 3 : 1) * ROW_SIZE; ++i)
            {
                if (MouseHelper.IsHovering(inventory[i].Rectangle))
                {
                    if (i < ARMOUR_SIZE)
                    {
                        adjustmentRectangle = inventory[i].Rectangle;
                        adjustmentRectangle.X -= 3 * ItemSlot.SIZE;
                        adjustmentRectangle.Y += 2 * ItemSlot.SIZE;
                        inventory[i].Item?.DrawInformation(spriteBatch, adjustmentRectangle);
                    }
                    else
                    {
                        inventory[i].Item?.DrawInformation(spriteBatch, inventory[i].Rectangle);
                    }
                }
            }
        }

        /// <summary>
        /// Subprogram to serialize this <see cref="Player"/>'s data
        /// </summary>
        /// <returns>A <see cref="string"/> containing the serialized data</returns>
        public string Serialize() => JsonConvert.SerializeObject(this);

        /// <summary>
        /// Subprogram to deserialize a <see cref="Player"/>
        /// </summary>
        /// <param name="serializedData">A <see cref="string"/> containing the <see cref="Player"/>'s data</param>
        /// <returns>The deserialized <see cref="Player"/></returns>
        public static Player Deserialize(string serializedData) => JsonConvert.DeserializeObject<Player>(serializedData);
    }
}