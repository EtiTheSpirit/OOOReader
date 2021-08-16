#nullable enable
using OOOReader.Clyde;
using OOOReader.Utility.Extension;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OOOReader.Reader.ShadowClassArray;

namespace OOOReader.Reader {

	/// <summary>
	/// A "shadow class" which is a generic container of arbitrary data that represents a Java class in the Clyde library. It cannot be extended.
	/// It can represent all four major formfactors of java types: <see langword="class"/>, <see langword="interface"/>, <see langword="enum"/>, and <see langword="@interface"/> (annotations).<para/>
	/// <para/>
	/// It also doubles as a representation of ThreeRings's <c>ClassWrapper</c> class (which provided access to the class statically, kind of like a super simple classloader impl). The only difference is that <see cref="ShadowClass"/> may function as an instance of those classes as well (see <see cref="IsTemplate"/>).
	/// </summary>
	public sealed class ShadowClass : AbstractShadowClassBase {

		internal static readonly Dictionary<string, ShadowClass> TEMPLATES = new Dictionary<string, ShadowClass>();

		/// <inheritdoc cref="AbstractShadowClassBase.Signature"/>
		/// <summary>
		/// This is identical to <see cref="AbstractShadowClassBase.Signature"/>
		/// </summary>
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
		/// Whether or not this is a primitive type.
		/// </summary>
		public bool IsPrimitive { get; }

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

		#region Construction

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
		/// Using OOO's default format of a fully-qualified class signature and a set of flags, this will return a <strong>Template</strong> <see cref="ShadowClass"/>,
		/// or a <see cref="ShadowClassArray"/>.
		/// </summary>
		/// <param name="className"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public static AbstractShadowClassBase CreateFromOOOFormat(string className, byte flags) {
			// TODO: Make use of flags? OOO used it to change how final classes are read.
			// Final classes in C# are sealed classes.
			// The thing is, I pre-cached all of those values so the flags are generally useless as far as I'm aware.

			(object? def, string sanitizedClassName) = GetDefaultValueFromSignature(className);
			if (def is null) {
				return GetOrCreateTemplate(sanitizedClassName, null, new Dictionary<string, object?>(), new Dictionary<string, string>());
			}
			if (def is PendingShadowClassArray pending) {
				GetOrCreateTemplate(sanitizedClassName, null, new Dictionary<string, object?>(), new Dictionary<string, string>());
				def = pending.Create(); // And since ^ will have made the template, this method call to Create() will always work.
			}
			if (!(def is ShadowClassArray)) {
				throw new ArgumentException("Input class name " + className + " resulted in the creation of an unexpected class " + def.GetType().ToString());
			}
			return (def as AbstractShadowClassBase)!;
		}

		#endregion

		#region Static Init

		private static object? TryGetReplacementForSig(string signature) {
			while (signature.StartsWith('[')) {
				signature = signature[1..];
			}
			if (signature.StartsWith('L') && signature.EndsWith(';')) {
				signature = signature[1..^1];
			}
			if (signature == "java.util.Set" || signature == "java.util.List"
				|| signature == "java.util.ArrayList" || signature == "java.util.HashSet"
				|| signature == "java.util.Collection") 
			{
				return new List<object>();
			}

			if (signature == "java.util.Map" || signature == "java.util.HashMap" || signature == "com.samskivert.util.LRUHashMap") {
				return new Dictionary<object, object?>();
			}

			if (signature == "com.samskivert.util.HashIntMap") {
				return new Dictionary<int, object?>();
			}

			if (signature == "com.google.common.collect.Multimap" || signature == "com.google.common.collect.SetMultimap" || signature == "com.google.common.collect.ListMultimap") {
				return new Dictionary<object, List<object>>();
			}

			return default;
		}

		private static (object?, string) GetDefaultValueFromSignature(string signature) {
			object? value = TryGetReplacementForSig(signature);
			int arrayDepth = 0;
			while (signature.StartsWith('[')) {
				arrayDepth++;
				signature = signature[1..];
			}
			/*
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
			*/
			if (ClydeFile.BOOTSTRAP_CLASS_JVM_NAMES.Contains(signature)) {
				signature = ClydeFile.BOOTSTRAP_CLASS_JAVA_NAMES[ClydeFile.BOOTSTRAP_CLASS_JVM_NAMES.IndexOf(signature)];
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
			// It's intentional that value is very often left to default.
			// This is due to static init - sometimes, a class won't be ready.
			// So what needs to happen is after everything is 
			return (value, signature);
		}

		static ShadowClass() {
			foreach (string bootstrapClass in ClydeFile.BOOTSTRAP_CLASS_JAVA_NAMES) {
				// As done in Clyde, these must be precreated
				TEMPLATES[bootstrapClass] = new ShadowClass(bootstrapClass, null, ShadowType.Class, false, true);
			}
			TEMPLATES["java.lang.Object"] = new ShadowClass("java.lang.Object", null, ShadowType.Class, false);
			TEMPLATES["java.lang.String"] = new ShadowClass("java.lang.String", null, ShadowType.Class, true);

			string[] clsDump = File.ReadAllLines("./data/OOOClassDump.txt");
			string? className = default;
			foreach (string info in clsDump) {
				string code = info.Substring(0, 2);
				string? baseClass = null;
				bool final = false;
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
						shadow.Fields[name] = value; // NOTE: If this is a ShadowClass itself, it'll be null instead right now.
						// We need to update this AFTER the read cycle is done!

						Type? shadowType = shadow.Fields[name]?.GetType();
						if (value is AbstractShadowClassBase absBase) {
							shadow.FieldOrFieldElementTypes[name] = absBase.Signature;
						} else if (value is null) {
							shadow.FieldOrFieldElementTypes[name] = cleanSignature; // Important to NOT create here.
						}
					}
					continue;
				}

				final = info[2] == 'f';

				className = info[3..];
				if (className.Contains(':')) {
					string[] splitName = className.Split(':');
					className = splitName[0];
					baseClass = splitName[1];
				}
				ShadowClass instance = new ShadowClass(className, baseClass, type, final);
				TEMPLATES[className] = instance;
			}

