#nullable enable
using OOOReader.Clyde;
using OOOReader.Utility.Extension;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
	/// It also doubles as a representation of ThreeRings's <c>ClassWrapper</c> class (which provided access to the class statically, kind of like a super simple classloader impl).<para/>
	/// <para/>
	/// Finally, <see cref="ShadowClass"/> may additionally function as an <em>instance</em> of a class as well (see <see cref="IsTemplate"/>).
	/// </summary>
	public sealed class ShadowClass : AbstractShadowClassBase {

		internal static readonly Dictionary<string, ShadowClass> TEMPLATES = new Dictionary<string, ShadowClass>();

		/// <summary>
		/// This <see cref="IEqualityComparer{T}"/> will compare the template types of two <see cref="ShadowClass"/> instances rather than the actual instances themselves. This is used for type comparison rather than instance comparison.
		/// </summary>
		public static readonly IEqualityComparer<ShadowClass?> SHADOW_TEMPLATE_COMPARATOR = new ShadowTemplateComparator();

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
		/// The template type of this <see cref="ShadowClass"/>, or <see langword="this"/> if <see cref="IsTemplate"/> is <see langword="true"/>.
		/// </summary>
		public ShadowClass TemplateType { get; }

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
		/// Whether or not this is a type of <c>java.util.Map</c>
		/// </summary>
		public bool IsMap => _InterfaceNames.Contains("java.util.Map");

		/// <summary>
		/// Whether or not this is a type of <c>java.util.Set</c>
		/// </summary>
		public bool IsSet => _InterfaceNames.Contains("java.util.Set");

		/// <summary>
		/// Whether or not the outer class of this (if applicable) should be read from a <see cref="ClydeFile"/>'s internal strea.
		/// </summary>
		public bool ShouldReadOuter { get; private set; }

		/// <summary>
		/// An instance of this class's outer class, if applicable.
		/// </summary>
		/// <remarks>
		/// For a reference to the outer class type, use <see cref="OuterClassType"/>.
		/// </remarks>
		public ShadowClass? OuterClass {
			get {
				if (GotOuterClassInstance) return _OuterClass;
				if (_OuterClass == null) {
					GotOuterClassInstance = true;
					ShadowClass? baseType = OuterClassType;
					if (baseType != null) {
						_OuterClass = baseType.NewInstance();
					}
				}
				return _OuterClass;
			}
		}
		private ShadowClass? _OuterClass = null;
		private bool GotOuterClassInstance = false;

		/// <summary>
		/// If this is an inner class, <see cref="OuterClassType"/> is a reference the outer class's template <see cref="ShadowClass"/> (that is, a <see cref="ShadowClass"/> where <see cref="IsTemplate"/> is <see langword="true"/> and its <see cref="Name"/> is the name of the outer class).
		/// </summary>
		/// <remarks>
		/// For an instance of this <see cref="ShadowClass"/>'s outer class, use <see cref="OuterClass"/>.
		/// </remarks>
		public ShadowClass? OuterClassType {
			get {
				if (!CheckedForOuterClassType) {
					CheckedForOuterClassType = true;
					if (Name.Contains('$')) {
						string outer = Name[0..(Name.LastIndexOf('$') + 1)];
						if (outer.EndsWith("$")) {
							outer = outer[0..^1];
						}
						_OuterClassType = TEMPLATES.GetValueOrDefault(outer);
					}
				}
				return _OuterClassType;
			}
		}
		private ShadowClass? _OuterClassType = null;
		private bool CheckedForOuterClassType = false;

		/// <summary>
		/// If this is an extension of another class, this is the base class.
		/// </summary>
		public ShadowClass? BaseClass {
			get {
				if (BaseClassName != null && _BaseClass == null) {
					_BaseClass = GetOrCreateTemplate(BaseClassName);
				}
				return _BaseClass;
			}
		}
		private ShadowClass? _BaseClass = null;
		private readonly string? BaseClassName = null;

		/// <summary>
		/// A reference to every interface this class inherits from, pointing to a list of template <see cref="ShadowClass"/>es
		/// </summary>
		public ShadowClass[] Interfaces {
			get {
				if (_Interfaces == null) {
					int length = _InterfaceNames.Length;
					_Interfaces = new ShadowClass[length];
					for (int idx = 0; idx < length; idx++) {
						_Interfaces[idx] = GetOrCreateTemplate(_InterfaceNames[idx]);
					}
				}
				return _Interfaces;
			}
		}
		private ShadowClass[]? _Interfaces = null;
		private string[] _InterfaceNames;

		/// <summary>
		/// An alias that determines if this class implements <c>com.threerings.export.Exportable</c>.
		/// </summary>
		public bool IsOOOExportable {
			get {
				if (!FoundOOOExportable) {
					FoundOOOExportable = true;
					_IsOOOExportable = _InterfaceNames.Contains("com.threerings.export.Exportable");
				}
				return _IsOOOExportable;
			}
		}
		private bool _IsOOOExportable;
		private bool FoundOOOExportable = false;

		/// <summary>
		/// An alias that determines if this class implements <c>com.threerings.export.Encodeable</c>
		/// </summary>
		public bool IsOOOEncodable {
			get {
				if (!FoundOOOEncodable) {
					FoundOOOEncodable = true;
					_IsOOOEncodable = _InterfaceNames.Contains("com.threerings.export.Encodable");
				}
				return _IsOOOEncodable;
			}
		}
		private bool _IsOOOEncodable;
		private bool FoundOOOEncodable = false;


		/// <summary>
		/// The fields in this ShadowClass.
		/// </summary>
		private readonly Dictionary<string, object?> Fields = new Dictionary<string, object?>();

		/// <summary>
		/// The types of values in the fields of this ShadowClass. The type values will be usable to look up something in <see cref="TEMPLATES"/>.
		/// </summary>
		private readonly Dictionary<string, string> FieldOrFieldElementTypes = new Dictionary<string, string>();

		/// <summary>
		/// For ShadowClasses that are enums, this is their value cache since Java enums are instance-based.
		/// </summary>
		private readonly Dictionary<string, ShadowClassEnumInstance> EnumItemCache = new Dictionary<string, ShadowClassEnumInstance>();

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
		public static ShadowClass CreateInstanceOf(string javaFullyQualifiedName, string? baseTypeName = null) {
			if (javaFullyQualifiedName.StartsWith('L') && javaFullyQualifiedName.EndsWith(';')) {
				javaFullyQualifiedName = javaFullyQualifiedName[1..^1];
			}
			if (TEMPLATES.TryGetValue(javaFullyQualifiedName, out ShadowClass? shadow)) {
				return shadow!.Clone();
			}
			return GetOrCreateTemplate(javaFullyQualifiedName, baseTypeName).Clone();
		}

		/// <summary>
		/// Returns the template representation of a class (from OOOClassDump.txt). If the class name is not recognized, this returns a new instance
		/// of a <see cref="ShadowClass"/> which is a template, hence the requirement of a field and field type dictionary. This will register the
		/// template as well.
		/// </summary>
		/// <remarks>
		/// <strong>This always returns the template object. Use <see cref="Clone"/> to get a workable instance, or more preferrably, use <see cref="CreateInstanceOf(string, string?)"/></strong><para/>
		/// A java fully qualified name looks like this: <c>java/lang/Object$SomeInnerClass</c> - If it starts with L and ends with ;, those chars will be stripped.
		/// </remarks>
		/// <param name="javaFullyQualifiedName"></param>
		/// <param name="fieldValues">The default values for fields. If the values are objects, consider defining their value type in <paramref name="shadowedFieldTypeValues"/>.</param>
		/// <param name="shadowedFieldTypeValues">For fields whose values are <see cref="ShadowClass"/> instances (or <see cref="ShadowClassArray"/> instances), this is the fully-qualified type name that can be used in <see cref="GetOrCreate(string)"/>.</param>
		/// <returns></returns>
		public static ShadowClass GetOrCreateTemplate(string javaFullyQualifiedName, string? baseClassName = null, Dictionary<string, object?>? fieldValues = null, Dictionary<string, string>? shadowedFieldTypeValues = null) {
			fieldValues ??= new Dictionary<string, object?>();
			shadowedFieldTypeValues ??= new Dictionary<string, string>();

			if (javaFullyQualifiedName.StartsWith('L') && javaFullyQualifiedName.EndsWith(';')) {
				javaFullyQualifiedName = javaFullyQualifiedName[1..^1];
			}
			if (TEMPLATES.TryGetValue(javaFullyQualifiedName, out ShadowClass? shadow)) {
				return shadow;
			}
			Debug.WriteLine("Warning: GetOrCreateTemplate failed to find a pre-constructed class: " + javaFullyQualifiedName);
			ShadowClass ret = new ShadowClass(javaFullyQualifiedName, baseClassName, null, ShadowType.Class);
			fieldValues.CopyTo(ret.Fields);
			shadowedFieldTypeValues.CopyTo(ret.FieldOrFieldElementTypes);
			TEMPLATES[javaFullyQualifiedName] = ret;
			return ret;
		}

		/// <summary>
		/// Using OOO's default format of a fully-qualified class signature and a set of flags, this will return a <strong>Template</strong> <see cref="ShadowClass"/>,
		/// or a <see cref="ShadowClassArray"/>. However, if the class in question is a map/set type (or something similar), a corresponding <see cref="List{T}"/> or <see cref="Dictionary{TKey, TValue}"/> will be returned.
		/// </summary>
		/// <param name="className"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public static object CreateFromOOOFormat(string className, byte flags) {
			// TODO: Make use of flags? OOO used it to change how final classes are read.
			// Final classes in C# are sealed classes.
			// The thing is, I pre-cached all of those values so the flags are generally useless as far as I'm aware.

			(object? def, string sanitizedClassName) = GetDefaultValueFromSignature(className);
			if (def is null) {
				ShadowClass template = GetOrCreateTemplate(sanitizedClassName, null, new Dictionary<string, object?>(), new Dictionary<string, string>());
				template.ShouldReadOuter = HasFlag(flags, 2);
				return template;
			}
			if (def is PendingShadowClassArray pending) {
				GetOrCreateTemplate(sanitizedClassName, null, new Dictionary<string, object?>(), new Dictionary<string, string>());
				def = pending.Create(); // And since ^ will have made the template, this method call to Create() will always work.
			}
			/*
			if (!(def is ShadowClassArray)) {
				throw new ArgumentException("Input class name " + className + " resulted in the creation of an unexpected class " + def.GetType().ToString());
			}*/
			//return (def as AbstractShadowClassBase)!;
			return def;
		}

		private static bool HasFlag(byte flagValue, byte toCheck) {
			return (flagValue & toCheck) == toCheck;
		}

		#endregion

		#region Static Init

		/// <summary>
		/// Given a class signature, this attempts to return an object that <em>mostly</em> represents the input class. This is mostly aimed at
		/// sets/collections/lists.
		/// </summary>
		/// <param name="signature"></param>
		/// <returns></returns>
		public static object? TryGetReplacementForSig(string signature) {
			while (signature.StartsWith('[')) {
				signature = signature[1..];
			}
			if (signature.StartsWith('L') && signature.EndsWith(';')) {
				signature = signature[1..^1];
			}
			// Can use flags for this.
			if (signature == "java.util.Set" || signature == "java.util.List"
				|| signature == "java.util.ArrayList" || signature == "java.util.HashSet"
				|| signature == "java.util.Collection") 
				// Multiset WOULD BE acceptable here because C# lists already support duplicates. 
				// But the thing is, its not read like that in Clyde, so...
			{
				return new List<object>();
			}

			if (signature == "com.google.common.collect.Multiset") {
				return new Dictionary<object, int>();
			}

			if (signature == "java.util.Map" || signature == "java.util.HashMap" || signature == "com.samskivert.util.LRUHashMap") {
				return new Dictionary<object, object?>();
			}

			if (signature == "com.samskivert.util.HashIntMap") {
				//return new Dictionary<int, object?>();
				return new Dictionary<object, object?>(); // Just use object for key, it's easier
			}

			if (signature == "com.google.common.collect.Multimap" || signature == "com.google.common.collect.SetMultimap" || signature == "com.google.common.collect.ListMultimap") {
				return new Dictionary<object, List<object>>();
			}

			// ArgumentMap has some special handling
			/*
			if (signature == "com.threerings.config.ArgumentMap") {
				return new Dictionary<object, object?>();
			}
			*/

			return default;
		}

		private static (object?, string) GetDefaultValueFromSignature(string signature) {
			object? value = TryGetReplacementForSig(signature);
			int arrayDepth = 0;
			while (signature.StartsWith('[')) {
				arrayDepth++;
				signature = signature[1..];
			}
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
				TEMPLATES[bootstrapClass] = new ShadowClass(bootstrapClass, null, null, ShadowType.Class, false, true);
			}
			TEMPLATES["java.lang.Object"] = new ShadowClass("java.lang.Object", null, null, ShadowType.Class, false);
			TEMPLATES["java.lang.String"] = new ShadowClass("java.lang.String", null, null, ShadowType.Class, true);

			string[] clsDump = File.ReadAllLines("./data/OOOClassDump.txt");
			string? className = default;
			foreach (string info in clsDump) {
				string code = info.Substring(0, 2);
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
				string? baseClass = null;
				string[]? interfaces = null;
				if (className.Contains(':')) {
					string[] splitName = className.Split(':');
					className = splitName[0];
					baseClass = splitName[1];
					if (baseClass.Contains('+')) {
						// Has interfaces
						string[] ifDefs = baseClass.Split('+');
						baseClass = ifDefs[0];
						interfaces = ifDefs.Skip(1).ToArray();
					}
				} 
				if (className.Contains('+')) {
					// ^ Will only occur if the class name didn't have a :
					string[] splitName = className.Split('+');
					className = splitName[0];
					interfaces = splitName.Skip(1).ToArray();
				}
				ShadowClass instance = new ShadowClass(className, baseClass, interfaces, type, final);
				TEMPLATES[className] = instance;
			}

			foreach (KeyValuePair<string, ShadowClass> data in TEMPLATES) {
				foreach (KeyValuePair<string, object?> fieldInfo in data.Value.Fields.Copy()) {
					if (fieldInfo.Value is PendingShadowClassArray pendingArray) {
						data.Value.Fields[fieldInfo.Key] = pendingArray.Create();
					} else if (fieldInfo.Value is ShadowClass shdCls) {
						data.Value.Fields[fieldInfo.Key] = shdCls.NewInstance(); // Turn it into an instance.
					}
				}
			}
		}

		#endregion

		#region Private Constructors

		private ShadowClass(string name, string? baseClassName, string[]? interfaces, ShadowType type, bool isFinal = false, bool isPrimitive = false) {
			Name = name;
			BaseClassName = baseClassName;
			Type = type;
			IsTemplate = true;
			IsSealed = isFinal;
			IsPrimitive = isPrimitive;
			TemplateType = this;
			_InterfaceNames = interfaces ?? Array.Empty<string>();
		}

		private ShadowClass(ShadowClass other) : this(other.Name, other.BaseClassName, null, other.Type) {
			IsTemplate = false;
			IsSealed = other.IsSealed;

			other.Fields.CopyTo(Fields);
			other.FieldOrFieldElementTypes.CopyTo(FieldOrFieldElementTypes);

			_OuterClassType = other.OuterClassType; // use the actual property so it's evaluated
			CheckedForOuterClassType = other.CheckedForOuterClassType;

			_BaseClass = other._BaseClass;
			// Name is copied already

			_Interfaces = other.Interfaces;
			_InterfaceNames = other._InterfaceNames;
			_IsOOOExportable = other._IsOOOExportable;
			FoundOOOExportable = other.FoundOOOExportable;

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
		/// <remarks>
		/// This will error on <see cref="ShadowClass"/> instance whose type is <see cref="ShadowType.Enum"/>.
		/// </remarks>
		/// <returns>The new instance using the same values as this.</returns>
		/// <exception cref="InvalidOperationException">If this is called on an enum.</exception>
		public ShadowClass Clone() {
			if (Type == ShadowType.Enum) throw new InvalidOperationException("Cannot clone an enum.");
			return new ShadowClass(this);
		}

		/// <summary>
		/// Identical to <see cref="Clone"/> but this always ensures it is a direct clone of the template. If this instance is a clone, then it will not be a clone of this instance, but rather their common template.
		/// </summary>
		/// <remarks>
		/// This will error on <see cref="ShadowClass"/> instance whose type is <see cref="ShadowType.Enum"/>.
		/// </remarks>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException">If this is called on an enum.</exception>
		public ShadowClass NewInstance() {
			if (Type == ShadowType.Enum) throw new InvalidOperationException("Cannot clone the template of an enum.");
			if (IsTemplate) return Clone();
			return new ShadowClass(TemplateType!);
		}

		/// <summary>
		/// Returns a copy of the array representing every field name in this <see cref="ShadowClass"/> (for class/annotation/interface types) or <see cref="ShadowClassEnumInstance"/> names for enum types.
		/// </summary>
		/// <returns></returns>
		public string[] GetMemberNames() {
			if (Type == ShadowType.Enum) {
				return EnumItemCache.Keys.ToArray();
			}
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
			if (Type == ShadowType.Annotation || Type == ShadowType.Enum) {
				throw new InvalidOperationException($"Cannot call {nameof(GetField)} on {nameof(ShadowClass)} instances whose type is {ShadowType.Annotation} or {ShadowType.Enum}");
			}
			if (IsTemplate) throw new InvalidOperationException("Cannot call " + nameof(GetField) + " on a template object.");
			if (TryGetField(name, out object? value)) {
				return value;
			}
			throw new MissingFieldException(Name, name);
		}

		public bool TryGetField<T>(string name, out T? value) {
			if (Type == ShadowType.Annotation || Type == ShadowType.Enum) {
				throw new InvalidOperationException($"Cannot call {nameof(TryGetField)} on {nameof(ShadowClass)} instances whose type is {ShadowType.Annotation} or {ShadowType.Enum}");
			}
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Invalid field name.");
			if (Fields.TryGetValue(name, out object? retn)) {
				value = (T?)retn;
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

		/// <summary>
		/// Returns whether or not this <see cref="ShadowClass"/>'s type inherits from or is directly equal to <paramref name="other"/>.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool IsA(ShadowClass other) {
			ShadowClass? comp = other.TemplateType;
			ShadowClass? self = TemplateType;
			do {
				if (self == comp) {
					return true;
				}
				self = self.BaseClass;
			} while (self != null);

			return false;
		}

		/// <summary>
		/// Sets (or adds) a field with the given name.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		/// <param name="addToThisObject">If true, the value will be added to this object if it's not already defined here (only useful if <see cref="BaseClass"/> is set and <em>does</em> have a field with this name, and you want to write to this instead of the base.</param>
		/// <returns></returns>
		public void SetField(string name, object? value, bool addToThisObject = false) {
			if (Type == ShadowType.Annotation || Type == ShadowType.Enum) {
				throw new InvalidOperationException($"Cannot call {nameof(SetField)} on {nameof(ShadowClass)} instances whose type is {ShadowType.Annotation} or {ShadowType.Enum}");
			}
			if (IsTemplate) throw new InvalidOperationException("Cannot call " + nameof(SetField) + " on a template object.");
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Invalid field name.");
			if (addToThisObject || BaseClass == null || Fields.ContainsKey(name)) {
				if (FieldOrFieldElementTypes.TryGetValue(name, out string? fieldSig)) {
					ShadowClass templateShadow = TEMPLATES[fieldSig];
					if (value is ShadowClass shadowClass && !shadowClass.IsA(templateShadow)) {
						throw new ArgumentException($"The field '{name}' is supposed to be an instance of {nameof(ShadowClass)} inheriting from or equal to {TEMPLATES[fieldSig].Name} (it is an instance of {shadowClass.Name})");
					} else if (value is ShadowClassArray shadowClassArray && !shadowClassArray.ElementType!.IsA(templateShadow)) {
						throw new ArgumentException($"The field '{name}' is supposed to be an instance of {nameof(ShadowClassArray)} with an element type of {TEMPLATES[fieldSig].Name}");
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
		/// <exception cref="InvalidOperationException">If this is an annotation or interface type.</exception>
		public void SetFields(Dictionary<string, object?> fields, bool addToThisObject = false) {
			if (Type == ShadowType.Annotation || Type == ShadowType.Enum) {
				throw new InvalidOperationException($"Cannot call {nameof(SetFields)} on {nameof(ShadowClass)} instances whose type is {ShadowType.Annotation} or {ShadowType.Enum}");
			}
			foreach (KeyValuePair<string, object?> fieldInfo in fields) {
				string fieldName = fieldInfo.Key;
				string altFieldName = '_' + fieldName;
				if (Fields.ContainsKey(altFieldName)) {
					fieldName = altFieldName;
				}
				SetField(fieldName, fieldInfo.Value, addToThisObject);
			}
		}

		/// <summary>
		/// Gets the <see cref="ShadowClassEnumInstance"/> with the given name, or creates it if it does not yet exist.
		/// </summary>
		/// <remarks>
		/// Only functions for <see cref="ShadowClass"/> instances whose <see cref="Type"/> is <see cref="ShadowType.Enum"/>.
		/// </remarks>
		/// <param name="name"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException">If this is not an enum type.</exception>
		public ShadowClassEnumInstance GetEnumItem(string name) {
			if (Type != ShadowType.Enum) {
				throw new InvalidOperationException($"Cannot call {nameof(GetEnumItem)} on {nameof(ShadowClass)} instances whose type is {Type}");
			}
			if (EnumItemCache.TryGetValue(name, out ShadowClassEnumInstance? ei)) {
				return ei!;
			}
			ShadowClassEnumInstance newInstance = new ShadowClassEnumInstance(TemplateType, name);
			EnumItemCache[name] = newInstance;
			return newInstance;
		}

		/// <summary>
		/// An alias to <see cref="GetField(string)"/> and <see cref="SetField(string, object?, bool)"/>. This returns <see langword="dynamic"/> for ease of access.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public dynamic? this[string key] {
			get {
				return GetField(key);
			}
			set {
				SetField(key, value);
			}
		}

		public override string ToString() {
			return $"ShadowClass[Type={Name}, IsTemplate={IsTemplate}]";
		}

		public string FullDump(int depth = 0) {
			StringBuilder sb = new StringBuilder(depth > 0 ? new string('\t', depth) : string.Empty);
			sb.Append(ToString());
			sb.AppendLine();
			foreach (KeyValuePair<string, object?> field in Fields) {
				sb.Append(new string('\t', depth + 1));
				sb.Append('[');
				sb.Append(field.Key);
				sb.Append("]=");
				if (field.Value is IEnumerable enumerable) {
					sb.Append(enumerable.ArrayToString(depth + 1));
				} else if (field.Value is ShadowClass shadow) {
					sb.Append(shadow.FullDump(depth + 1));
				} else {
					sb.Append(field.Value?.ToString() ?? "null");
				}
				sb.AppendLine();
			}
			return sb.ToString();
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

		private sealed class ShadowTemplateComparator : IEqualityComparer<ShadowClass?> {
			public bool Equals(ShadowClass? x, ShadowClass? y) {
				if (x is null && y is null) return true;
				if (x is null || y is null) return false;
				return x!.TemplateType == y!.TemplateType;
			}

			public int GetHashCode([DisallowNull] ShadowClass? obj) {
				return obj.GetHashCode();
			}
		}
	}
}
