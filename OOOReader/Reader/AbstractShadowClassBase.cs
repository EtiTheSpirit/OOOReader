#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOOReader.Reader {
	public abstract class AbstractShadowClassBase {

		/// <summary>
		/// The signature of this <see cref="ShadowClass"/> or <see cref="ShadowClass.ShadowClassArray"/> element type.
		/// </summary>
		public virtual string Signature { get; protected set; } = string.Empty;

		/// <summary>
		/// If this is a <see cref="ShadowClass.ShadowClassArray"/> or <see cref="ShadowClass.PendingShadowClassArray"/>, then this is the element type's actual instance (created from <see cref="Signature"/>)
		/// </summary>
		public virtual ShadowClass? ElementType { get; protected set; }

	}
}
