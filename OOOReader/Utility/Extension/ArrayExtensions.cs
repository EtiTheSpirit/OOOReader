using System;
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
		/// Packs multiple parameters into an array.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="ts"></param>
		/// <returns></returns>
		[Obsolete("This is relatively useless in the current rendition of the program.")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T[] Pack<T>(params T[] ts) => ts;

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

	}
}
