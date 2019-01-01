﻿// Author: Joon Song, Steven Ung
// File Name: LeatherBelt.cs
// Project Name: ISU_Medieval_Odyssey
// Creation Date: 12/18/2018
// Modified Date: 12/18/2018
// Description: Class to hold LeatherBelt object

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace ISU_Medieval_Odyssey
{
    public sealed class LeatherBelt : Belt
    {
        // Dictionary to map MovementTypes to the appropriate images
        private new static Dictionary<MovementType, Texture2D[,]> movementImages = new Dictionary<MovementType, Texture2D[,]>();

        // Constants dictating minimum and maximum values of armour attributes
        private const int DEFENCE_MIN = 1;
        private const int DEFENSE_MAX = 2;

        /// <summary>
        /// Static constructor to setup various <see cref="LeatherBelt"/> components
        /// </summary>
        static LeatherBelt()
        {
            // Setting up movement images dictionary
            string basePath = "Images/Sprites/Armour/Belt/LeatherBelt/";
            string armourTypeName = "leatherBelt";
            movementImages = EntityHelper.LoadMovementImages(basePath, armourTypeName);
        }

        /// <summary>
        /// Constructor for <see cref="LeatherBelt"/> object
        /// </summary>
        public LeatherBelt()
        {
            // Setting up armour attributes and images
            base.movementImages = movementImages;
            defence = SharedData.RNG.Next(DEFENCE_MIN, DEFENSE_MAX + 1);
        }
    }
}
