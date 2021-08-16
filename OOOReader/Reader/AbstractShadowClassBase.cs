#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOOReader.Reader {

	/// <summary>
	/// The base class for all <see cref="ShadowClass"/>-based instances.
	/// </summary>
	public abstract class AbstractShadowClassBase {

		/// <summary>
		/// The signature of this <see cref="ShadowClass"/> or <see cref="ShadowClass.ShadowClassArray"/> element type.
		/// </summary>
		/// <remarks>
		/// This is a fully-qualified, Java-style signature not including the L and ; (so <c>java/lang/Object</c> not <c>Ljava/lang/Object;</c>).
		/// </remarks>
		public virtual string Signature { get; protected set; } = string.Empty;

		/// <summary>
		/// Whether or not this <see cref="ShadowClass"/> represents a <see langword="sealed"/> class (or in Java, a <see langword="final"/> class).
		/// </summary>
		/// <remarks>
		/// This is only important in the context that <see langword="final"/> and <see langword="sealed"/> classes cannot be extended.
		/// </remarks>
		public virtual bool IsSealed { get; protected set; } = false;

		/// <summary>
		/// If this is a <see cref="ShadowClass.ShadowClassArray"/> or <see cref="ShadowClass.PendingShadowClassArray"/>, 
		/// then this is the element type's actual instance (created from <see cref="Signature"/>)
		/// </summary>
		/// <exception cref="InvalidOperationException">If this implementation does not use an element type.</exception>
		public virtual ShadowClass? ElementType { get; protected set; }

	}
}
