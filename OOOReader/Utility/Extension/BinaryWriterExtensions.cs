using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOOReader.Utility.Extension {
	public static class BinaryWriterExtensions {

		public static void WriteUTF(this BinaryWriter writer, string str) {
			byte[] bytes = Encoding.UTF8.GetBytes(str);
			if (bytes.Length > ushort.MaxValue) {
				throw new ArgumentOutOfRangeException(nameof(str), "String is too long (exceeds 65535 chars)");
			}
			// Big endian
			byte[] lenBytes = BitConverter.GetBytes(bytes.Length);
			writer.Write(lenBytes[1]);
			writer.Write(lenBytes[0]);
			writer.Write(bytes);
		}

	}
}
