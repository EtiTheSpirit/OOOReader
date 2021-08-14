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
	/// It can represent all four major formfactors of java types: <see langword="class"/>, <see langword="interface"/>, <see langword="enum"/>, and <see langword="@interface"/> (annotations).
	/// </summary>
	public sealed class ShadowClass : AbstractShadowClassBase {

		private static readonly Dictionary<string, ShadowClass> TEMPLATES = new Dictionary<string, ShadowClass>();

		/// <summary>
		/// The name of this class (fully-qualified, Java signature style not including the L and ; (so java/lang/Object not Ljava/lang/Object;).
		/// </summary>
		/// <remarks>
		/// This is identical to <see cref="AbstractShadowClassBase.Signature"/>
		/// </remarks>
		public string Name {
			get => Signature;
			set => Signature = value;
		}

		/// <summary>
		/// Not applicable for <see cref="ShadowClass"/>; always raises <see cref="InvalidOperationException"/>.
		///	</summary>
		public override ShadowClass? ElementType {
			get => throw new InvalidOperationException("A ShadowClass instance does not have an element type.");
			protected set => throw new InvalidOperationException("A ShadowClass instance does not have an element type.");
		}

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
		private readonly string? BaseClassName = null;


		/// <summary>
		/// The fields in this ShadowClass.
		/// </summary>
		private readonly Dictionary<string, object?> Fields = new Dictionary<string, object?>();

		/// <summary>
		/// The types of values in the fields of this ShadowClass. The type values will be usable to look up something in <see cref="TEMPLATES"/>.
		/// </summary>
		private readonly Dictionary<string, string> FieldOrFieldElementTypes = new Dictionary<string, string>();

		/// <summary>
		/// Returns a new instance of a class (from OOOClassDump.txt) as a ShadowClass. If the class name is not recognized, this raises
		/// of <see cref="ShadowClass"/> with no predefined fields or field types and no base type.
		/// </summary>
		/// <remarks>
		/// A java fully qualified name looks like this: <c>java/lang/Object$SomeInnerClass</c> - If it starts with L and ends with ;, those chars will be stripped.
		/// </remarks>
		/// <param name="javaFullyQualifiedName"></param>
		/// <param name="baseTypeName">Only used if the <see cref="ShadowClass"/> does not already have a template. This is the base class that this shadow will use.</param>
		/// <returns></returns>
		public static ShadowClass GetOrCreate(string javaFullyQualifiedName, string? baseTypeName = null) {
			if (javaFullyQualifiedName.StartsWith('L') && javaFullyQualifiedName.EndsWith(';')) {
				javaFullyQualifiedName = javaFullyQualifiedName[1..^1];
			}
			if (TEMPLATES.TryGetValue(javaFullyQualifiedName, out ShadowClass? shadow)) {
				return shadow!.Clone();
			}
			return GetOrCreateTemplate(javaFullyQualifiedName, baseTypeName, new Dictionary<string, object?>(), new Dictionary<string, string>()).Clone();
		}

		/// <summary>
		/// Returns the template representation of a class (from OOOClassDump.txt). If the class name is not recognized, this returns a new instance
		/// of a <see cref="ShadowClass"/> which is a template, hence the requirement of a field and field type dictionary. This will register the
		/// template as well.
		/// </summary>
		/// <remarks>
		/// <strong>This always returns the template object. Use <see cref="Clone"/> to get a workable instance.</strong><para/>
		/// A java fully qualified name looks like this: <c>java/lang/Object$SomeInnerClass</c> - If it starts with L and ends with ;, those chars will be stripped.
		/// </remarks>
		/// <param name="javaFullyQualifiedName"></param>
		/// <param name="fieldValues">The default values for fields. If the values are objects, consider defining their value type in <paramref name="shadowedFieldTypeValues"/>.</param>
		/// <param name="shadowedFieldTypeValues">For fields whose values are <see cref="ShadowClass"/> instances (or <see cref="ShadowClassArray"/> instances), this is the fully-qualified type name that can be used in <see cref="GetOrCreate(string)"/>.</param>
		/// <returns></returns>
		public static ShadowClass GetOrCreateTemplate(string javaFullyQualifiedName, string? baseClassName, Dictionary<string, object?> fieldValues, Dictionary<string, string> shadowedFieldTypeValues) {
			if (javaFullyQualifiedName.StartsWith('L') && javaFullyQualifiedName.EndsWith(';')) {
				javaFullyQualifiedName = javaFullyQualifiedName[1..^1];
			}
			if (TEMPLATES.TryGetValue(javaFullyQualifiedName, out ShadowClass? shadow)) {
				return shadow;
			}
			ShadowClass ret = new ShadowClass(javaFullyQualifiedName, baseClassName, ShadowType.Class);
			fieldValues.CopyTo(ret.Fields);
			shadowedFieldTypeValues.CopyTo(ret.FieldOrFieldElementTypes);
			TEMPLATES[javaFullyQualifiedName] = ret;
			return ret;
		}

		/// <summary>
		/// Using OOO's default format of a fully-qualified class signature and a set of flags, this will return a new <strong>Template</strong> <see cref="ShadowClass"/>,
		/// or a <see cref="ShadowClassArray"/>.
		/// </summary>
		/// <param name="className"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public static AbstractShadowClassBase CreateFromOOOFormat(string className, byte flags) {
			(object? def, string sanitizedClassName) = GetDefaultValueFromSignature(className);
			if (def is null) {
				return GetOrCreateTemplate(sanitizedClassName, null, new Dictionary<string, object?>(), new Dictionary<string, string>());
			}
			if (def is PendingShadowClassArray pending) {
				GetOrCreateTemplate(sanitizedClassName, null, new Dictionary<string, object?>(), new Dictionary<string, string>());
				def = pending.Create(); // And since ^ will have made the template, this method works now.
			}
			if (def.GetType() != typeof(ShadowClassArray)) {
				// Now at THIS point if it's not an array, it's wrong.
				throw new ArgumentException("The input class name caused an unexpected class type of " + def.GetType() + " to be created.");
			}
			return (def as AbstractShadowClassBase)!;
		}

		private static (object?, string) GetDefaultValueFromSignature(string signature) {
			object? value = default;
			int arrayDepth = 0;
			while (signature.StartsWith('[')) {
				arrayDepth++;
				signature = signature[1..];
			}
			if (signature == "Z") {
				value = default(bool);
			} else if (signature == "B") {
				value = default(byte);
			} else if (signature == "C") {
				value = default(char);
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
			if (arrayDepth > 0) {
				if (value == null) {
					if (TEMPLATES.TryGetValue(signature, out ShadowClass? template)) {
						value = new ShadowClassArray(template);
					} else {
						if (signature.StartsWith('L') && signature.EndsWith(';')) {
							signature = signature[1..^1];
						}
						value = new PendingShadowClassArray(signature);
					}
				} else {
					value = Array.CreateInstance(value.GetType(), 0);
				}
			}
			if (signature.StartsWith('L') && signature.EndsWith(';')) {
				signature = signature[1..^1];
			}
			return (value, signature);
		}

		static ShadowClass() {
			TEMPLATES["java/lang/String"] = new ShadowClass("java/lang/String", null, ShadowType.Class);
			TEMPLATES["java/lang/Object"] = new ShadowClass("java/lang/Object", null, ShadowType.Class);

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
						string signature = fInfo[1];
						(object? value, string cleanSignature) = GetDefaultValueFromSignature(signature);
						shadow.Fields[name] = value;
						Type? shadowType = shadow.Fields[name]?.GetType();
						if (shadow.Fields[name] is AbstractShadowClassBase absBase) {
							shadow.FieldOrFieldElementTypes[name] = absBase.Signature;
						} else if (shadow.Fields[name] is null) {
							shadow.FieldOrFieldElementTypes[name] = cleanSignature; // Important to NOT create here.
						}
					}
					continue;
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
			IsTemplate = false;

			other.Fields.CopyTo(Fields);
			other.FieldOrFieldElementTypes.CopyTo(FieldOrFieldElementTypes);

			_OuterClass = other.OuterClass; // use the actual property so it's evaluated
			CheckedForOuterClass = other.CheckedForOuterClass;

			_BaseClass = other._BaseClass;
			// Name is copied already

			if (other.IsTemplate) {
				TemplateType = other;
			} else {
				TemplateType = other.TemplateType;
			}
		}

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
				if (FieldOrFieldElementTypes.TryGetValue(name, out string? fieldSig)) {
					ShadowClass templateShadow = TEMPLATES[fieldSig];
					if (value is ShadowClass shadowClass && shadowClass.TemplateType != templateShadow) {
						throw new ArgumentException("The field '" + name + "' is supposed to be an instance of ShadowClass pointing to " + TEMPLATES[fieldSig].Name);
					} else if (value is ShadowClassArray shadowClassArray && shadowClassArray.ElementType != templateShadow) {
						throw new ArgumentException("The field '" + name + "' is supposed to be an instance of ShadowClassArray with an element type of " + TEMPLATES[fieldSig].Name);
					} else if (value is PendingShadowClassArray) {
						throw new InvalidOperationException("The type of this value is a PendingShadowClassArray which should not be possible.");
					}
				}
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

		public override string ToString() {
			return $"ShadowClass[Type={Name}, IsTemplate={IsTemplate}]";
		}

		/// <summary>
		/// A pending shadow class array instance. Used for populating fields early.
		/// When this is used, it is used in place of a <see cref="ShadowClassArray"/> because the element type of the array does not (yet) exist.
		/// Calling <see cref="Create"/> will return a new <see cref="ShadowClassArray"/> with the proper element type, assuming it has been created prior to its calling.
		/// </summary>
		private class PendingShadowClassArray : AbstractShadowClassBase {

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
				return new ShadowClassArray(TEMPLATES[Signature]);
			}

		}
		
		/// <summary>
		/// A shadow class array. It enforces that its elements share the same template type.
		/// </summary>
		public class ShadowClassArray : AbstractShadowClassBase {

			public override string Signature { 
				get => ElementType!.Name;
				protected set => throw new InvalidOperationException();
			}

			/// <summary>
			/// A reference to the array.
			/// </summary>
			public Array Array => InternalArray;

			//private readonly List<ShadowClass> InternalArray;
			private readonly Array InternalArray;

			/*
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
			*/

			public ShadowClassArray(ShadowClass template, int length = 0, int depth = 1) {
				if (!template.IsTemplate) throw new ArgumentException("Unexpected parameter for 'template' (input ShadowClass is not a template)");
				ElementType = template;
				int[] lengths = new int[depth];
				for (int idx = 0; idx < depth; idx++) {
					lengths[idx] = length;
				}
				InternalArray = Array.CreateInstance(typeof(ShadowClass), lengths);
				for (int idx = 0; idx < InternalArray.Length; idx++) {
					InternalArray.SetValue(template.Clone(), idx); // This works on all dimensions.
				}
			}

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
