﻿/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System;
using System.Collections.Generic;
using System.Text;
using DOL.GS;
using DOL.Database;
using System.Collections;
using DOL.GS.Spells;
using log4net;
using System.Reflection;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    /// <summary>
    /// Simple Teleporter.
    /// This teleporter uses the npc guild name to determine available teleport locations in the Teleport table
    /// PackageID is used for the text displayed to the player
    /// 
    /// Example:
    /// Add this npc to the world and set guild name to 'My Teleports'
    /// Go to a location you want to teleport too and use the command /teleport 'location name' 'My Teleports'
    /// 
    /// You can whisper refresh to this teleporter to reload the teleport locations
    /// </summary>
    /// <author>Tolakram; from SI teleporter created by Aredhel</author>
    public class RandomTeleporter : GameTeleporter
    {
        protected override string Type
        {
            get
            {
                return GuildName;
            }
        }

        private List<Teleport> m_destinations = new List<Teleport>();

        /// <summary>
        /// Player right-clicked the teleporter.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            if (player.InCombat)
                return false;

            if (Realm != eRealm.None && Realm != player.Realm && player.Client.Account.PrivLevel == (int)ePrivLevel.Player)
                return false;

            if ((GuildName == null || GuildName.Length == 0) && player.Client.Account.PrivLevel > (int)ePrivLevel.Player)
            {
                SayTo(player, "I have not been set up properly, I need a guild name in order to work.");
                SayTo(player, "You can set what I say to players by setting the packageid with /mob package \"Some Text\"");
                return true;
            }

            LoadDestinations();

            if (PackageID != string.Empty)
            {
                SayTo(player, PackageID);
            }
            else
            {
                SayTo(player, "[Teleport] to a random location in this zone");
            }


            return true;
        }

        /// <summary>
        /// Use the NPC Guild Name to find all the valid destinations for this teleporter
        /// </summary>
        protected void LoadDestinations()
        {
            if (m_destinations.Count > 0 || GuildName == null || GuildName.Length == 0)
                return;

            m_destinations.AddRange(GameServer.Database.SelectObjects<Teleport>("Type = '" + GameServer.Database.Escape(GuildName) + "'"));
        }

        public override bool WhisperReceive(GameLiving source, string text)
        {
            GamePlayer player = source as GamePlayer;

            if (player == null)
                return false;

            if (player.InCombat)
                return false;

            if (player.Client.Account.PrivLevel > 1 && text.ToLower() == "refresh")
            {
                m_destinations.Clear();
                return false;
            }

            if (Realm != eRealm.None && Realm != player.Realm && player.Client.Account.PrivLevel == (int)ePrivLevel.Player)
                return false;

            Teleport destination = null;

            if (text == "Teleport")
            {
                int random = Util.Random(1, m_destinations.Count);
                int newNumber = 1;
                foreach (Teleport t in m_destinations)
                {
                    if (newNumber == random)
                    {
                        destination = t;
                    }
                    else
                    {
                        newNumber += 1;
                    }
                }

                if (destination != null)
                {
                    OnDestinationPicked(player, destination);
                }
            }

            return false;
        }


        /// <summary>
        /// Player has picked a destination.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="destination"></param>
        protected override void OnDestinationPicked(GamePlayer player, Teleport destination)
        {
            SayTo(player, "Have a safe journey!");
            base.OnDestinationPicked(player, destination);
        }

        /// <summary>
        /// Teleport the player to the designated coordinates.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="destination"></param>
        protected override void OnTeleport(GamePlayer player, Teleport destination)
        {
            OnTeleportSpell(player, destination);
        }
    }
}
