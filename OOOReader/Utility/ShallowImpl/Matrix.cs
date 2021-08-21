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

	}
}
