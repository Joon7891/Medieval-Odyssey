﻿// Author: Joon Song
// File Name: Spear.cs
// Project Name: ISU_Medieval_Odyssey
// Creation Date: 12/19/2018
// Modified Date: 12/19/2018
// Description: Class to hold Spear object

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ISU_Medieval_Odyssey
{
    public sealed class Spear : ThrustWeapon
    {
        // Spear specific images and rectangles
        private new static DirectionalSpriteSheet directionalSpriteSheet;
        private new static Texture2D iconImage;
        private new static Rectangle verticalHitBox = new Rectangle(0, 0, 20, 50);
        private new static Rectangle horizontalHitBox = new Rectangle(0, 0, 50, 20);

        // Various constants for Spear components
        private const int MIN_DAMAGE = 30;
        private const int MAX_DAMAGE = 60;
        private const int MIN_DURABILITY = 50;
        private const int MAX_DURABILITY = 200;

        /// <summary>
        /// Static constructor for <see cref="Spear"/> object
        /// </summary>
        static Spear()
        {
            // Loading in various Spear images
            directionalSpriteSheet = new DirectionalSpriteSheet("Images/Sprites/Weapon/Thrust/Spear/", "spear", NUM_FRAMES);
            iconImage = Main.Content.Load<Texture2D>("Images/Sprites/IconImages/spearIcon");
        }

        /// <summary>
        /// Constructor for <see cref="Spear"/> object
        /// </summary>
        public Spear()
        {
            // Setting up Spear
            itemName = "Spear";
            base.directionalSpriteSheet = directionalSpriteSheet;
            base.iconImage = iconImage;
            base.verticalHitBox = verticalHitBox;
            base.horizontalHitBox = horizontalHitBox;
            Initialize(MIN_DAMAGE, MAX_DAMAGE, MIN_DURABILITY, MAX_DURABILITY);
        }
    }
}
