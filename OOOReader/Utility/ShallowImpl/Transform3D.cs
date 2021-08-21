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
			selfTransform3D.SetField("_matrix", thisElement);
		}

		public static void ResetTranslation(ShadowClass selfTransform3D) {
			ShadowClass thisElement = selfTransform3D.GetField<ShadowClass>("_translation");
			if (thisElement == null) {
				thisElement = ShadowClass.CreateInstanceOf("com.threerings.math.Vector3f");
			}
			Vector.Reset(thisElement);
			selfTransform3D.SetField("_translation", thisElement);
		}

		public static void ResetRotation(ShadowClass selfTransform3D) {
			ShadowClass thisElement = selfTransform3D.GetField<ShadowClass>("_rotation");
			if (thisElement == null) {
				thisElement = ShadowClass.CreateInstanceOf("com.threerings.math.Quaternion");
			}
			Vector.Reset(thisElement);
			selfTransform3D.SetField("_rotation", thisElement);
		}

		public static void ResetScale(ShadowClass selfTransform3D) {
			selfTransform3D.SetField("_scale", 1f);
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
			}
		}

		public static ShadowClass Compose(ShadowClass selfTransform3D, ShadowClass otherTransform3D) {
			int composedType = Math.Max(selfTransform3D.GetField<int>("_type"), otherTransform3D.GetField<int>("_type"));
			return null;
		}

	}
}
