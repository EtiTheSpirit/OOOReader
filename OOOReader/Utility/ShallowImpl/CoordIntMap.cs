using OOOReader.Reader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOOReader.Utility.ShallowImpl {

	[Obsolete("May not be needed; testing.")]
	public class CoordIntMap {

		public static List<CoordIntEntry> CoordIntEntrySet() {
			return null;
		}

		public class CoordIntEntry {

			public ShadowClass Key { get; protected set; } = ShadowClass.CreateInstanceOf("com.threerings.tudey.util.Coord");
			protected int[] Values;
			protected int Index;

			public CoordIntEntry() { }

			public int GetValue() {
				return Values[Index];
			}

			public int SetValue(int value) {
				int oldValue = Values[Index];
				Values[Index] = value;
				return oldValue;
			}

			public int GetIntValue() => Values[Index];

			public override bool Equals(object obj) {
				if (obj is CoordIntEntry coord) {
					return coord.Key == Key && coord.GetValue() == GetValue();
				}
				return false;
			}

			public override int GetHashCode() {
				int coordHash = Key["x"] + 31 * Key["y"];
				return coordHash ^ GetValue();
			}


		}

	}
}
