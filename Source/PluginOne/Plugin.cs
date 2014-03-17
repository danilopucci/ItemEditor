﻿#region Licence
/**
* Copyright (C) 2014 <https://github.com/Mignari/ItemEditor>
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

using ItemEditor;
using PluginInterface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace PluginOne
{
	# region Enum

	enum ItemFlag : byte
	{
		Ground = 0x00,
		GroundBorder = 0x01,
		OnBottom = 0x02,
		OnTop = 0x03,
		Container = 0x04,
		Stackable = 0x05,
		ForceUse = 0x06,
		MultiUse = 0x07,
		HasCharges = 0x08,
		Writable = 0x09,
		WritableOnce = 0x0A,
		FluidContainer = 0x0B,
		Fluid = 0x0C,
		IsUnpassable = 0x0D,
		IsUnmoveable = 0x0E,
		BlockMissiles = 0x0F,
		BlockPathfinder = 0x10,
		Pickupable = 0x11,
		Hangable = 0x12,
		IsHorizontal = 0x13,
		IsVertical = 0x14,
		Rotatable = 0x15,
		HasLight = 0x16,
		DontHide = 0x17,
		FloorChange = 0x18,
		HasOffset = 0x19,
		HasElevation = 0x1A,
		Lying = 0x1B,
		AnimateAlways = 0x1C,
		Minimap = 0x1D,
		LensHelp = 0x1E,
		FullGround = 0x1F,
		IgnoreLook = 0x20,

		LastFlag = 0xFF
	}

	#endregion

	public class Plugin : IPlugin
	{
		#region Private Properties

		private Dictionary<UInt32, Sprite> sprites = new Dictionary<UInt32, Sprite>();
		private ClientItems items = new ClientItems();
		private List<SupportedClient> supportedClients = new List<SupportedClient>();
		private IPluginHost myHost = null;

		#endregion

		#region Public Properties

		//internal implementation
		public Settings settings = new Settings();

		//IPlugin implementation
		public IPluginHost Host { get { return myHost; } set { myHost = value; } }
		public List<SupportedClient> SupportedClients { get { return supportedClients; } }
		public ClientItems Items { get { return items; } set { items = value; } }

		#endregion

		#region General Methods

		public bool LoadClient(SupportedClient client, bool extended, string datFullPath, string sprFullPath)
		{
			if (!LoadDat(datFullPath, client, extended))
			{
				Trace.WriteLine("Failed to load dat.");
				return false;
			}

			if (!LoadSprites(sprFullPath, client, extended))
			{
				Trace.WriteLine("Failed to load spr.");
				return false;
			}
			return true;
		}

		public void Initialize()
		{
			settings.Load("PluginOne.xml");
			supportedClients = settings.GetSupportedClientList();
		}

		public void Dispose()
		{
			sprites.Clear();
			items.Clear();
		}

		public bool LoadSprites(string filename, SupportedClient client, bool extended)
		{
			return Sprite.LoadSprites(filename, ref sprites, client, extended);
		}

		public bool LoadDat(string filename, SupportedClient client, bool extended)
		{
			FileStream fileStream = new FileStream(filename, FileMode.Open);
			try
			{
				using (BinaryReader reader = new BinaryReader(fileStream))
				{
					UInt32 datSignature = reader.ReadUInt32();
					if (client.DatSignature != datSignature)
					{
						string message = "PluginOne: Bad dat signature. Expected signature is {0:X} and loaded signature is {1:X}.";
						Trace.WriteLine(String.Format(message, client.DatSignature, datSignature));
						return false;
					}

					//get max id
					UInt16 itemCount = reader.ReadUInt16();
					UInt16 creatureCount = reader.ReadUInt16();
					UInt16 effectCount = reader.ReadUInt16();
					UInt16 distanceCount = reader.ReadUInt16();

					UInt16 minclientID = 100; //items starts at 100
					UInt16 maxclientID = itemCount;

					UInt16 id = minclientID;
					while (id <= maxclientID)
					{
						ClientItem item = new ClientItem();
						item.id = id;
						items[id] = item;

						// read the options until we find 0xff
						ItemFlag flag;
						do
						{
							flag = (ItemFlag)reader.ReadByte();

							switch (flag)
							{
								case ItemFlag.Ground:
									item.type = ItemType.Ground;
									item.groundSpeed = reader.ReadUInt16();
									break;

								case ItemFlag.GroundBorder:
									item.alwaysOnTop = true;
									item.alwaysOnTopOrder = 1;
									break;

								case ItemFlag.OnBottom:
									item.alwaysOnTop = true;
									item.alwaysOnTopOrder = 2;
									break;

								case ItemFlag.OnTop:
									item.alwaysOnTop = true;
									item.alwaysOnTopOrder = 3;
									break;

								case ItemFlag.Container:
									item.type = ItemType.Container;
									break;

								case ItemFlag.Stackable:
									item.isStackable = true;
									break;

								case ItemFlag.ForceUse:
									break;

								case ItemFlag.MultiUse:
									item.multiUse = true;
									break;

								case ItemFlag.HasCharges:
									break;

								case ItemFlag.Writable:
									item.isReadable = true;
									item.maxReadWriteChars = reader.ReadUInt16();
									break;

								case ItemFlag.WritableOnce:
									item.isReadable = true;
									item.maxReadChars = reader.ReadUInt16();
									break;

								case ItemFlag.FluidContainer:
									item.type = ItemType.Fluid;
									break;

								case ItemFlag.Fluid:
									item.type = ItemType.Splash;
									break;

								case ItemFlag.IsUnpassable:
									item.isUnpassable = true;
									break;

								case ItemFlag.IsUnmoveable:
									item.isMoveable = false;
									break;

								case ItemFlag.BlockMissiles:
									item.blockMissiles = true;
									break;

								case ItemFlag.BlockPathfinder:
									item.blockPathfinder = true;
									break;

								case ItemFlag.Pickupable:
									item.isPickupable = true;
									break;

								case ItemFlag.Hangable:
									item.isHangable = true;
									break;

								case ItemFlag.IsHorizontal:
									item.isHorizontal = true;
									break;

								case ItemFlag.IsVertical:
									item.isVertical = true;
									break;

								case ItemFlag.Rotatable:
									item.isRotatable = true;
									break;

								case ItemFlag.HasLight:
									item.lightLevel = reader.ReadUInt16();
									item.lightColor = reader.ReadUInt16();
									break;

								case ItemFlag.DontHide:
									break;

								case ItemFlag.FloorChange:
									break;

								case ItemFlag.HasOffset:
									reader.ReadUInt16(); // OffsetX
									reader.ReadUInt16(); // OffsetY
									break;

								case ItemFlag.HasElevation:
									item.hasElevation = true;
									reader.ReadUInt16(); // Height
									break;

								case ItemFlag.Lying:
									break;

								case ItemFlag.AnimateAlways:
									break;

								case ItemFlag.Minimap:
									item.minimapColor = reader.ReadUInt16();
									break;

								case ItemFlag.LensHelp:
									UInt16 opt = reader.ReadUInt16();
									if (opt == 1112)
									{
										item.isReadable = true;
									}
									break;

								case ItemFlag.FullGround:
									item.fullGround = true;
									break;

								case ItemFlag.IgnoreLook:
									item.ignoreLook = true;
									break;

								case ItemFlag.LastFlag:
									break;

								default:
									Trace.WriteLine(String.Format("PluginOne: Error while parsing, unknown flag 0x{0:X} at id {1}.", flag, id));
									return false;
							}

						} while (flag != ItemFlag.LastFlag);

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
						item.isAnimation = item.frames > 1;
						item.numSprites = (UInt32)item.width * item.height * item.layers * item.patternX * item.patternY * item.patternZ * item.frames;

						// Read the sprite ids
						for (UInt32 i = 0; i < item.numSprites; ++i)
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
			}
			finally
			{
				fileStream.Close();
			}
			return true;
		}

		#endregion
	}
}
