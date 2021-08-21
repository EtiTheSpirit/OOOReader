#nullable enable
using OOOReader.Clyde;
using OOOReader.Reader;
using OOOReader.Utility.Extension;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOOReader.WithCustomReadBehavior {
	public static class EncodableProvider {

		private static readonly IReadOnlyDictionary<string, IEncodable> ENCODABLES = new Dictionary<string, IEncodable> {
			["com.threerings.math.Plane"] = new PlaneEncoder(),
			["com.threerings.opengl.renderer.Color4f"] = new Color4fEncoder(),
			["com.threerings.tudey.util.Coord"] = new CoordEncoder(),
			["com.threerings.math.Matrix3f"] = new Matrix3fEncoder(),
			["com.threerings.math.Matrix4f"] = new Matrix4fEncoder(),
			["com.threerings.math.Plane"] = new PlaneEncoder(),
			["com.threerings.math.Quaternion"] = new QuaternionEncoder(),
			["com.threerings.math.SphereCoords"] = new SphereCoordsEncoder(),
			["com.threerings.math.Vector2f"] = new Vector2fEncoder(),
			["com.threerings.math.Vector3f"] = new Vector3fEncoder(),
			["com.threerings.math.Vector4f"] = new Vector4fEncoder()
		};

		public static IEncodable? GetEncoder(ShadowClass? forClass) {
			do {
				if (ENCODABLES.TryGetValue(forClass!.Signature, out var val)) {
					return val;
				}
				forClass = forClass.BaseClass;
			} while (forClass != null);
			return null;
		}

		public interface IEncodable {

			[Obsolete("Encoding is not yet supported. Only decoding is.", true)] virtual string EncodeToString() => throw new NotImplementedException();

			[Obsolete("String decoding is not yet supported.", true)] virtual void DecodeFromString(ShadowClass into, string text) => throw new NotImplementedException();

			[Obsolete("Encoding is not yet supported. Only decoding is.", true)] virtual void EncodeToStream(BinaryWriter writer) => throw new NotImplementedException();

			void DecodeFromStream(ShadowClass into, BinaryReader reader);

		}

		#region Implementations

		private sealed class PlaneEncoder : IEncodable {
			public void DecodeFromStream(ShadowClass into, BinaryReader reader) {
				ShadowClass vec3 = ShadowClass.CreateInstanceOf("com.threerings.math.Vector3f");
				vec3.SetField("x", reader.ReadSingleBE());
				vec3.SetField("y", reader.ReadSingleBE());
				vec3.SetField("z", reader.ReadSingleBE());
				into.SetField("_normal", vec3);
				into.SetField("constant", reader.ReadSingleBE());
			}
		}

		private sealed class Color4fEncoder : IEncodable {
			public void DecodeFromStream(ShadowClass into, BinaryReader reader) {
				into.SetField("r", reader.ReadSingleBE());
				into.SetField("g", reader.ReadSingleBE());
				into.SetField("b", reader.ReadSingleBE());
				into.SetField("a", reader.ReadSingleBE());
			}
		}

		private sealed class CoordEncoder : IEncodable {
			public void DecodeFromStream(ShadowClass into, BinaryReader reader) {
				into.SetField("x", reader.ReadInt32BE());
				into.SetField("y", reader.ReadInt32BE());
			}
		}

		private sealed class Matrix3fEncoder : IEncodable {
			public void DecodeFromStream(ShadowClass into, BinaryReader reader) {
				for (int c0 = 0; c0 < 3; c0++) {
					for (int c1 = 0; c1 < 3; c1++) {
						string mtxField = "m" + c1.ToString() + c0.ToString();
						into.SetField(mtxField, reader.ReadSingleBE());
					}
				}
			}
		}

		private sealed class Matrix4fEncoder : IEncodable {
			public void DecodeFromStream(ShadowClass into, BinaryReader reader) {
				for (int c0 = 0; c0 < 4; c0++) {
					for (int c1 = 0; c1 < 4; c1++) {
						string mtxField = "m" + c1.ToString() + c0.ToString();
						into.SetField(mtxField, reader.ReadSingleBE());
					}
				}
			}
		}

		private sealed class QuaternionEncoder : IEncodable {
			public void DecodeFromStream(ShadowClass into, BinaryReader reader) {
				into.SetField("x", reader.ReadSingleBE());
				into.SetField("y", reader.ReadSingleBE());
				into.SetField("z", reader.ReadSingleBE());
				into.SetField("w", reader.ReadSingleBE());
			}
		}

		private sealed class SphereCoordsEncoder : IEncodable {
			public void DecodeFromStream(ShadowClass into, BinaryReader reader) {
				into.SetField("azimuth", reader.ReadSingleBE());
				into.SetField("elevation", reader.ReadSingleBE());
				into.SetField("distance", reader.ReadSingleBE());
			}
		}

		private sealed class Vector2fEncoder : IEncodable {
			public void DecodeFromStream(ShadowClass into, BinaryReader reader) {
				into.SetField("x", reader.ReadSingleBE());
				into.SetField("y", reader.ReadSingleBE());
			}
		}

		private sealed class Vector3fEncoder : IEncodable {
			public void DecodeFromStream(ShadowClass into, BinaryReader reader) {
				into.SetField("x", reader.ReadSingleBE());
				into.SetField("y", reader.ReadSingleBE());
				into.SetField("z", reader.ReadSingleBE());
			}
		}

		private sealed class Vector4fEncoder : IEncodable {
			public void DecodeFromStream(ShadowClass into, BinaryReader reader) {
				into.SetField("x", reader.ReadSingleBE());
				into.SetField("y", reader.ReadSingleBE());
				into.SetField("z", reader.ReadSingleBE());
				into.SetField("w", reader.ReadSingleBE()); // yes W is last
			}
		}

		#endregion

	}
}
