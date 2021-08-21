using OOOReader.Reader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOOReader.Utility.ShallowImpl {
	public static class Vector {

		public static void Reset(ShadowClass anyVectorType) {
			if (anyVectorType.Signature == "com.threerings.math.Vector2f") {
				anyVectorType.SetField("x", 0f);
				anyVectorType.SetField("y", 0f);
			} else if (anyVectorType.Signature == "com.threerings.math.Vector3f") {
				anyVectorType.SetField("x", 0f);
				anyVectorType.SetField("y", 0f);
				anyVectorType.SetField("z", 0f);
			} else if (anyVectorType.Signature == "com.threerings.math.Vector4f") {
				anyVectorType.SetField("x", 0f);
				anyVectorType.SetField("y", 0f);
				anyVectorType.SetField("z", 0f);
				anyVectorType.SetField("w", 0f);
			} else {
				throw new ArgumentException("Unknown vector type.");
			}
		}

	}
}
