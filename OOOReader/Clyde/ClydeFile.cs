using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using OOOReader.Utility.Attributes;
using OOOReader.Utility.Data;
using OOOReader.Utility.Extension;
using OOOReader.ValueTypes;

namespace OOOReader.Clyde {
	public class ClydeFile : IDisposable {

		#region Preset Information

		private static readonly object NULL = new object();

		private static readonly Type[] BOOTSTRAP_CLASSES = {
			typeof(bool), typeof(byte), typeof(char), typeof(double),
			typeof(float), typeof(int), typeof(long), typeof(short)
		};

		#endregion

		/// <summary>
		/// The header of a Clyde file. This is in big endian and shows in the order displayed in the constant value.
		/// </summary>
		[BigEndian]
		public const uint HEADER = 0xFACEAF0E;

		/// <summary>
		/// The underlying stream that this <see cref="ClydeFile"/> is reading from.
		/// </summary>
		public Stream BaseStream { get; }

		/// <summary>
		/// The version of this <see cref="ClydeFile"/>, which determines how IDs and segment lengths are written.
		/// </summary>
		public ClydeVersion Version { get; }

		/// <summary>
		/// Whether or not this <see cref="ClydeFile"/> has been compressed.
		/// </summary>
		public bool Compressed { get; }

		/// <summary>
		/// A <see cref="BinaryReader"/> wrapped around <see cref="BaseStream"/>
		/// </summary>
		private BinaryReader Reader { get; }

		/// <summary>
		/// The system responsible for reading object IDs and segment lengths.
		/// </summary>
		private IDReader IDReader { get; }

		/// <summary>
		/// Objects bound to IDs.
		/// </summary>
		private Dictionary<int, object> CachedValues { get; } = new Dictionary<int, object>();

		public ClydeFile(Stream input) {
			BaseStream = input;
			Reader = new BinaryReader(input);

			uint header = Reader.ReadUInt32BE();
			if (header != HEADER) throw new IOException($"Invalid header value! Expected 0x{HEADER:X8}, got 0x{header:X8}");
			
			ClydeVersion version = (ClydeVersion)Reader.ReadUInt16BE();
			IDReader = IDReader.For(Reader, version);

			Compressed = Reader.ReadUInt16BE() == 0x1000;
			if (Compressed) {
				Reader = new BinaryReader(new InflaterInputStream(Reader.BaseStream));
			}

			CachedValues[0] = NULL;
		}

		public T Read<T>() where T : struct {
			return default;
		}

		public object ReadObject() {

			return null;
		}

		private object ReadNext() {
			int id = IDReader.ReadID();
			if (CachedValues.TryGetValue(id, out object cached)) {
				return cached;
			}
			return ReadValue(id);
		}

		private object ReadValue(int id) {
			int classId = IDReader.ReadID();
			string className = Reader.ReadUTF();
			byte flags = Reader.ReadByte();

			return null;
		}

		/// <summary>
		/// Releases all the resources used by the <see cref="ClydeFile"/>.
		/// </summary>
		public void Dispose() {
			GC.SuppressFinalize(this);
			BaseStream.Dispose();
		}
	}
}
