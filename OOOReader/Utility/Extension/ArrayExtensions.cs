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
		/// Wraps multiple parameters into an array.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="ts"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T[] Wrap<T>(params T[] ts) => ts;


	}
}
