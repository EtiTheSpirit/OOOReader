using OOOReader.Utility.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOOReader.Clyde {

	/// <summary>
	/// Represents a version of the Clyde model format.
	/// </summary>
	public enum ClydeVersion : ushort {

		/// <summary>
		/// Represents the Classic clyde model version, where IDs used the nearest required data type and stepped up in size as IDs were used, and segment lengths were always Int32
		/// </summary>
		[BigEndian]
		Classic = 0x1000,

		/// <summary>
		/// An intermediate version between <see cref="Classic"/> and <see cref="VarInt"/> version where IDs use the newer VarInt standard, but segment lengths still use Int32.
		/// </summary>
		[BigEndian]
		Intermediate = 0x1001,

		/// <summary>
		/// The final Clyde model version, where both IDs and segment lengths use VarInts.
		/// </summary>
		[BigEndian]
		VarInt = 0x1002

	}
}
