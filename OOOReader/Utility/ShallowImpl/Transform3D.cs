using OOOReader.Reader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOOReader.Utility.ShallowImpl {
	public static class Transform3D {

		public static void ResetMatrix(ShadowClass selfTransform3D) {
			ShadowClass thisElement = selfTransform3D.GetField<ShadowClass>("_matrix");
			if (thisElement == null) {
				thisElement = ShadowClass.CreateInstanceOf("com.threerings.math.Matrix4f");
			}
			Matrix.SetToIdentity(thisElement);
			selfTransform3D["_matrix"] = thisElement;
		}

		public static void ResetTranslation(ShadowClass selfTransform3D) {
			ShadowClass thisElement = selfTransform3D.GetField<ShadowClass>("_translation");
			if (thisElement == null) {
				thisElement = ShadowClass.CreateInstanceOf("com.threerings.math.Vector3f");
			}
			Vector.Reset(thisElement);
			selfTransform3D["_translation"] = thisElement;
		}

		public static void ResetRotation(ShadowClass selfTransform3D) {
			ShadowClass thisElement = selfTransform3D.GetField<ShadowClass>("_rotation");
			if (thisElement == null) {
				thisElement = ShadowClass.CreateInstanceOf("com.threerings.math.Quaternion");
			}
			Vector.Reset(thisElement);
			selfTransform3D["_rotation"] = thisElement;
		}

		public static void ResetScale(ShadowClass selfTransform3D) {
			selfTransform3D["_scale"] = 1f;
			//selfTransform3D.SetField("_scale", 1f);
		}

		public static void Update(ShadowClass selfTransform3D, int newType) {
			int thisType = selfTransform3D.GetField<int>("_type");
			if (thisType == 0) {
				if (newType >= 3) {
					ResetMatrix(selfTransform3D);
				} else if (newType >= 1) {
					ResetTranslation(selfTransform3D);
					ResetRotation(selfTransform3D);
					ResetScale(selfTransform3D);
				}
			} else if (thisType == 1) {
				if (newType >= 3) {
					ShadowClass matrix = selfTransform3D.GetField<ShadowClass>("_matrix");
					if (matrix == null) {
						matrix = ShadowClass.CreateInstanceOf("com.threerings.math.Matrix4f");
					}
					Matrix.SetToTransform(matrix, selfTransform3D.GetField<ShadowClass>("_translation"), selfTransform3D.GetField<ShadowClass>("_rotation"));
					selfTransform3D["_matrix"] = matrix;
				} else if (newType == 2) {
					ResetScale(selfTransform3D);
				}
			} else if (thisType == 2 && newType >= 3) {
				ShadowClass matrix = selfTransform3D.GetField<ShadowClass>("_matrix");
				if (matrix == null) {
					matrix = ShadowClass.CreateInstanceOf("com.threerings.math.Matrix4f");
				}
				Matrix.SetToTransform(matrix, selfTransform3D.GetField<ShadowClass>("_translation"), selfTransform3D.GetField<ShadowClass>("_rotation"), selfTransform3D.GetField<float>("_scale"));
				selfTransform3D["_matrix"] = matrix;
			}
		}

		public static void SetType(ShadowClass selfTransform3D, int type) {
			selfTransform3D["_type"] = type;
			if (type == 3 || type == 4) {
				ShadowClass matrix = selfTransform3D.GetField<ShadowClass>("_matrix");
				if (matrix == null) {
					selfTransform3D["_matrix"] = ShadowClass.CreateInstanceOf("com.threerings.math.Matrix4f");
				}
			} else if (type == 1 || type == 2) {
				ShadowClass trs = selfTransform3D.GetField<ShadowClass>("_translation");
				if (trs == null) {
					selfTransform3D["_translation"] = ShadowClass.CreateInstanceOf("com.threerings.math.Vector3f");
				}
				ShadowClass rot = selfTransform3D.GetField<ShadowClass>("_rotation");
				if (rot == null) {
					selfTransform3D["_rotation"] = ShadowClass.CreateInstanceOf("com.threerings.math.Quaternion");
				}
			}
		}

		public static void Promote(ShadowClass selfTransform3D, int type) {
			Update(selfTransform3D, type);
			SetType(selfTransform3D, type);
		}

		public static ShadowClass Compose(ShadowClass selfTransform3D, ShadowClass otherTransform3D) {
			ShadowClass result = ShadowClass.CreateInstanceOf("com.threerings.math.Transform3D");
			int composedType = Math.Max(selfTransform3D.GetField<int>("_type"), otherTransform3D.GetField<int>("_type"));
			Update(selfTransform3D, composedType);
			Update(otherTransform3D, composedType);
			SetType(result, composedType);
			if (composedType == 1) {
				result["_translation"] = Quaternion.TransformAndAdd(selfTransform3D["_rotation"], otherTransform3D["_translation"], selfTransform3D["_translation"]);
				result["_rotation"] = Quaternion.Mult(selfTransform3D["_rotation"], otherTransform3D["_rotation"]);
			} else if (composedType == 2) {
				result["_translation"] = Quaternion.TransformScaleAndAdd(selfTransform3D["_rotation"], otherTransform3D["_translation"], selfTransform3D["_translation"], selfTransform3D["_scale"]);
				result["_rotation"] = Quaternion.Mult(selfTransform3D["_rotation"], otherTransform3D["_rotation"]);
				result["_scale"] = selfTransform3D["_scale"] * otherTransform3D["_scale"];
			} else if (composedType == 3) {
				result["_matrix"] = Matrix.MultAffine(selfTransform3D["_matrix"], otherTransform3D["_matrix"]);
			} else if (composedType == 4) {
				result["_matrix"] = Matrix.Mult(selfTransform3D["_matrix"], otherTransform3D["_matrix"]);
			}

			return result;
		}

	}
}
