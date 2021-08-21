using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using OOOReader.Clyde;
using OOOReader.Reader;
using System;
using System.Diagnostics;
using System.IO;

namespace OOOReader {
	/// <summary>
	/// The main entrypoint of the system.
	/// </summary>
	public static class Core {

		private const string RSRC = @"E:\Steam Games\steamapps\common\Spiral Knights\rsrc\";

		static void Main(string[] args) {
			/*
			using MemoryStream testmem = new MemoryStream();
			using DeflaterOutputStream test = new DeflaterOutputStream(testmem);

			using FileStream fstr = File.OpenWrite(RSRC + @"character\npc\monster\gremlin\null\model-customcomp.dat");
			byte[] data = File.ReadAllBytes(RSRC + @"character\npc\monster\gremlin\null\model-decomp.dat");
			testmem.WriteByte(0xFA);
			testmem.WriteByte(0xCE);
			testmem.WriteByte(0xAF);
			testmem.WriteByte(0x0E);
			testmem.WriteByte(0x10);
			testmem.WriteByte(0x00);
			testmem.WriteByte(0x10);
			testmem.WriteByte(0x00);
			test.Write(data);

			fstr.Write(testmem.ToArray());
			*/
			
			//ClydeFile file = new ClydeFile(File.OpenRead(@"E:\Steam Games\steamapps\common\Spiral Knights\rsrc\character\npc\monster\gremlin\null\model-decomp.dat"));
			ClydeFile file = new ClydeFile(RSRC + @"character\npc\monster\gremlin\null\model.dat");
			ShadowClass grem = (ShadowClass)file.ReadObject();
			float[] buffer = grem["implementation"]["skin"]["visible"][0]["geometry"]["vertexArray"]["floatArray"];
			string s = "";
			foreach (float b in buffer) {
				s += b + ", ";
			}
			File.WriteAllText(".\\DUMP.txt", s);
			
		}
	}
}
