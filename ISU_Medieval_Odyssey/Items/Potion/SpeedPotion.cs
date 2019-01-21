﻿// Author: Joon Song
// File Name: SpeedPotion.cs
// Project Name: ISU_Medieval_Odyssey
// Creation Date: 01/15/2019
// Modified Date: 01/15/2019
// Description: Class to hold SpeedPotion object

using Microsoft.Xna.Framework.Graphics;

namespace ISU_Medieval_Odyssey
{
    public sealed class SpeedPotion : Potion
    {
        // SpeedPotion specific constants
        private new static Texture2D iconImage;
        private const int TIME_AMOUNT = 30;
        private const int VALUE = 25;
        public const float BOOST_AMOUNT = 0.5f;

        /// <summary>
        /// Static constructor for <see cref="SpeedPotion"/> object
        /// </summary>
        static SpeedPotion()
        {
            // Importing speed potion icon image
            iconImage = Main.Content.Load<Texture2D>("Images/Sprites/IconImages/speedPotionIcon");
        }

        /// <summary>
        /// Constructor for <see cref="SpeedPotion"/> object
        /// </summary>
        public SpeedPotion()
        {
            // Setting up speed potion
            itemName = "Speed Potion";
            base.iconImage = iconImage;
            Value = VALUE;
        }

        /// <summary>
        /// Subprogram to use the <see cref="SpeedPotion"/> <see cref="Item"/>
        /// </summary>
        /// <param name="player">The <see cref="Player"/> using the <see cref="SpeedPotion"/></param>
        public override void Use(Player player)
        {
            // Increasing player's speed boost time and calling base use subprogram
            player.SpeedBoostTime += TIME_AMOUNT;
            base.Use(player);
        }
    }
}
