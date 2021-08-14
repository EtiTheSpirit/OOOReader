using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOOReader.Utility.Extension {
	/// <summary>
	/// Adds more methods to enumerables and dictionaries.
	/// </summary>
	public static class EnumerableExtensions {

		/// <summary>
		/// Copies the contents from this dictionary into a new instance.
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="dict"></param>
		/// <returns></returns>
		public static Dictionary<TKey, TValue> Copy<TKey, TValue>(this Dictionary<TKey, TValue> dict) where TKey : notnull {
			Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();
			foreach (KeyValuePair<TKey, TValue> data in dict) {
				dictionary.Add(data.Key, data.Value);
			}
			return dictionary;
		}

		/// <summary>
		/// Shallow copies all entries from this dictionary into the destination dictionary, overwriting any identical keys that are present in the destination. Other keys will be preserved.
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="dict"></param>
		/// <param name="destination"></param>
		/// <returns></returns>
		public static void CopyTo<TKey, TValue>(this Dictionary<TKey, TValue> dict, Dictionary<TKey, TValue> destination) where TKey : notnull {
			foreach (KeyValuePair<TKey, TValue> data in dict) {
				destination[data.Key] = data.Value;
			}
		}

	}
}
