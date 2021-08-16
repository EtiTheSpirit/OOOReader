#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOOReader.Reader {


	/// <summary>
	/// A shadow class array. It enforces that its elements share the same template type.
	/// </summary>
	public sealed class ShadowClassArray : AbstractShadowClassBase {

		public override string Signature => ElementType!.Name;

		public override bool IsSealed => true;

		public ShadowClassArray(ShadowClass template) {
			if (!template.IsTemplate) throw new ArgumentException("Unexpected parameter for 'template' (input ShadowClass is not a template)");
			ElementType = template;
		}
		
		/// <summary>
		/// Create a new instance of an array of the given <see cref="ShadowClass"/> element type (see <see cref="AbstractShadowClassBase.ElementType"/>)
		/// </summary>
		/// <param name="length"></param>
		/// <param name="depth"></param>
		/// <returns></returns>
		public Array NewInstance(int length = 0, int depth = 1) {
			int[] lengths = new int[depth];
			for (int idx = 0; idx < depth; idx++) {
				lengths[idx] = length;
			}
			Array ret = Array.CreateInstance(typeof(ShadowClass), lengths);
			for (int idx = 0; idx < ret.Length; idx++) {
				ret.SetValue(ElementType!.Clone(), idx); // This works on all dimensions.
			}
			return ret;
		}

		/// <summary>
		/// A pending shadow class array instance. Used for populating fields early.
		/// When this is used, it is used in place of a <see cref="ShadowClassArray"/> because the element type of the array does not (yet) exist.
		/// Calling <see cref="Create"/> will return a new <see cref="ShadowClassArray"/> with the proper element type, assuming it has been created prior to its calling.
		/// </summary>
		internal class PendingShadowClassArray : AbstractShadowClassBase {

			/// <summary>
			/// Not applicable for <see cref="PendingShadowClassArray"/>; always raises <see cref="InvalidOperationException"/>.
			///	</summary>
			public override ShadowClass? ElementType {
				get => throw new InvalidOperationException("A PendingShadowClassArray instance does not have an element type, only an actual ShadowClassArray does.");
				protected set => throw new InvalidOperationException("A PendingShadowClassArray instance does not have an element type, only an actual ShadowClassArray does.");
			}

			public PendingShadowClassArray(string signature) {
				Signature = signature;
			}

			public ShadowClassArray Create() {
				return new ShadowClassArray(ShadowClass.TEMPLATES[Signature]);
			}

		}

	}

}
