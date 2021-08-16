#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOOReader.Reader {

	/// <summary>
	/// A <see cref="ShadowClass"/> implementation that intends to represent primitive value types.
	/// </summary>
	[Obsolete("Primitives are now default for ShadowClasses", true)] // Maybe?
	public sealed class PrimitiveShadowClass : AbstractShadowClassBase {

		public override ShadowClass? ElementType => throw new InvalidOperationException();

		public override bool IsSealed => true;

		public override string Signature => Type.FullName!.Replace(".", "/").Replace("+", "$");

		/// <summary>
		/// The type of the value stored in this instance.
		/// </summary>
		public Type Type { get; }

		public object Value { get; }

		public PrimitiveShadowClass(object value) {
			Type = value.GetType();
			Value = value;
		}

	}
}
