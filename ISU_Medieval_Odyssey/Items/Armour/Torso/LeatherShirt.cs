﻿// Author: Joon Song
// File Name: LeatherShirt.cs
// Project Name: ISU_Medieval_Odyssey
// Creation Date: 12/27/2018
// Modified Date: 12/27/2018
// Description: Class to hold LeatherShirt object

using Microsoft.Xna.Framework.Graphics;

namespace ISU_Medieval_Odyssey
{
    public sealed class LeatherShirt : Torso
    {
        // LeatherShirt specific images
        private new static MovementSpriteSheet movementSpriteSheet;
        private new static Texture2D iconImage;

        // Constants dictating minimum and maximum values of armour attributes
        private const int MIN_DEFENSE = 1;
        private const int MAX_DEFENSE = 3;
        private const int MIN_DURABILITY = 5;
        private const int MAX_DURABILITY = 15;

        /// <summary>
        /// Static constructor to setup various <see cref="LeatherShirt"/> components
        /// </summary>
        static LeatherShirt()
        {
            // Loading in various LeatherShirt images
            movementSpriteSheet = new MovementSpriteSheet("Images/Sprites/Armour/Torso/LeatherShirt/", "leatherShirt");
            iconImage = Main.Content.Load<Texture2D>("Images/Sprites/IconImages/leatherShirtIcon");
        }

        /// <summary>
        /// Constructor for <see cref="LeatherShirt"/> object
        /// </summary>
        public LeatherShirt()
        {
            // Setting up LeatherShirt
            itemName = "Leather Shirt";
            base.iconImage = iconImage;
            base.movementSpriteSheet = movementSpriteSheet;
            InitializeArmourStatistics(MIN_DEFENSE, MAX_DEFENSE, MIN_DURABILITY, MAX_DURABILITY);
        }
    }
}
