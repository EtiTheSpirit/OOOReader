using OOOReader.Reader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOOReader.Utility.ShallowImpl {
	public static class ArticulatedConfigNode {

		public static void UpdateRefTransforms(ShadowClass articulatedConfigNode, ShadowClass parentRefTransform3D) {
			ShadowClass refTransform = Transform3D.Compose(parentRefTransform3D, articulatedConfigNode["transform"]);
			articulatedConfigNode["invRefTransform"] = Matrix.Invert(refTransform);
			Array nodes = articulatedConfigNode["children"];
			for (int i = 0; i < nodes.Length; i++) {
				ShadowClass childNode = (ShadowClass)nodes.GetValue(i);
				UpdateRefTransforms(childNode, refTransform);
			}
		}

	}
}
