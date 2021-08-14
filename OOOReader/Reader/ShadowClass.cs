#nullable enable
using OOOReader.Utility.Extension;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOOReader.Reader {

	/// <summary>
	/// A "shadow class" which is a generic container of arbitrary data that represents a Java class in the Clyde library. It cannot be extended.
	/// It can represent all four major formfactors of java types: <see langword="class"/>, <see langword="interface"/>, <see langword="enum"/>, and <see langword="@interface"/>.
	/// </summary>
	public sealed class ShadowClass {

		private static readonly Dictionary<string, ShadowClass> TEMPLATES = new Dictionary<string, ShadowClass>();

		/// <summary>
		/// The name of this class (fully-qualified, Java signature style not including the L and ; (so java/lang/Object not Ljava/lang/Object;).
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// The template type of this <see cref="ShadowClass"/>, or <see langword="null"/> if <see cref="IsTemplate"/> is <see langword="true"/>.
		/// </summary>
		public ShadowClass? TemplateType { get; }

		/// <summary>
		/// The type of Java class that this is.
		/// </summary>
		public ShadowType Type { get; }

		/// <summary>
		/// Whether or not this <see cref="ShadowClass"/> is a prototype, or, the master object that new instances of the same type (see <see cref="Name"/>) are created from.
		/// </summary>
		public bool IsTemplate { get; }

		/// <summary>
		/// If this is an inner class, <see cref="OuterClass"/> is a reference the outer class's template <see cref="ShadowClass"/> (that is, a <see cref="ShadowClass"/> where <see cref="IsTemplate"/> is <see langword="true"/> and its <see cref="Name"/> is the name of the outer class).
		/// </summary>
		public ShadowClass? OuterClass {
			get {
				if (!CheckedForOuterClass) {
					CheckedForOuterClass = true;
					if (Name.Contains('$')) {
						string outer = Name[0..(Name.LastIndexOf('$') + 1)];
						_OuterClass = TEMPLATES.GetValueOrDefault(outer);
					}
					_OuterClass = null;
				}
				return _OuterClass;
			}
		}
		private ShadowClass? _OuterClass = null;
		private bool CheckedForOuterClass = false;

		/// <summary>
		/// If this is an extension of another class, this is the base class.
		/// </summary>
		public ShadowClass? BaseClass {
			get {
				if (BaseClassName != null && _BaseClass == null) {
					_BaseClass = TEMPLATES[BaseClassName]; // Should never be null. Let the exception occur.
				}
				return _BaseClass;
			}
		}
		private ShadowClass? _BaseClass = null;
		private string? BaseClassName = null;

		/// <summary>
		/// Returns a new instance of a known class (from OOOClassDump.txt). Raises <see cref="ArgumentException"/> if the class name is not known.
		/// </summary>
		/// <remarks>
		/// A java fully qualified name looks like this: <c>java/lang/Object$SomeInnerClass</c> - If it starts with L and ends with ;, those chars will be stripped.
		/// </remarks>
		/// <param name="javaFullyQualifiedName"></param>
		/// <returns></returns>
		public static ShadowClass FromNamedType(string javaFullyQualifiedName) {
			if (javaFullyQualifiedName.StartsWith('L') && javaFullyQualifiedName.EndsWith(';')) {
				javaFullyQualifiedName = javaFullyQualifiedName[1..^1];
			}
			if (TEMPLATES.TryGetValue(javaFullyQualifiedName, out ShadowClass? shadow)) {
				return shadow!.Clone();
			}
			throw new ArgumentException("Unknown class.");
		}

		private static object? GetDefaultValueFromSignature(string signature) {
			object? value = default;
			bool makeArray = signature.StartsWith("[");
			if (makeArray) {
				signature = signature[1..];
			}
			if (signature == "Z") {
				value = default(bool);
			} else if (signature == "B") {
				value = default(byte);
			} else if (signature == "S") {
				value = default(short);
			} else if (signature == "I") {
				value = default(int);
			} else if (signature == "J") {
				value = default(long);
			} else if (signature == "F") {
				value = default(float);
			} else if (signature == "D") {
				value = default(double);
			}
			if (makeArray) {
				if (value == null) {
					if (TEMPLATES.TryGetValue(signature, out ShadowClass? template)) {
						value = new ShadowClassArray(template);
					} else {
						value = new PendingShadowClassArray(signature);
					}
				} else {
					value = Array.CreateInstance(value.GetType(), 0);
				}
			}

			return value;
		}

		static ShadowClass() {
			string[] clsDump = File.ReadAllLines("./data/OOOClassDump.txt");
			string? className = default;
			foreach (string info in clsDump) {
				string code = info.Substring(0, 2);
				string? baseClass = null;
				ShadowType type;
				if (code == "CL") {
					// Class
					type = ShadowType.Class;
				} else if (code == "IF") {
					// Interface
					type = ShadowType.Interface;
				} else if (code == "EN") {
					// Enum
					type = ShadowType.Enum;
				} else if (code == "AN") {
					// Annotation
					type = ShadowType.Annotation;
				} else {
					if (!string.IsNullOrWhiteSpace(className)) {
						ShadowClass shadow = TEMPLATES[className];
						string[] fInfo = info[1..].Split(' ');
						string name = fInfo[0];
						string signature = fInfo[2];
						shadow.Fields[name] = GetDefaultValueFromSignature(signature);
					}
					return;
				}

				className = info[3..];
				if (className.Contains(':')) {
					string[] splitName = className.Split(':');
					className = splitName[0];
					baseClass = splitName[1];
				}
				ShadowClass instance = new ShadowClass(className, baseClass, type);
				TEMPLATES[className] = instance;
			}

			foreach (KeyValuePair<string, ShadowClass> data in TEMPLATES) {
				foreach (KeyValuePair<string, object?> fieldInfo in data.Value.Fields.Copy()) {
					if (fieldInfo.Value is PendingShadowClassArray pendingArray) {
						data.Value.Fields[fieldInfo.Key] = pendingArray.Create();
					}
				}
			}
		}

		private ShadowClass(string name, string? baseClassName, ShadowType type) {
			Name = name;
			BaseClassName = baseClassName;
			Type = type;
			IsTemplate = true;
		}

		private ShadowClass(ShadowClass other) : this(other.Name, other.BaseClassName, other.Type) {
			other.Fields.CopyTo(Fields);

			_OuterClass = other._OuterClass;
			CheckedForOuterClass = other.CheckedForOuterClass;

			_BaseClass = other._BaseClass;
			// Name is copied already

			if (other.IsTemplate) {
				TemplateType = other;
			} else {
				TemplateType = other.TemplateType;
			}
		}

		private readonly Dictionary<string, object?> Fields = new Dictionary<string, object?>();

		/// <summary>
		/// Clones this <see cref="ShadowClass"/> into a new instance. Mostly useful for templates (where <see cref="IsTemplate"/> is <see langword="true"/>).
		/// </summary>
		/// <returns>The new instance using the same values as this.</returns>
		public ShadowClass Clone() {
			return new ShadowClass(this);
		}

		/// <summary>
		/// Returns a copy of the array representing every field name in this <see cref="ShadowClass"/>.
		/// </summary>
		/// <returns></returns>
		public string[] GetFieldNames() {
			return Fields.Keys.ToArray();
		}

		/// <summary>
		/// Reads a field with the given name.
		/// </summary>
		/// <param name="name">The name of the field to read from.</param>
		/// <returns>The object stored in this field.</returns>
		/// <exception cref="ArgumentException">If the field name is invalid.</exception>
		/// <exception cref="MissingFieldException">If a field with this name has not been set.</exception>
		public object? ReadField(string name) {
			if (IsTemplate) throw new InvalidOperationException("Cannot call ReadField on a template object.");
			if (TryReadField(name, out object? value)) {
				return value;
			}
			throw new MissingFieldException(Name, name);
		}

		private bool TryReadField(string name, out object? value) {
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Invalid field name.");
			if (Fields.TryGetValue(name, out value)) {
				return true;
			}
			if (BaseClass != null) {
				if (BaseClass.TryReadField(name, out value)) {
					return true;
				}
			}

			value = default;
			return false;
		}

		/// <inheritdoc cref="ReadField(string)"/>
		/// <remarks>
		/// May be <see langword="null"/> if <typeparamref name="T"/> is a reference type.
		/// </remarks>
		/// <typeparam name="T">The type of object to return as.</typeparam>
		public T? ReadField<T>(string name) => (T?)ReadField(name);

		/// <summary>
		/// Sets (or adds) a field with the given name.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		/// <param name="addToThisObject">If true, the value will be added to this object if it's not already defined here (only useful if <see cref="BaseClass"/> is set and <em>does</em> have a field with this name, and you want to write to this instead of the base.</param>
		/// <returns></returns>
		public void SetField(string name, object value, bool addToThisObject = false) {
			if (IsTemplate) throw new InvalidOperationException("Cannot call SetField on a template object.");
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Invalid field name.");
			if (addToThisObject || BaseClass == null || Fields.ContainsKey(name)) {
				Fields[name] = value;
			} else {
				// Base class exists and said field is not part of this. Set base?
				if (BaseClass.TryReadField(name, out object _)) {
					BaseClass.SetField(name, value); // Yes, base has a field with this name.
				} else {
					Fields[name] = value; // No, just add it here instead.
				}
			}
		}

		/// <summary>
		/// A pending shadow class array instance. Used for populating fields early.
		/// </summary>
		private class PendingShadowClassArray {
			private readonly string Signature;

			public PendingShadowClassArray(string signature) {
				Signature = signature;
			}

			public ShadowClassArray Create() {
				return new ShadowClassArray(TEMPLATES[Signature]);
			}

		}
		
		/// <summary>
		/// A shadow class array. It enforces that its elements share the same template type.
		/// </summary>
		public class ShadowClassArray : IEnumerable<ShadowClass> {

			/// <summary>
			/// The template <see cref="ShadowClass"/> used as this array's element type.
			/// </summary>
			public ShadowClass ElementType { get; }

			/// <summary>
			/// The number of elements in this array.
			/// </summary>
			public int Length => InternalArray.Count;

			private readonly List<ShadowClass> InternalArray;

			/// <summary>
			/// Adds the given <see cref="ShadowClass"/> instance to this array.
			/// </summary>
			/// <param name="instance"></param>
			/// <exception cref="ArgumentException">If the input instance is a template.</exception>
			/// <exception cref="ArrayTypeMismatchException">If the template type of the input instance is not the same as this array's element type.</exception>
			public void Add(ShadowClass instance) {
				if (instance.IsTemplate) throw new ArgumentException("Cannot add a template object to an array of ShadowClass instances.");
				if (instance.TemplateType != ElementType) throw new ArrayTypeMismatchException("Template type mismatch. Input ShadowClass is instance of " + instance.TemplateType!.Name + " but this array stores instances of " + ElementType.Name);
				InternalArray.Add(instance);
			}

			/// <summary>
			/// Removes the given <see cref="ShadowClass"/> from this array, returning whether or not the item actually existed here in the first place.
			/// </summary>
			/// <param name="instance"></param>
			/// <returns></returns>
			public bool Remove(ShadowClass instance) => InternalArray.Remove(instance);

			/// <exception cref="ArgumentException">If the input instance is a template.</exception>
			/// <exception cref="ArrayTypeMismatchException">If the template type of the input instance is not the same as this array's element type.</exception>
			public ShadowClass this[int index] {
				get => InternalArray[index];
				set {
					if (value == null) throw new ArgumentNullException(nameof(value));
					if (value.IsTemplate) throw new ArgumentException("Cannot add a template object to an array of ShadowClass instances.");
					if (value.TemplateType != ElementType) throw new ArrayTypeMismatchException("Template type mismatch. Input ShadowClass is instance of " + value.TemplateType!.Name + " but this array stores instances of " + ElementType.Name);
					InternalArray[index] = value;
				}
			}

			public ShadowClassArray(ShadowClass template, int length = 0) {
				if (!template.IsTemplate) throw new ArgumentException("Unexpected parameter for 'template' (input ShadowClass is not a template)");
				ElementType = template;
				InternalArray = new List<ShadowClass>(length);
				for (int i = 0; i < length; i++) {
					InternalArray[i] = template.Clone();
				}
			}

			public IEnumerator<ShadowClass> GetEnumerator() => InternalArray.GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => InternalArray.GetEnumerator();
		}

		public enum ShadowType {

			/// <summary>
			/// Represents that this type is a class.
			/// </summary>
			Class,

			/// <summary>
			/// Represents that this type is an interface.
			/// </summary>
			Interface,

			/// <summary>
			/// Represents that this type is an enum.
			/// </summary>
			Enum,

			/// <summary>
			/// Represents that this type is an annotation.
			/// </summary>
			Annotation,

		}

	}
}