			foreach (KeyValuePair<string, ShadowClass> data in TEMPLATES) {
				foreach (KeyValuePair<string, object?> fieldInfo in data.Value.Fields.Copy()) {
					if (fieldInfo.Value is PendingShadowClassArray pendingArray) {
						data.Value.Fields[fieldInfo.Key] = pendingArray.Create();
					} else if (fieldInfo.Value is ShadowClass shdCls) {
						data.Value.Fields[fieldInfo.Key] = shdCls.CloneTemplate(); // Turn it into an instance.
					}
				}
			}
		}

		#endregion

		#region Private Constructors

		private ShadowClass(string name, string? baseClassName, ShadowType type, bool isFinal = false, bool isPrimitive = false) {
			Name = name;
			BaseClassName = baseClassName;
			Type = type;
			IsTemplate = true;
			IsSealed = isFinal;
			IsPrimitive = isPrimitive;
		}

		private ShadowClass(ShadowClass other) : this(other.Name, other.BaseClassName, other.Type) {
			IsTemplate = false;
			IsSealed = other.IsSealed;

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

		#endregion

		/// <summary>
		/// Clones this <see cref="ShadowClass"/> into a new instance. Mostly useful for templates (where <see cref="IsTemplate"/> is <see langword="true"/>).
		/// </summary>
		/// <returns>The new instance using the same values as this.</returns>
		public ShadowClass Clone() {
			return new ShadowClass(this);
		}

		/// <summary>
		/// Identical to <see cref="Clone"/> but this always ensures it is a direct clone of the template. If this instance is a clone, then it will not be a clone of this instance, but rather their common template.
		/// </summary>
		/// <returns></returns>
		public ShadowClass CloneTemplate() {
			if (IsTemplate) return Clone();
			return new ShadowClass(TemplateType!);
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
		public object? GetField(string name) {
			if (IsTemplate) throw new InvalidOperationException("Cannot call " + nameof(GetField) + " on a template object.");
			if (TryGetField(name, out object? value)) {
				return value;
			}
			throw new MissingFieldException(Name, name);
		}

		private bool TryGetField(string name, out object? value) {
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Invalid field name.");
			if (Fields.TryGetValue(name, out value)) {
				return true;
			}
			if (BaseClass != null) {
				if (BaseClass.TryGetField(name, out value)) {
					return true;
				}
			}

			value = default;
			return false;
		}

		/// <inheritdoc cref="GetField(string)"/>
		/// <remarks>
		/// May be <see langword="null"/> if <typeparamref name="T"/> is a reference type.
		/// </remarks>
		/// <typeparam name="T">The type of object to return as.</typeparam>
		public T? GetField<T>(string name) => (T?)GetField(name);

		/// <inheritdoc cref="TryGetField(string, out object?)"/>
		/// <remarks>
		/// May be <see langword="null"/> if <typeparamref name="T"/> is a reference type.
		/// </remarks>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool TryGetField<T>(string name, out T? value) => TryGetField(name, out value);

		/// <summary>
		/// Sets (or adds) a field with the given name.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		/// <param name="addToThisObject">If true, the value will be added to this object if it's not already defined here (only useful if <see cref="BaseClass"/> is set and <em>does</em> have a field with this name, and you want to write to this instead of the base.</param>
		/// <returns></returns>
		public void SetField(string name, object? value, bool addToThisObject = false) {
			if (IsTemplate) throw new InvalidOperationException("Cannot call " + nameof(SetField) + " on a template object.");
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Invalid field name.");
			if (addToThisObject || BaseClass == null || Fields.ContainsKey(name)) {
				if (FieldOrFieldElementTypes.TryGetValue(name, out string? fieldSig)) {
					ShadowClass templateShadow = TEMPLATES[fieldSig];
					if (value is ShadowClass shadowClass && shadowClass.TemplateType != templateShadow) {
						throw new ArgumentException("The field '" + name + "' is supposed to be an instance of " + nameof(ShadowClass) + " pointing to " + TEMPLATES[fieldSig].Name);
					} else if (value is ShadowClassArray shadowClassArray && shadowClassArray.ElementType != templateShadow) {
						throw new ArgumentException("The field '" + name + "' is supposed to be an instance of " + nameof(ShadowClassArray) + " with an element type of " + TEMPLATES[fieldSig].Name);
					} else if (value is PendingShadowClassArray) {
						throw new InvalidOperationException("The type of this value is a " + nameof(PendingShadowClassArray) + " which should not be possible.");
					}
				}
				Fields[name] = value;
			} else {
				// Base class exists and said field is not part of this. Set base?
				if (BaseClass.TryGetField(name, out object? _)) {
					BaseClass.SetField(name, value); // Yes, base has a field with this name.
				} else {
					Fields[name] = value; // No, just add it here instead.
				}
			}
		}

		/// <summary>
		/// Iterates through all keys/values of <paramref name="fields"/> and calls <see cref="SetField(string, object, bool)"/> for each.
		/// </summary>
		/// <param name="fields"></param>
		/// <param name="addToThisObject"></param>
		public void SetFields(Dictionary<string, object?> fields, bool addToThisObject = false) {
			foreach (KeyValuePair<string, object?> fieldInfo in fields) {
				SetField(fieldInfo.Key, fieldInfo.Value, addToThisObject);
			}
		}

		public override string ToString() {
			return $"ShadowClass[Type={Name}, IsTemplate={IsTemplate}]";
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
