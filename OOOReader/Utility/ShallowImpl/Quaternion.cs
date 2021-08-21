using OOOReader.Reader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOOReader.Utility.ShallowImpl {
	public static class Quaternion {

		public static void SetToIdentity(ShadowClass quaternion) {
			quaternion.SetField("x", 0f);
			quaternion.SetField("y", 0f);
			quaternion.SetField("z", 0f);
			quaternion.SetField("w", 1f);
		}

		public static ShadowClass TransformAndAdd(ShadowClass quaternion, ShadowClass vector3f, ShadowClass vector3fAdd) {
			float x = quaternion["x"];
			float y = quaternion["y"];
			float z = quaternion["z"];
			float w = quaternion["w"];
			float xx = x * x;
			float yy = y * y;
			float zz = z * z;
			float xy = x * y;
			float xz = x * z;
			float xw = x * w;
			float yz = y * z;
			float yw = y * w;
			float zw = z * w;
			float vx2 = vector3f["x"] * 2;
			float vy2 = vector3f["y"] * 2;
			float vz2 = vector3f["z"] * 2;
			ShadowClass result = ShadowClass.CreateInstanceOf("com.threerings.math.Vector3f");
			result["x"] = vector3f["x"] + vector3fAdd["x"] + vy2 * (xy - zw) + vz2 * (xz + yw) - vx2 * (yy + zz);
			result["y"] = vector3f["y"] + vector3fAdd["y"] + vx2 * (xy + zw) + vz2 * (yz - xw) - vy2 * (xx + zz);
			result["z"] = vector3f["z"] + vector3fAdd["z"] + vx2 * (xz - yw) + vy2 * (yz + xw) - vz2 * (xx + yy);
			return result;
		}


		public static ShadowClass TransformScaleAndAdd(ShadowClass quaternion, ShadowClass vector3f, ShadowClass vector3fAdd, float scale) {
			float x = quaternion["x"];
			float y = quaternion["y"];
			float z = quaternion["z"];
			float w = quaternion["w"];
			float xx = x * x;
			float yy = y * y;
			float zz = z * z;
			float xy = x * y;
			float xz = x * z;
			float xw = x * w;
			float yz = y * z;
			float yw = y * w;
			float zw = z * w;
			float vx2 = vector3f["x"] * 2;
			float vy2 = vector3f["y"] * 2;
			float vz2 = vector3f["z"] * 2;
			ShadowClass result = ShadowClass.CreateInstanceOf("com.threerings.math.Vector3f");
			result["x"] = (vector3f["x"] + vy2 * (xy - zw) + vz2 * (xz + yw) - vx2 * (yy + zz)) * scale + vector3fAdd["x"];
			result["y"] = (vector3f["y"] + vx2 * (xy + zw) + vz2 * (yz - xw) - vy2 * (xx + zz)) * scale + vector3fAdd["y"];
			result["z"] = (vector3f["z"] + vx2 * (xz - yw) + vy2 * (yz + xw) - vz2 * (xx + yy)) * scale + vector3fAdd["z"];
			return result;
		}

		public static ShadowClass Mult(ShadowClass selfQuaternion, ShadowClass otherQuaternion) {
			float thisx = selfQuaternion["x"];
			float thisy = selfQuaternion["y"];
			float thisz = selfQuaternion["z"];
			float thisw = selfQuaternion["w"];
			float otherx = otherQuaternion["x"];
			float othery = otherQuaternion["y"];
			float otherz = otherQuaternion["z"];
			float otherw = otherQuaternion["w"];
			ShadowClass quat = ShadowClass.CreateInstanceOf("com.threerings.math.Quaternion");
			quat["x"] = thisw * otherx + thisx * otherw + thisy * otherz - thisz * othery;
			quat["y"] = thisw * othery + thisy * otherw + thisz * otherx - thisx * otherz;
			quat["z"] = thisw * otherz + thisz * otherw + thisx * othery - thisy * otherx;
			quat["w"] = thisw * otherw - thisx * otherx - thisy * othery - thisz * otherz;
			return quat;
		}

	}
}
