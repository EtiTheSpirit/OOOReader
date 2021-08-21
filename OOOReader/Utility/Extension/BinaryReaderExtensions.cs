using OOOReader.ValueTypes;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace OOOReader.Utility.Extension {
	/// <summary>
	/// Extensions to <see cref="BinaryReader"/> that permit reading big endian values and returning them in the native format.
	/// </summary>
	public static class BinaryReaderExtensions {

		/// <summary>
		/// Flips the bytes of a big endian byte array if the system is little endian.
		/// </summary>
		/// <param name="incoming"></param>
		/// <returns></returns>
		private static byte[] FlipIfNeeded(byte[] incoming) {
			if (BitConverter.IsLittleEndian) {
				return incoming.Reverse().ToArray();
			}
			return incoming;
		}

		/// <summary>
		/// Reads a signed 16 bit integer that is in big-endian format, and returns it in the proper endianness for this system.
		/// </summary>
		/// <remarks>
		/// If a field you are comparing it to has the BigEndian attribute, you do not need to flip the result of this method as it is done for you.
		/// </remarks>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static short ReadInt16BE(this BinaryReader reader) {
			byte[] data = FlipIfNeeded(reader.ReadBytes(2));
			return BitConverter.ToInt16(data);
		}

		/// <summary>
		/// Reads an unsigned 16 bit integer that is in big-endian format, and returns it in the proper endianness for this system.
		/// </summary>
		/// <remarks>
		/// If a field you are comparing it to has the BigEndian attribute, you do not need to flip the result of this method as it is done for you.
		/// </remarks>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static ushort ReadUInt16BE(this BinaryReader reader) {
			byte[] data = FlipIfNeeded(reader.ReadBytes(2));
			return BitConverter.ToUInt16(data);
		}

		/// <summary>
		/// Reads a signed 32 bit integer that is in big-endian format, and returns it in the proper endianness for this system.
		/// </summary>
		/// <remarks>
		/// If a field you are comparing it to has the BigEndian attribute, you do not need to flip the result of this method as it is done for you.
		/// </remarks>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static int ReadInt32BE(this BinaryReader reader) {
			byte[] data = FlipIfNeeded(reader.ReadBytes(4));
			return BitConverter.ToInt32(data);
		}

		/// <summary>
		/// Reads an unsigned 32 bit integer that is in big-endian format, and returns it in the proper endianness for this system.
		/// </summary>
		/// <remarks>
		/// If a field you are comparing it to has the BigEndian attribute, you do not need to flip the result of this method as it is done for you.
		/// </remarks>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static uint ReadUInt32BE(this BinaryReader reader) {
			byte[] data = FlipIfNeeded(reader.ReadBytes(4));
			return BitConverter.ToUInt32(data);
		}

		/// <summary>
		/// Reads a signed 64 bit integer that is in big-endian format, and returns it in the proper endianness for this system.
		/// </summary>
		/// <remarks>
		/// If a field you are comparing it to has the BigEndian attribute, you do not need to flip the result of this method as it is done for you.
		/// </remarks>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static long ReadInt64BE(this BinaryReader reader) {
			byte[] data = FlipIfNeeded(reader.ReadBytes(8));
			return BitConverter.ToInt64(data);
		}

		/// <summary>
		/// Reads an unsigned 64 bit integer that is in big-endian format, and returns it in the proper endianness for this system.
		/// </summary>
		/// <remarks>
		/// If a field you are comparing it to has the BigEndian attribute, you do not need to flip the result of this method as it is done for you.
		/// </remarks>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static ulong ReadUInt64BE(this BinaryReader reader) {
			byte[] data = FlipIfNeeded(reader.ReadBytes(8));
			return BitConverter.ToUInt64(data);
		}

		/// <summary>
		/// Reads a single-precision floating point value that is in big-endian format, and returns it in the proper endianness for this system.
		/// </summary>
		/// <remarks>
		/// If a field you are comparing it to has the BigEndian attribute, you do not need to flip the result of this method as it is done for you.
		/// </remarks>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static float ReadSingleBE(this BinaryReader reader) {
			byte[] data = FlipIfNeeded(reader.ReadBytes(4));
			return BitConverter.ToSingle(data);
		}

		/// <summary>
		/// Reads a double-precision floating point value that is in big-endian format, and returns it in the proper endianness for this system.
		/// </summary>
		/// <remarks>
		/// If a field you are comparing it to has the BigEndian attribute, you do not need to flip the result of this method as it is done for you.
		/// </remarks>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static double ReadDoubleBE(this BinaryReader reader) {
			byte[] data = FlipIfNeeded(reader.ReadBytes(8));
			return BitConverter.ToDouble(data);
		}

		/// <summary>
		/// Reads a variable-length integer value from the stream.
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		/*
		public static VarInt ReadVarInt(this BinaryReader reader) {
			ulong ret = 0;
			int size = 1;
			for (int bitShift = 0; bitShift < 63; bitShift += 7) {
				int val = reader.ReadByte();
				ret |= ((ulong)(val & 0x7f)) << bitShift;
				if ((val & 0x80) == 0) {
					if (bitShift > 0 && ((val & 0x7f) == 0)) {
						break; // Invalid zero padding.
					}
					return new VarInt(ret, size);
				}
				size++;
			}
			throw new InvalidDataException();
		}
		*/
		public static VarInt ReadVarInt(this BinaryReader reader) {
			ulong ret = 0;
			int shift = 0;
			while (true) {
				if (shift < 63) {
					try {
						byte b = reader.ReadByte();
						ret |= ((ulong)b & 0x7F) << shift;
						if ((b & 0x80) != 0) {
							shift += 7;
							continue;
						}

						if (shift <= 0 || (b & 0x7F) != 0) {
							return (VarInt)ret;
						}
					} catch (EndOfStreamException) {
						return VarInt.Invalid;
					}
				}
				throw new InvalidDataException();
			}
		}

		public static string TryReadUTFBoth(this BinaryReader reader) {
			ushort bytesLeft = reader.ReadUInt16BE();
			long pos = reader.BaseStream.Position;
			try {
				return ReadUTFSkipLength(reader, bytesLeft);
			} catch {
				reader.BaseStream.Position = pos;
				byte[] txt = reader.ReadBytes(bytesLeft);
				return Encoding.UTF8.GetString(txt);
			}
		}

		/// <summary>
		/// Implements <c>DataInputStream.readUtf()</c>. See the <a href="https://docs.oracle.com/javase/7/docs/api/java/io/DataInput.html">Java documentation</a> for more information.
		/// </summary>
		/// <returns></returns>
		public static string ReadUTF(this BinaryReader reader) {
			// Ripped from IKVM's implementation of DataInputStream and cleaned up (from its decompiled form) by me.
			ushort bytesLeft = reader.ReadUInt16BE();
			return ReadUTFSkipLength(reader, bytesLeft);
		}

		private static string ReadUTFSkipLength(this BinaryReader reader, ushort bytesLeft) {
			// Ripped from IKVM's implementation of DataInputStream and cleaned up (from its decompiled form) by me.
			char[] result = new char[bytesLeft];
			long readerAt = reader.BaseStream.Position;
			byte[] stringBytes = reader.ReadBytes(bytesLeft);
			int byteIndex = 0;
			int charsRead = 0;
			while (byteIndex < bytesLeft) {
				int atChar = stringBytes[byteIndex];
				if (atChar > 127) {
					break;
				}
				byteIndex++;
				result[charsRead] = (char)(ushort)atChar;
				charsRead++;
			}
			while (byteIndex < bytesLeft) {
				int atChar = stringBytes[byteIndex];
				switch (atChar >> 4) {
					case 0:
					case 1:
					case 2:
					case 3:
					case 4:
					case 5:
					case 6:
					case 7: {
							result[charsRead] = (char)(ushort)atChar;
							byteIndex++;
							charsRead++;
							continue;
						}
					case 12:
					case 13: {
							byteIndex += 2;
							if (byteIndex > bytesLeft)
								throw new InvalidDataException($"Malformed input: Partial character at end (stream position {(readerAt + byteIndex):X8})");

							int prevByte1 = stringBytes[byteIndex - 1];
							if ((prevByte1 & 0xC0) != 0x80)
								throw new InvalidDataException($"Malformed input around byte {byteIndex} (stream position {(readerAt + byteIndex):X8})");

							result[charsRead] = (char)(ushort)((atChar & 31) << 6 | (prevByte1 & 63));
							charsRead++;
							continue;
						}
					case 14: {
							byteIndex += 3;
							if (byteIndex > bytesLeft)
								throw new InvalidDataException($"Malformed input: Partial character at end (stream position {(readerAt + byteIndex):X8})");

							int prevByte1 = stringBytes[byteIndex - 2];
							int prevByte2 = stringBytes[byteIndex - 1];
							if ((prevByte1 & 0xC0) != 0x80 || (prevByte2 & 0xC0) != 0x80)
								throw new InvalidDataException($"Malformed input around byte {byteIndex} (stream position {(readerAt + byteIndex):X8})");

							result[charsRead] = (char)(ushort)((atChar & 15) << 12 | (prevByte1 & 63) << 6 | (prevByte2 & 63) << 0);
							charsRead++;
							continue;
						}
				}
				throw new InvalidDataException($"Malformed input around byte {byteIndex} (stream position {(readerAt + byteIndex):X8})");
			}
			return new string(result, 0, charsRead);
		}

	}
}
