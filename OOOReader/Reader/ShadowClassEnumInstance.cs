using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOOReader.Reader {
	public class ShadowClassEnumInstance {

		public ShadowClass Type { get; }

		/// <summary>
		/// The name of this enum item. Contrary to C# enums, these do not have numeric values associated with them.
		/// </summary>
		public string Name { get; }

		internal ShadowClassEnumInstance(ShadowClass parent, string name) {
			if (!parent.IsTemplate) throw new ArgumentException("Cannot create a ShadowClassEnumInstance for a non-template ShadowClass!");
			Type = parent;
			Name = name;
		}

	}
}
