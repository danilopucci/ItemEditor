﻿#region Licence
/**
* Copyright (C) 2015 Nailson S. <nailsonnego@gmail.com>
*
* This program is free software; you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation; either version 2 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License along
* with this program; if not, write to the Free Software Foundation, Inc.,
* 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/
#endregion

#region Using Statements
using ItemEditor;
using OTLib.Server.Items;
using PluginInterface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
#endregion

namespace PluginTwo
{
    # region Enum

    internal enum ItemFlag : byte
    {
        Ground = 0x00,
        GroundBorder = 0x01,
        OnBottom = 0x02,
        OnTop = 0x03,
        Container = 0x04,
        Stackable = 0x05,
        ForceUse = 0x06,
        MultiUse = 0x07,
        Writable = 0x08,
        WritableOnce = 0x09,
        FluidContainer = 0x0A,
        Fluid = 0x0B,
        IsUnpassable = 0x0C,
        IsUnmoveable = 0x0D,
        BlockMissiles = 0x0E,
        BlockPathfinder = 0x0F,
        Pickupable = 0x10,
        Hangable = 0x11,
        IsHorizontal = 0x12,
        IsVertical = 0x13,
        Rotatable = 0x14,
        HasLight = 0x15,
        DontHide = 0x16,
        Translucent = 0x17,
        HasOffset = 0x18,
        HasElevation = 0x19,
        Lying = 0x1A,
        AnimateAlways = 0x1B,
        Minimap = 0x1C,
        LensHelp = 0x1D,
        FullGround = 0x1E,
        IgnoreLook = 0x1F,
        Cloth = 0x20,
        Market = 0x21,

        LastFlag = 0xFF
    }

    #endregion

    public class Plugin : IPlugin
    {
        #region Private Properties

        private Dictionary<uint, Sprite> sprites;

        #endregion

        #region Constructor
        
        public Plugin()
        {
            this.sprites = new Dictionary<uint, Sprite>();
            this.Items = new ClientItems();
            this.SupportedClients = new List<SupportedClient>();
        }

        #endregion

        #region Public Properties

        // internal implementation
        public Settings settings = new Settings();

        // IPlugin implementation
        public IPluginHost Host { get; set; }
        public List<SupportedClient> SupportedClients { get; private set; }
        public ClientItems Items { get; set; }

        #endregion

        #region General Methods

        public bool LoadClient(SupportedClient client, bool extended, bool transparency, string datFullPath, string sprFullPath)
        {
            if (!LoadDat(datFullPath, client, extended))
            {
                Trace.WriteLine("Failed to load dat.");
                return false;
            }

            if (!LoadSprites(sprFullPath, client, extended, transparency))
            {
                Trace.WriteLine("Failed to load spr.");
                return false;
            }
            return true;
        }

        public void Initialize()
        {
            this.settings.Load("PluginTwo.xml");
            this.SupportedClients = settings.GetSupportedClientList();
        }

        public SupportedClient GetClientBySignatures(uint datSignature, uint sprSignature)
        {
            foreach (SupportedClient client in this.SupportedClients)
            {
                if (client.DatSignature == datSignature && client.SprSignature == sprSignature)
                {
                    return client;
                }
            }

            return null;
        }

        public void Dispose()
        {
            sprites.Clear();
            this.Items.Clear();
        }

        public bool LoadSprites(string filename, SupportedClient client, bool extended, bool transparency)
        {
            return Sprite.LoadSprites(filename, ref sprites, client, extended, transparency);
        }

        public bool LoadDat(string filename, SupportedClient client, bool extended)
        {
            using (FileStream fileStream = new FileStream(filename, FileMode.Open))
            {
                BinaryReader reader = new BinaryReader(fileStream);
                uint datSignature = reader.ReadUInt32();
                if (client.DatSignature != datSignature)
                {
                    string message = "PluginTwo: Bad dat signature. Expected signature is {0:X} and loaded signature is {1:X}.";
                    Trace.WriteLine(String.Format(message, client.DatSignature, datSignature));
                    return false;
                }

                // get max id
                ushort itemCount = reader.ReadUInt16();
                reader.ReadUInt16(); // skipping outfits count
                reader.ReadUInt16(); // skipping effects count
                reader.ReadUInt16(); // skipping missiles count

                ushort minclientID = 100; // items starts at 100
                ushort maxclientID = itemCount;

                ushort id = minclientID;
                while (id <= maxclientID)
                {
                    ClientItem item = new ClientItem();
                    item.ID = id;
                    this.Items[id] = item;

                    // read the options until we find 0xff
                    ItemFlag flag;
                    do
                    {
                        flag = (ItemFlag)reader.ReadByte();

                        switch (flag)
                        {
                            case ItemFlag.Ground:
                                item.Type = ServerItemType.Ground;
                                item.GroundSpeed = reader.ReadUInt16();
                                break;

                            case ItemFlag.GroundBorder:
                                item.HasStackOrder = true;
                                item.StackOrder = TileStackOrder.Border;
                                break;

                            case ItemFlag.OnBottom:
                                item.HasStackOrder = true;
                                item.StackOrder = TileStackOrder.Bottom;
                                break;

                            case ItemFlag.OnTop:
                                item.HasStackOrder = true;
                                item.StackOrder = TileStackOrder.Top;
                                break;

                            case ItemFlag.Container:
                                item.Type = ServerItemType.Container;
                                break;

                            case ItemFlag.Stackable:
                                item.Stackable = true;
                                break;

                            case ItemFlag.ForceUse:
                                break;

                            case ItemFlag.MultiUse:
                                item.MultiUse = true;
                                break;

                            case ItemFlag.Writable:
                                item.Readable = true;
                                item.MaxReadWriteChars = reader.ReadUInt16();
                                break;

                            case ItemFlag.WritableOnce:
                                item.Readable = true;
                                item.MaxReadChars = reader.ReadUInt16();
                                break;

                            case ItemFlag.FluidContainer:
                                item.Type = ServerItemType.Fluid;
                                break;

                            case ItemFlag.Fluid:
                                item.Type = ServerItemType.Splash;
                                break;

                            case ItemFlag.IsUnpassable:
                                item.Unpassable = true;
                                break;

                            case ItemFlag.IsUnmoveable:
                                item.Movable = false;
                                break;

                            case ItemFlag.BlockMissiles:
                                item.BlockMissiles = true;
                                break;

                            case ItemFlag.BlockPathfinder:
                                item.BlockPathfinder = true;
                                break;

                            case ItemFlag.Pickupable:
                                item.Pickupable = true;
                                break;

                            case ItemFlag.Hangable:
                                item.Hangable = true;
                                break;

                            case ItemFlag.IsHorizontal:
                                item.HookEast = true;
                                break;

                            case ItemFlag.IsVertical:
                                item.HookSouth = true;
                                break;

                            case ItemFlag.Rotatable:
                                item.Rotatable = true;
                                break;

                            case ItemFlag.HasLight:
                                item.LightLevel = reader.ReadUInt16();
                                item.LightColor = reader.ReadUInt16();
                                break;

                            case ItemFlag.DontHide:
                                break;

                            case ItemFlag.Translucent:
                                break;

                            case ItemFlag.HasOffset:
                                reader.ReadUInt16(); // OffsetX
                                reader.ReadUInt16(); // OffsetY
                                break;

                            case ItemFlag.HasElevation:
                                item.HasElevation = true;
                                reader.ReadUInt16(); // Height
                                break;

                            case ItemFlag.Lying:
                                break;

                            case ItemFlag.AnimateAlways:
                                break;

                            case ItemFlag.Minimap:
                                item.MinimapColor = reader.ReadUInt16();
                                break;

                            case ItemFlag.LensHelp:
                                ushort opt = reader.ReadUInt16();
                                if (opt == 1112)
                                {
                                    item.Readable = true;
                                }
                                break;

                            case ItemFlag.FullGround:
                                item.FullGround = true;
                                break;

                            case ItemFlag.IgnoreLook:
                                item.IgnoreLook = true;
                                break;

                            case ItemFlag.Cloth:
                                reader.ReadUInt16();
                                break;

                            case ItemFlag.Market:
                                reader.ReadUInt16(); // category
                                item.TradeAs = reader.ReadUInt16(); // trade as
                                reader.ReadUInt16(); // show as
                                var size = reader.ReadUInt16();
                                item.Name = new string(reader.ReadChars(size));
                                reader.ReadUInt16(); // profession
                                reader.ReadUInt16(); // level
                                break;

                            case ItemFlag.LastFlag:
                                break;

                            default:
                                Trace.WriteLine(String.Format("PluginTwo: Error while parsing, unknown flag 0x{0:X} at id {1}.", flag, id));
                                return false;
                        }

                    }
                    while (flag != ItemFlag.LastFlag);

                    item.width = reader.ReadByte();
                    item.height = reader.ReadByte();
                    if ((item.width > 1) || (item.height > 1))
                    {
                        reader.BaseStream.Position++;
                    }

                    item.layers = reader.ReadByte();
                    item.patternX = reader.ReadByte();
                    item.patternY = reader.ReadByte();
                    item.patternZ = reader.ReadByte();
                    item.frames = reader.ReadByte();
                    item.IsAnimation = item.frames > 1;
                    item.numSprites = (uint)item.width * item.height * item.layers * item.patternX * item.patternY * item.patternZ * item.frames;

                    // Read the sprite ids
                    for (uint i = 0; i < item.numSprites; ++i)
                    {
                        uint spriteId;
                        if (extended)
                        {
                            spriteId = reader.ReadUInt32();
                        }
                        else
                        {
                            spriteId = reader.ReadUInt16();
                        }

                        Sprite sprite;
                        if (!sprites.TryGetValue(spriteId, out sprite))
                        {
                            sprite = new Sprite();
                            sprite.id = spriteId;
                            sprites[spriteId] = sprite;
                        }
                        item.spriteList.Add(sprite);
                    }
                    ++id;
                }
            }

            return true;
        }

        #endregion
    }
}
