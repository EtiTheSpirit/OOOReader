﻿using OOOReader.Clyde;
using System;
using System.IO;

namespace OOOReader {
	/// <summary>
	/// The main entrypoint of the system.
	/// </summary>
	public static class Core {

		static void Main(string[] args) {
			ClydeFile file = new ClydeFile(File.OpenRead(@"E:\Steam Games\steamapps\common\Spiral Knights\rsrc\character\npc\monster\gremlin\null\model.dat"));
			
			Console.ReadKey();
		}
	}
}