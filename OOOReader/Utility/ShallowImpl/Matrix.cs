using OOOReader.Reader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOOReader.Utility.ShallowImpl {
	public static class Matrix {

		public static void SetToIdentity(ShadowClass anyMatrixType) {
			int cap;
			if (anyMatrixType.Signature == "com.threerings.math.Matrix3f") {
				cap = 3;
			} else if (anyMatrixType.Signature == "com.threerings.math.Matrix4f") {
				cap = 4;
			} else {
				throw new ArgumentException("Unknown matrix type.");
			}

			for (int c0 = 0; c0 < cap; c0++) {
				for (int c1 = 0; c1 < cap; c1++) {
					string mtxField = "m" + c1.ToString() + c0.ToString();
					if (c0 == c1) {
						anyMatrixType.SetField(mtxField, 0);
					}
				}
			}
		}

		public static void SetTo(ShadowClass matrix, 
			float m00, float m10, float m20, float m30,
			float m01, float m11, float m21, float m31,
			float m02, float m12, float m22, float m32,
			float m03, float m13, float m23, float m33) {

			matrix["m00"] = m00;
			matrix["m10"] = m10;
			matrix["m20"] = m20;
			matrix["m30"] = m30;
			matrix["m01"] = m01;
			matrix["m11"] = m11;
			matrix["m21"] = m21;
			matrix["m31"] = m31;
			matrix["m02"] = m02;
			matrix["m12"] = m12;
			matrix["m22"] = m22;
			matrix["m32"] = m32;
			matrix["m03"] = m03;
			matrix["m13"] = m13;
			matrix["m23"] = m23;
			matrix["m33"] = m33;
		}

		#region Translation

		public static void SetTranslation(ShadowClass matrix4f, ShadowClass vector3) {
			SetTranslation(matrix4f, vector3["x"], vector3["y"], vector3["z"]);
		}

		public static void SetTranslation(ShadowClass matrix4f, float x, float y, float z) {
			matrix4f["m30"] = x;
			matrix4f["m31"] = y;
			matrix4f["m32"] = z;
		}

		#endregion

		#region Rotation

		/*
		public static void SetToRotation(ShadowClass vector3From, ShadowClass vector3To) {

		}
		*/

		public static void SetToRotation(ShadowClass matrix4f, ShadowClass quaternion) {
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
			SetTo(matrix4f, 1.0F - 2.0F * (yy + zz), 2.0F * (xy - zw), 2.0F * (xz + yw), 0.0F, 2.0F * (xy + zw), 1.0F - 2.0F * (xx + zz), 2.0F * (yz - xw), 0.0F, 2.0F * (xz - yw), 2.0F * (yz + xw), 1.0F - 2.0F * (xx + yy), 0.0F, 0.0F, 0.0F, 0.0F, 1.0F);
		}

		#endregion

		#region Scale

		public static void SetToScale(ShadowClass matrix4f, float scale) {
			SetToScale(matrix4f, scale, scale, scale);
		}

		public static void SetToScale(ShadowClass matrix4f, float x, float y, float z) {
			SetTo(matrix4f,
				x, 0, 0, 0,
				0, y, 0, 0,
				0, 0, z, 0,
				0, 0, 0, 1);
		}

		#endregion

		#region Transform

		public static void SetToTransform(ShadowClass matrix4f, ShadowClass translationVector3, ShadowClass rotationQuaternion) {
			SetToRotation(matrix4f, rotationQuaternion);
			SetTranslation(matrix4f, translationVector3);
		}

		public static void SetToTransform(ShadowClass matrix4f, ShadowClass translationVector3, ShadowClass rotationQuaternion, float scale) {
			SetToRotation(matrix4f, rotationQuaternion);
			SetTo(matrix4f,
				matrix4f["m00"] * scale, matrix4f["m10"] * scale, matrix4f["m20"] * scale, translationVector3["x"],
				matrix4f["m01"] * scale, matrix4f["m11"] * scale, matrix4f["m21"] * scale, translationVector3["y"],
				matrix4f["m02"] * scale, matrix4f["m12"] * scale, matrix4f["m22"] * scale, translationVector3["z"],
				0, 0, 0, 1
			);
		}

		#endregion

		#region Multiplication

		public static ShadowClass MultAffine(ShadowClass self, ShadowClass other) {
			ShadowClass result = ShadowClass.CreateInstanceOf("com.threerings.math.Matrix4f");

			SetTo(result,
				self["m00"] * other["m00"] + self["m10"] * other["m01"] + self["m20"] * other["m02"],
				self["m00"] * other["m10"] + self["m10"] * other["m11"] + self["m20"] * other["m12"],
				self["m00"] * other["m20"] + self["m10"] * other["m21"] + self["m20"] * other["m22"],
				self["m00"] * other["m30"] + self["m10"] * other["m31"] + self["m20"] * other["m32"] + self["m30"],

				self["m01"] * other["m00"] + self["m11"] * other["m01"] + self["m21"] * other["m02"],
				self["m01"] * other["m10"] + self["m11"] * other["m11"] + self["m21"] * other["m12"],
				self["m01"] * other["m20"] + self["m11"] * other["m21"] + self["m21"] * other["m22"],
				self["m01"] * other["m30"] + self["m11"] * other["m31"] + self["m21"] * other["m32"] + self["m31"],

				self["m02"] * other["m00"] + self["m12"] * other["m01"] + self["m22"] * other["m02"],
				self["m02"] * other["m10"] + self["m12"] * other["m11"] + self["m22"] * other["m12"],
				self["m02"] * other["m20"] + self["m12"] * other["m21"] + self["m22"] * other["m22"],
				self["m02"] * other["m30"] + self["m12"] * other["m31"] + self["m22"] * other["m32"] + self["m32"],

				0f,
				0f,
				0f,
				1f
			);

			return result;
		}

		public static ShadowClass Mult(ShadowClass self, ShadowClass other) {
			ShadowClass result = ShadowClass.CreateInstanceOf("com.threerings.math.Matrix4f");

			SetTo(result,
				self["m00"] * other["m00"] + self["m10"] * other["m01"] + self["m20"] * other["m02"] + self["m30"] * other["m03"],
				self["m00"] * other["m10"] + self["m10"] * other["m11"] + self["m20"] * other["m12"] + self["m30"] * other["m13"],
				self["m00"] * other["m20"] + self["m10"] * other["m21"] + self["m20"] * other["m22"] + self["m30"] * other["m23"],
				self["m00"] * other["m30"] + self["m10"] * other["m31"] + self["m20"] * other["m32"] + self["m30"] * other["m33"],

				self["m01"] * other["m00"] + self["m11"] * other["m01"] + self["m21"] * other["m02"] + self["m31"] * other["m03"],
				self["m01"] * other["m10"] + self["m11"] * other["m11"] + self["m21"] * other["m12"] + self["m31"] * other["m13"],
				self["m01"] * other["m20"] + self["m11"] * other["m21"] + self["m21"] * other["m22"] + self["m31"] * other["m23"],
				self["m01"] * other["m30"] + self["m11"] * other["m31"] + self["m21"] * other["m32"] + self["m31"] * other["m33"],

				self["m02"] * other["m00"] + self["m12"] * other["m01"] + self["m22"] * other["m02"] + self["m32"] * other["m03"],
				self["m02"] * other["m10"] + self["m12"] * other["m11"] + self["m22"] * other["m12"] + self["m32"] * other["m13"],
				self["m02"] * other["m20"] + self["m12"] * other["m21"] + self["m22"] * other["m22"] + self["m32"] * other["m23"],
				self["m02"] * other["m30"] + self["m12"] * other["m31"] + self["m22"] * other["m32"] + self["m32"] * other["m33"],

				self["m03"] * other["m00"] + self["m13"] * other["m01"] + self["m23"] * other["m02"] + self["m33"] * other["m03"],
				self["m03"] * other["m10"] + self["m13"] * other["m11"] + self["m23"] * other["m12"] + self["m33"] * other["m13"],
				self["m03"] * other["m20"] + self["m13"] * other["m21"] + self["m23"] * other["m22"] + self["m33"] * other["m23"],
				self["m03"] * other["m30"] + self["m13"] * other["m31"] + self["m23"] * other["m32"] + self["m33"] * other["m33"]
			);

			return result;
		}

		#endregion

		#region Inversion

		public static ShadowClass InvertAffine(ShadowClass self) {
			ShadowClass result = ShadowClass.CreateInstanceOf("com.threerings.math.Matrix4f");
			float sd00 = self["m11"] * self["m22"] - self["m21"] * self["m12"];
			float sd10 = self["m01"] * self["m22"] - self["m21"] * self["m02"];
			float sd20 = self["m01"] * self["m12"] - self["m11"] * self["m02"];
			float det = self["m00"] * sd00 + self["m20"] * sd20 - self["m10"] * sd10;
			if (det == 0) {
				// -0 and 0 are different for floating point, but C# treats them as the same value.
				throw new InvalidOperationException("This matrix is singular -- it has no inverted form. Cannot invert this matrix.");
				// TODO: Mimic OOO and make a special error type for this?
			}
			float invdet = 1 / det;
			SetTo(result,
				sd00 * invdet,
				-(self["m10"] * self["m22"] - self["m20"] * self["m12"]) * invdet,
				(self["m10"] * self["m21"] - self["m20"] * self["m11"]) * invdet,
				-(self["m10"] * (self["m21"] * self["m32"] - self["m22"] * self["m31"]) + self["m20"] * (self["m12"] * self["m31"] - self["m11"] * self["m32"]) + self["m30"] * sd00) * invdet,

				-sd10 * invdet,
				(self["m00"] * self["m22"] - self["m20"] * self["m02"]) * invdet,
				-(self["m00"] * self["m21"] - self["m20"] * self["m01"]) * invdet,
				(self["m00"] * (self["m21"] * self["m32"] - self["m22"] * self["m31"]) + self["m20"] * (self["m02"] * self["m31"] - self["m01"] * self["m32"]) + self["m30"] * sd10) * invdet,

				sd20 * invdet,
				-(self["m00"] * self["m12"] - self["m10"] * self["m02"]) * invdet,
				(self["m00"] * self["m11"] - self["m10"] * self["m01"]) * invdet,
				-(self["m00"] * (self["m11"] * self["m32"] - self["m12"] * self["m31"]) + self["m10"] * (self["m02"] * self["m31"] - self["m01"] * self["m32"]) + self["m30"] * sd20) * invdet,

				0f,
				0f,
				0f,
				1f
			);
			return result;
		}

		public static ShadowClass Invert(ShadowClass self) {
			ShadowClass result = ShadowClass.CreateInstanceOf("com.threerings.math.Matrix4f");
			float sd00 = self["m11"] * (self["m22"] * self["m33"] - self["m23"] * self["m32"]) + self["m21"] * (self["m13"] * self["m32"] - self["m12"] * self["m33"]) + self["m31"] * (self["m12"] * self["m23"] - self["m13"] * self["m22"]);
			float sd10 = self["m01"] * (self["m22"] * self["m33"] - self["m23"] * self["m32"]) + self["m21"] * (self["m03"] * self["m32"] - self["m02"] * self["m33"]) + self["m31"] * (self["m02"] * self["m23"] - self["m03"] * self["m22"]);
			float sd20 = self["m01"] * (self["m12"] * self["m33"] - self["m13"] * self["m32"]) + self["m11"] * (self["m03"] * self["m32"] - self["m02"] * self["m33"]) + self["m31"] * (self["m02"] * self["m13"] - self["m03"] * self["m12"]);
			float sd30 = self["m01"] * (self["m12"] * self["m23"] - self["m13"] * self["m22"]) + self["m11"] * (self["m03"] * self["m22"] - self["m02"] * self["m23"]) + self["m21"] * (self["m02"] * self["m13"] - self["m03"] * self["m12"]);
			float det = self["m00"] * sd00 + self["m20"] * sd20 - self["m10"] * sd10 - self["m30"] * sd30;
			if (det == 0.0F) {
				// -0 and 0 are different for floating point, but C# treats them as the same value.
				throw new InvalidOperationException("This matrix is singular -- it has no inverted form. Cannot invert this matrix.");
				// TODO: Mimic OOO and make a special error type for this?
			} else {
				float invdet = 1.0F / det;
				SetTo(result,

					sd00 * invdet, 
					-(self["m10"] * (self["m22"] * self["m33"] - self["m23"] * self["m32"]) + self["m20"] * (self["m13"] * self["m32"] - self["m12"] * self["m33"]) + self["m30"] * (self["m12"] * self["m23"] - self["m13"] * self["m22"])) * invdet, 
					(self["m10"] * (self["m21"] * self["m33"] - self["m23"] * self["m31"]) + self["m20"] * (self["m13"] * self["m31"] - self["m11"] * self["m33"]) + self["m30"] * (self["m11"] * self["m23"] - self["m13"] * self["m21"])) * invdet, 
					-(self["m10"] * (self["m21"] * self["m32"] - self["m22"] * self["m31"]) + self["m20"] * (self["m12"] * self["m31"] - self["m11"] * self["m32"]) + self["m30"] * (self["m11"] * self["m22"] - self["m12"] * self["m21"])) * invdet, 
				
					-sd10 * invdet, 
					(self["m00"] * (self["m22"] * self["m33"] - self["m23"] * self["m32"]) + self["m20"] * (self["m03"] * self["m32"] - self["m02"] * self["m33"]) + self["m30"] * (self["m02"] * self["m23"] - self["m03"] * self["m22"])) * invdet,
					-(self["m00"] * (self["m21"] * self["m33"] - self["m23"] * self["m31"]) + self["m20"] * (self["m03"] * self["m31"] - self["m01"] * self["m33"]) + self["m30"] * (self["m01"] * self["m23"] - self["m03"] * self["m21"])) * invdet, 
					(self["m00"] * (self["m21"] * self["m32"] - self["m22"] * self["m31"]) + self["m20"] * (self["m02"] * self["m31"] - self["m01"] * self["m32"]) + self["m30"] * (self["m01"] * self["m22"] - self["m02"] * self["m21"])) * invdet,
					
					sd20 * invdet, 
					-(self["m00"] * (self["m12"] * self["m33"] - self["m13"] * self["m32"]) + self["m10"] * (self["m03"] * self["m32"] - self["m02"] * self["m33"]) + self["m30"] * (self["m02"] * self["m13"] - self["m03"] * self["m12"])) * invdet, 
					(self["m00"] * (self["m11"] * self["m33"] - self["m13"] * self["m31"]) + self["m10"] * (self["m03"] * self["m31"] - self["m01"] * self["m33"]) + self["m30"] * (self["m01"] * self["m13"] - self["m03"] * self["m11"])) * invdet,
					-(self["m00"] * (self["m11"] * self["m32"] - self["m12"] * self["m31"]) + self["m10"] * (self["m02"] * self["m31"] - self["m01"] * self["m32"]) + self["m30"] * (self["m01"] * self["m12"] - self["m02"] * self["m11"])) * invdet, 
					
					-sd30 * invdet, 
					(self["m00"] * (self["m12"] * self["m23"] - self["m13"] * self["m22"]) + self["m10"] * (self["m03"] * self["m22"] - self["m02"] * self["m23"]) + self["m20"] * (self["m02"] * self["m13"] - self["m03"] * self["m12"])) * invdet, 
					-(self["m00"] * (self["m11"] * self["m23"] - self["m13"] * self["m21"]) + self["m10"] * (self["m03"] * self["m21"] - self["m01"] * self["m23"]) + self["m20"] * (self["m01"] * self["m13"] - self["m03"] * self["m11"])) * invdet, 
					(self["m00"] * (self["m11"] * self["m22"] - self["m12"] * self["m21"]) + self["m10"] * (self["m02"] * self["m21"] - self["m01"] * self["m22"]) + self["m20"] * (self["m01"] * self["m12"] - self["m02"] * self["m11"])) * invdet);
			}
			return result;
		}

		#endregion

	}
}
