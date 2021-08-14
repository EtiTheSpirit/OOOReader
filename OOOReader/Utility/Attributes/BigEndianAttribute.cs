using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOOReader.Utility.Attributes {
	/// <summary>
	/// Denotes a field or property is written as if it's using big endian byte order. Any constant values should have their byte order flipped when compared to a value read in a stream that is big endian.
	/// </summary>
	/// <remarks>
	/// <strong>This field is written in big endian byte order</strong>, and should be flipped when compared to any values read from a stream that are big endian (or the value read from the stream should be flipped)
	/// </remarks>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public sealed class BigEndianAttribute : Attribute { }
}
