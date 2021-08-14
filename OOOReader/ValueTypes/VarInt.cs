using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOOReader.ValueTypes {
	/// <summary>
	/// Represents a number with a variable number of bytes depending on its needs.
	/// </summary>
	public readonly struct VarInt : IEquatable<VarInt>, IComparable<VarInt> {

		/// <summary>
		/// The highest possible value and size stored in a <see cref="VarInt"/>
		/// </summary>
		public static readonly VarInt MaxValue = new VarInt(ulong.MaxValue, 8);

		/// <summary>
		/// The lowest possible value and size stored in a <see cref="VarInt"/>
		/// </summary>
		public static readonly VarInt MinValue = new VarInt(0, 1);

		/// <summary>
		/// An invalid <see cref="VarInt"/> with a zero byte size. Identical to <see langword="default"/>.
		/// </summary>
		public static readonly VarInt Invalid = default;

		/// <summary>
		/// The value stored within this <see cref="VarInt"/>.
		/// </summary>
		public readonly ulong Value;

		/// <summary>
		/// The number of bytes this <see cref="VarInt"/> uses.
		/// </summary>
		public readonly int Size;

		/// <summary>
		/// Constructs a new <see cref="VarInt"/> from the given value.
		/// </summary>
		public VarInt(ulong value, int size) {
			Value = value;
			Size = size;
		}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		#region VarInt => Numeric Types
		public static implicit operator ulong(VarInt @in) {
			checked {
				return @in.Value;
			}
		}

		public static implicit operator uint(VarInt @in) {
			checked {
				return (uint)@in.Value;
			}
		}

		public static implicit operator ushort(VarInt @in) {
			checked {
				return (ushort)@in.Value;
			}
		}

		public static implicit operator sbyte(VarInt @in) {
			checked {
				return (sbyte)@in.Value;
			}
		}

		public static implicit operator long(VarInt @in) {
			checked {
				return (long)@in.Value;
			}
		}

		public static implicit operator int(VarInt @in) {
			checked {
				return (int)@in.Value;
			}
		}

		public static implicit operator short(VarInt @in) {
			checked {
				return (short)@in.Value;
			}
		}

		public static implicit operator byte(VarInt @in) {
			checked {
				return (byte)@in.Value;
			}
		}
		#endregion

		#region Numeric Types => VarInt
		public static explicit operator VarInt(ulong @in) {
			return new VarInt(@in, 8);
		}

		public static explicit operator VarInt(uint @in) {
			return new VarInt(@in, 4);
		}

		public static explicit operator VarInt(ushort @in) {
			return new VarInt(@in, 2);
		}

		public static explicit operator VarInt(byte @in) {
			return new VarInt(@in, 1);
		}
		#endregion
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Returns whether or not this <see cref="VarInt"/> and the given <paramref name="obj"/> have the same value and size, granted it's a <see cref="VarInt"/>.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) {
			if (obj is VarInt varInt) return Equals(varInt);
			return false;
		}

		/// <inheritdoc/>
		public override int GetHashCode() {
			return Value.GetHashCode();
		}

		/// <summary>
		/// Returns a string describing the value stored inside and the amount of bytes this takes.
		/// </summary>
		/// <returns></returns>
		public override string ToString() => $"[VarInt={Value} // Size={Size} B]";

		/// <summary>
		/// Returns whether or not this <see cref="VarInt"/> and the given <paramref name="other"/> <see cref="VarInt"/> have the same value and size.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(VarInt other) {
			return Value == other.Value && Size == other.Size;
		}

		/// <summary>
		/// Sorts this <see cref="VarInt"/> by its stored <see cref="Value"/>. <see cref="Size"/> has no bearing on the sort order.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public int CompareTo(VarInt other) {
			return Value.CompareTo(other.Value);
		}

		#region Equality
#pragma warning disable CS1591
		public static bool operator ==(VarInt left, VarInt right) {
			return left.Equals(right);
		}

		public static bool operator !=(VarInt left, VarInt right) {
			return !(left == right);
		}

		public static bool operator <(VarInt left, VarInt right) {
			return left.CompareTo(right) < 0;
		}

		public static bool operator <=(VarInt left, VarInt right) {
			return left.CompareTo(right) <= 0;
		}

		public static bool operator >(VarInt left, VarInt right) {
			return left.CompareTo(right) > 0;
		}

		public static bool operator >=(VarInt left, VarInt right) {
			return left.CompareTo(right) >= 0;
		}
#pragma warning restore CS1591
		#endregion
	}
}
