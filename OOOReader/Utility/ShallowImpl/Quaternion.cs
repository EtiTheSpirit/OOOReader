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

	}
}
