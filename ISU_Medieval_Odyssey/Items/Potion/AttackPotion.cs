﻿// Author: Joon Song
// File Name: AttackPotion.cs
// Project Name: ISU_Medieval_Odyssey
// Creation Date: 01/15/2019
// Modified Date: 01/15/2019
// Description: Class to hold AttackPotion object

using Microsoft.Xna.Framework.Graphics;

namespace ISU_Medieval_Odyssey
{
    public sealed class AttackPotion : Potion
    {
        // AttackPotion specific constants
        private new static Texture2D iconImage;
        private const int TIME_AMOUNT = 20;
        private const int VALUE = 40;
        public const float BOOST_AMOUNT = 0.3f;

        /// <summary>
        /// Static constructor for <see cref="AttackPotion"/> object
        /// </summary>
        static AttackPotion()
        {
            // Importing attack potion image
            iconImage = Main.Content.Load<Texture2D>("Images/Sprites/IconImages/attackPotionIcon");
        }

        /// <summary>
        /// Constructor for <see cref="AttackPotion"/>
        /// </summary>
        public AttackPotion()
        {
            // Setting up attack potion
            base.iconImage = iconImage;
            Value = VALUE;
        }

        /// <summary>
        /// Subprogram to use the <see cref="AttackPotion"/> <see cref="Item"/>
        /// </summary>
        /// <param name="player">The <see cref="Player"/> using the <see cref="AttackPotion"/></param>
        public override void Use(Player player)
        {
            // Increasing player's attack boost time and calling base use subprogram
            player.AttackBoostTime += TIME_AMOUNT;
            base.Use(player);
        }
    }
}
