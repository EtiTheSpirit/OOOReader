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
		/// <param name="correctTypes">If true, and if this element type is a primitive / known value type (such as <c>java.lang.String</c>), then the array's type will be the system equivalent.</param>
		/// <returns></returns>
		public Array NewInstance(int length = 0, int depth = 1, bool correctTypes = true) {
			int[] lengths = new int[depth];
			for (int idx = 0; idx < depth; idx++) {
				lengths[idx] = length;
			}
			
			Type t = typeof(ShadowClass);
			if (correctTypes) {
				string signature = ElementType!.Signature;
				if (signature == "java.lang.String") {
					t = typeof(string);
				} else if (signature == "java.lang.Boolean") {
					t = typeof(bool);
				} else if (signature == "java.lang.Char") {
					t = typeof(char);
				} else if (signature == "java.lang.Byte") {
					t = typeof(byte);
				} else if (signature == "java.lang.Short") {
					t = typeof(short);
				} else if (signature == "java.lang.Integer") {
					t = typeof(int);
				} else if (signature == "java.lang.Long") {
					t = typeof(long);
				} else if (signature == "java.lang.Float") {
					t = typeof(float);
				} else if (signature == "java.lang.Double") {
					t = typeof(double);
				}
			}
			Array ret = Array.CreateInstance(t, lengths);
			if (t == typeof(ShadowClass)) {
				for (int idx = 0; idx < ret.Length; idx++) {
					ret.SetValue(ElementType!.Clone(), idx); // This works on all dimensions.
				}
			}
			return ret;
		}

		public override string ToString() {
			return $"ShadowClassArray [Type={Signature}]";
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
				if (ShadowClass.TEMPLATES.TryGetValue(Signature, out ShadowClass? template)) {
					return new ShadowClassArray(template);
				} else {
					return new ShadowClassArray(ShadowClass.TEMPLATES["java.lang.Object"]);
				}
			}

		}

	}

}
