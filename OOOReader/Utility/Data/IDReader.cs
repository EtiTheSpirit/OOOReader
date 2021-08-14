using OOOReader.Clyde;
using OOOReader.Utility.Extension;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOOReader.Utility.Data {
	/// <summary>
	/// An abstract class representing a helper to read class IDs and segment lengths from Clyde files.
	/// </summary>
	public abstract class IDReader : IDisposable {

		/// <summary>
		/// The underlying <see cref="BinaryReader"/> this <see cref="IDReader"/> reads from.
		/// </summary>
		public BinaryReader BaseStream { get; }

		/// <summary>
		/// Reads an ID from the stream.
		/// </summary>
		/// <returns></returns>
		public abstract int ReadID();

		/// <summary>
		/// Returns the length of the next component from the Clyde file.
		/// </summary>
		/// <returns></returns>
		public abstract int ReadNextSegmentLength();

		/// <summary>
		/// Instantiates a new <see cref="IDReader"/> with the given underlying <see cref="BinaryReader"/>.
		/// </summary>
		/// <param name="baseStream"></param>
		public IDReader(BinaryReader baseStream) {
			BaseStream = baseStream;
		}

		/// <summary>
		/// Given the underlying <see cref="BinaryReader"/> as well as the <see cref="ClydeVersion"/> acquired from a Clyde file, this will return a new
		/// instance of the appropriate <see cref="IDReader"/>.
		/// </summary>
		/// <param name="baseStream"></param>
		/// <param name="version"></param>
		/// <returns></returns>
		public static IDReader For(BinaryReader baseStream, ClydeVersion version) {
			if (version == ClydeVersion.Classic)
				return new ClassicReader(baseStream);

			if (version == ClydeVersion.Intermediate)
				return new IntermediateReader(baseStream);

			if (version == ClydeVersion.VarInt)
				return new VarIntReader(baseStream);

			throw new ArgumentOutOfRangeException(nameof(version), $"The given Clyde Version [{(int)version:X4}] is not valid!");
		}

		/// <summary>
		/// Destroys this <see cref="IDReader"/>, closing the underlying <see cref="BaseStream"/>.
		/// </summary>
		public void Dispose() {
			GC.SuppressFinalize(this);
			BaseStream.Dispose();
		}

		#region Implementations

		/// <summary>
		/// Reads IDs and segment lengths in the <see cref="ValueTypes.VarInt"/> format.
		/// </summary>
		public class VarIntReader : IDReader {

			/// <summary>
			/// Instantiates a new <see cref="VarIntReader"/> with the given underlying <see cref="BinaryReader"/>.
			/// </summary>
			/// <param name="baseStream"></param>
			public VarIntReader(BinaryReader baseStream) : base(baseStream) { }

			/// <inheritdoc/>
			public override int ReadID() => BaseStream.ReadVarInt();


			/// <inheritdoc/>
			public override int ReadNextSegmentLength() => BaseStream.ReadVarInt();
		}

		/// <summary>
		/// Reads IDs in the <see cref="ValueTypes.VarInt"/> format, but segment lengths as <see langword="int"/>.
		/// </summary>
		public class IntermediateReader : IDReader {

			/// <summary>
			/// Instantiates a new <see cref="IntermediateReader"/> with the given underlying <see cref="BinaryReader"/>.
			/// </summary>
			/// <param name="baseStream"></param>
			public IntermediateReader(BinaryReader baseStream) : base(baseStream) { }

			/// <inheritdoc/>
			public override int ReadID() => BaseStream.ReadVarInt();

			/// <inheritdoc/>
			public override int ReadNextSegmentLength() => BaseStream.ReadInt32BE();
		}

		/// <summary>
		/// Reads IDs in an ordered-use format, where all IDs up to 255 are written as single <see langword="byte"/>s, all IDs from 256 to 65535 are written as <see langword="ushort"/>, and IDs beyond 65535 are written as <see langword="int"/>. Segment lengths are read as <see langword="int"/>.
		/// </summary>
		public class ClassicReader : IDReader {

			/// <summary>
			/// Instantiates a new <see cref="ClassicReader"/> with the given underlying <see cref="BinaryReader"/>.
			/// </summary>
			/// <param name="baseStream"></param>
			public ClassicReader(BinaryReader baseStream) : base(baseStream) { }

			/// <summary>
			/// The highest id read thus far.
			/// </summary>
			private int HighestID = 0;

			/// <inheritdoc/>
			public override int ReadID() {
				int id;
				if (HighestID < byte.MaxValue) {
					id = BaseStream.ReadByte();
				} else if (HighestID < ushort.MaxValue) {
					id = BaseStream.ReadUInt16BE();
				} else {
					id = BaseStream.ReadInt32BE();
				}
				HighestID = Math.Max(HighestID, id);
				return id;
			}

			/// <inheritdoc/>
			public override int ReadNextSegmentLength() => BaseStream.ReadInt32BE();
		}

		#endregion
	}
}
