using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OOOReader.Utility.Extension {
	/// <summary>
	/// Provides extensions to <see cref="Type"/>
	/// </summary>
	public static class TypeExtensions {

		/// <summary>
		/// Creates a new instance of the given type through a parameterless constructor.<para/>
		/// This is strictly suitable for cases where constraints such as <c>where T : new()</c> are not possible prior to compiletime, such as if
		/// the class is ported from another source.
		/// </summary>
		/// <param name="type">The type to create a new instance of.</param>
		/// <returns></returns>
		/// <inheritdoc cref="Type.GetConstructor(Type[])"/>
		/// <inheritdoc cref="ConstructorInfo.Invoke(object?[])"/>
		/// <exception cref="NullReferenceException">If <paramref name="type"/> is null or if a parameterless constructor is null.</exception>
		public static object NewInstance(this Type type) {
			return type.GetConstructor(Array.Empty<Type>())!.Invoke(null);
		}
		
		/// <summary>
		/// Returns all non-synthetic (compiler generated) fields.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public static IEnumerable<FieldInfo> GetNonSyntheticFields(this Type type, BindingFlags flags = BindingFlags.Default) {
			return type.GetFields(flags).Where(field => !field.IsDefined(typeof(CompilerGeneratedAttribute), false));
		}


		/// <summary>
		/// Returns true if ANY method exists on this type with the given name, no matter what its signature is.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public static bool HasMethodNamed(this Type type, string name, bool declaredOnly = false) {
			return type.GetMethods().FirstOrDefault(mtd => {
				if (mtd.Name == name) {
					if (!declaredOnly || (declaredOnly && mtd.DeclaringType == type)) {
						return true;
					}
				}
				return false;
			}) != null;
		}
	}
}
