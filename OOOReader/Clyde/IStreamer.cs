using OOOReader.Reader;
using OOOReader.Utility.Extension;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOOReader.Clyde {

	public interface IStreamer {

		// OOO implements:
		// - Enum/Class
		// - All primitives (and string) (single value will be read)
		// - Arrays of primitives (and NOT string[])
		// - File (as its path)
		// - Buffer types
		// The mechanics of C# can collapse a lot of these, for instances arrays and buffers are effectively interchangeable for my case
		// Files, Enums, and Classes are all treatable as strings

		private static readonly Dictionary<string, IStreamer> STREAMERS = new Dictionary<string, IStreamer> {
			["java.lang.String"] = new StringStreamer(),
			["java.lang.Boolean"] = new BooleanStreamer(),
			["java.lang.Byte"] = new ByteStreamer(),
			["java.lang.Character"] = new CharStreamer(),
			["java.lang.Short"] = new ShortStreamer(),
			["java.lang.Integer"] = new IntegerStreamer(),
			["java.lang.Long"] = new LongStreamer(),
			["java.lang.Float"] = new FloatStreamer(),
			["java.lang.Double"] = new DoubleStreamer(),

			// Considering how Clyde resolves classes I don't actually think the array streamers ever get used for arrays
			// Only the buffer streamers end up getting used?
			["java.nio.ByteBuffer"] = new ByteArrayStreamer(),
			["java.nio.CharBuffer"] = new CharArrayStreamer(),
			["java.nio.ShortBuffer"] = new ShortArrayStreamer(),
			["java.nio.IntBuffer"] = new IntegerArrayStreamer(),
			["java.nio.LongBuffer"] = new LongArrayStreamer(),
			["java.nio.FloatBuffer"] = new FloatArrayStreamer(),
			["java.nio.DoubleBuffer"] = new DoubleArrayStreamer(),

			["java.io.File"] = new StringStreamer(),
			["java.lang.Class"] = new StringStreamer(),
		};

		/// <summary>
		/// Return the streamer for the applicable type, or null if no streamer is applicable.
		/// </summary>
		/// <param name="className"></param>
		/// <returns></returns>
		public static IStreamer GetStreamer(string className) {
			if (STREAMERS.ContainsKey(className)) {
				return STREAMERS[className];
			}
			return null;
		}

		/// <inheritdoc cref="IStreamer{T}.Read(BinaryReader)"/>
		public virtual object Read(BinaryReader reader) {
			return null;
		}


		// Not sealed because some other stuff will employ this.
		internal class StringStreamer : IStreamer<string> {
			public string Read(BinaryReader reader) {
				return reader.TryReadUTFBoth();
			}
		}

		#region Singular Value Streamers

		internal sealed class BooleanStreamer : IStreamer<bool> {
			public bool Read(BinaryReader reader) {
				return reader.ReadBoolean();
			}
		}

		internal sealed class ByteStreamer : IStreamer<byte> {
			public byte Read(BinaryReader reader) {
				return reader.ReadByte();
			}
		}

		internal sealed class CharStreamer : IStreamer<char> {
			public char Read(BinaryReader reader) {
				return reader.ReadChar();
			}
		}

		internal sealed class DoubleStreamer : IStreamer<double> {
			public double Read(BinaryReader reader) {
				return reader.ReadDouble();
			}
		}

		internal sealed class FloatStreamer : IStreamer<float> {
			public float Read(BinaryReader reader) {
				return reader.ReadSingle();
			}
		}

		internal sealed class IntegerStreamer : IStreamer<int> {
			public int Read(BinaryReader reader) {
				return reader.ReadInt32();
			}
		}

		internal sealed class LongStreamer : IStreamer<long> {
			public long Read(BinaryReader reader) {
				return reader.ReadInt64();
			}
		}

		internal sealed class ShortStreamer : IStreamer<short> {
			public short Read(BinaryReader reader) {
				return reader.ReadInt16();
			}
		}

		#endregion

		#region Array / Buffer Streamers

		internal sealed class BooleanArrayStreamer : IStreamer<bool[]> {
			public bool[] Read(BinaryReader reader) {
				bool[] result = new bool[reader.ReadInt32()];
				for (int idx = 0; idx < result.Length; idx++) {
					result[idx] = reader.ReadBoolean();
				}
				return result;
			}
		}

		internal sealed class ByteArrayStreamer : IStreamer<byte[]> {
			public byte[] Read(BinaryReader reader) {
				byte[] result = new byte[reader.ReadInt32()];
				for (int idx = 0; idx < result.Length; idx++) {
					result[idx] = reader.ReadByte();
				}
				return result;
			}
		}

		internal sealed class CharArrayStreamer : IStreamer<char[]> {
			public char[] Read(BinaryReader reader) {
				char[] result = new char[reader.ReadInt32()];
				for (int idx = 0; idx < result.Length; idx++) {
					result[idx] = reader.ReadChar();
				}
				return result;
			}
		}

		internal sealed class DoubleArrayStreamer : IStreamer<double[]> {
			public double[] Read(BinaryReader reader) {
				double[] result = new double[reader.ReadInt32()];
				for (int idx = 0; idx < result.Length; idx++) {
					result[idx] = reader.ReadDouble();
				}
				return result;
			}
		}

		internal sealed class FloatArrayStreamer : IStreamer<float[]> {
			public float[] Read(BinaryReader reader) {
				float[] result = new float[reader.ReadInt32()];
				for (int idx = 0; idx < result.Length; idx++) {
					result[idx] = reader.ReadSingle();
				}
				return result;
			}
		}

		internal sealed class IntegerArrayStreamer : IStreamer<int[]> {
			public int[] Read(BinaryReader reader) {
				int[] result = new int[reader.ReadInt32()];
				for (int idx = 0; idx < result.Length; idx++) {
					result[idx] = reader.ReadInt32();
				}
				return result;
			}
		}

		internal sealed class LongArrayStreamer : IStreamer<long[]> {
			public long[] Read(BinaryReader reader) {
				long[] result = new long[reader.ReadInt32()];
				for (int idx = 0; idx < result.Length; idx++) {
					result[idx] = reader.ReadInt64();
				}
				return result;
			}
		}

		internal sealed class ShortArrayStreamer : IStreamer<short[]> {
			public short[] Read(BinaryReader reader) {
				short[] result = new short[reader.ReadInt32()];
				for (int idx = 0; idx < result.Length; idx++) {
					result[idx] = reader.ReadInt16();
				}
				return result;
			}
		}

		#endregion

	}

	public interface IStreamer<out T> : IStreamer {
		/// <summary>
		/// Read the applicable type from the stream.
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public new T Read(BinaryReader reader);

		object IStreamer.Read(BinaryReader reader) => Read(reader); // Make the nongeneric read method from IStreamer point to read here in IStreamer<T>

	}
}
