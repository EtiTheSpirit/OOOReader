using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OOOReader.Utility.Extension {
	/// <summary>
	/// Array utilities.
	/// </summary>
	public static class ArrayExtensions {

		/// <summary>
		/// Assuming this is an instance of <see cref="Array"/>, this will make a 2D array of its contents.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="array"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">If the input array's <see cref="Array.Rank"/> is not 2.</exception>
		public static T[,] Make2D<T>(this Array array) {
			if (array.Rank != 2) {
				throw new ArgumentException("Cannot call " + nameof(Make2D) + " on an array whose Rank is not equal to 2!");
			}
			return (T[,])array; // TODO: Does this even work?
		}

		public static string ArrayToString(this IEnumerable array, int depth = 1) {
			StringBuilder sb = new StringBuilder('[');
			sb.AppendLine();
			foreach (object o in array) {
				if (o is IDictionary dictionary) {
					foreach (KeyValuePair<object, object> info in dictionary) {
						sb.AppendLine();
						sb.Append(new string('\t', depth));
						sb.Append('[');
						sb.Append(info.Key.ToString());
						sb.Append("]=");
						object value = info.Value;
						if (value is IEnumerable dEnumerable) {
							sb.Append(ArrayToString(dEnumerable, depth + 1));
						} else {
							sb.Append(value?.ToString() ?? "null");
						}
					}
				} else if (o is IEnumerable enumerable) {
					sb.Append(ArrayToString(enumerable, depth + 1));
				}
				sb.AppendLine();
			}
			sb.Append(']');
			return sb.ToString();
		}

	}
}
