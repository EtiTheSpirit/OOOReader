using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OOODumper.LazySRGReimpl {
	public sealed class ExportableShadowClass {

		public static readonly Dictionary<string, ExportableShadowClass> CACHE = new Dictionary<string, ExportableShadowClass>();

		public string Signature { get; set; }

		private string Code { get; set; }

		public List<string> InterfaceSignatures { get; set; }

		public string BaseClassSignature { get; set; }

		public List<ShadowField> Fields { get; set; }

		/// <summary>
		/// Returns the base class signature, or null if there is no base class (other than object)
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string GetBaseClassSignature(Type type) {
			if (type.BaseType != null && type.BaseType != typeof(object) && type.BaseType != typeof(java.lang.Object) && type.BaseType != typeof(ikvm.@internal.AnnotationAttributeBase) && type.BaseType != typeof(java.lang.Enum) && type.BaseType != typeof(Enum)) {
				string otherFullName = type.BaseType.FullName.Replace("+", "$");
				if (otherFullName == "System.Object") otherFullName = "java.lang.Object";
				if (otherFullName.StartsWith("__") || otherFullName.Length == 0) return null;
				return otherFullName;
			}
			return null;
		}

		/// <summary>
		/// Gets an existing instance of or creates a new instance of a shadow class wrapper. Returns null if this class should not be exported.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static ExportableShadowClass Populate(Type type) {
			string fullName = type.FullName.Replace("+", "$"); // Do NOT replace . with /
			if (fullName.StartsWith("__") || fullName.Length == 0) return null;
			if (fullName.EndsWith("__<CallerID>")) return null;
			if (type.Name.EndsWith("__Enum")) return null;

			ExportableShadowClass instance;
			if (CACHE.ContainsKey(fullName)) {
				instance = CACHE[fullName]; // Yes, use string. Type will be a different reference even if the class is the same
				// this is because both OOOLibAndDeps AND projectx-pcode share the same classes.
			} else {
				Debug.WriteLine(fullName);
				instance = new ExportableShadowClass {
					Signature = fullName,
					InterfaceSignatures = new List<string>(),
					BaseClassSignature = GetBaseClassSignature(type),
					Fields = new List<ShadowField>()
				};
			}

			Type[] interfaces = type.GetInterfaces();
			foreach (Type t in interfaces) {
				if (t.FullName.StartsWith("ikvm")) continue;
				if (t.FullName.StartsWith("System")) continue;
				string name = t.FullName.Replace('+', '$');
				if (name.EndsWith("$__Interface")) {
					name = name.Replace("$__Interface", "");
				}
				if (!instance.InterfaceSignatures.Contains(name)) {
					instance.InterfaceSignatures.Add(name);
				}
			}

			FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			foreach (FieldInfo field in fields) {
				if (field.IsStatic) continue;
				if (field.Name == "value__") continue;
				ShadowField info = GetFieldInfo(field);
				if (info != null) {
					if (!instance.Fields.Contains(info)) {
						instance.Fields.Add(info);
					}
				}
			}

			if (type.IsInterface) {
				instance.Code = "IF";
			} else if (type.IsAssignableTo(typeof(Attribute))) {
				instance.Code = "AN";
			} else if (type.BaseType == typeof(java.lang.Enum)) {
				instance.Code = "EN";
			} else if (type.IsClass) {
				instance.Code = "CL";
			} else {
				Console.WriteLine("Unknown type signature for " + type);
			}

			if (type.IsSealed) {
				instance.Code += 'f';
			} else {
				instance.Code += '-';
			}

			CACHE[fullName] = instance;

			return instance;
		}

		private static ShadowField GetFieldInfo(MemberInfo field) {
			string name = field.Name;
			string signature = "";

			Type t;
			if (field is FieldInfo fi) {
				t = fi.FieldType;
			} else if (field is PropertyInfo pi) {
				t = pi.PropertyType;
			} else {
				return null;
			}
			while (t.IsArray) {
				t = t.GetElementType()!;
				signature += '[';
			}
			if (t == typeof(bool)) {
				signature += 'Z';
			} else if (t == typeof(byte)) {
				signature += 'B';
			} else if (t == typeof(char)) {
				signature += 'C';
			} else if (t == typeof(short)) {
				signature += 'S';
			} else if (t == typeof(int)) {
				signature += 'I';
			} else if (t == typeof(long)) {
				signature += 'J';
			} else if (t == typeof(float)) {
				signature += 'F';
			} else if (t == typeof(double)) {
				signature += 'D';
			} else if (t == typeof(string)) {
				signature += "Ljava.lang.String;";
			} else {
				signature += 'L';
				string fullName = t.FullName;
				if (fullName == "System.Object") fullName = "java.lang.Object";
				signature += fullName.Replace("+", "$");
				signature += ';';
			}
			return new ShadowField {
				Name = name,
				Signature = signature
			};
		}

		private string BuildClassSignature() {
			string baseName = Code + Signature;
			if (BaseClassSignature != null) {
				baseName += $":{BaseClassSignature}";
			}
			foreach (string ifSig in InterfaceSignatures) {
				baseName += $"+{ifSig}";
			}
			return baseName;
		}

		public void AppendTo(StringBuilder builder) {
			builder.AppendLine(BuildClassSignature());
			foreach (ShadowField shadow in Fields) {
				builder.Append('\t');
				shadow.AppendTo(builder);
			}
		}

		public static void AppendAllTo(StringBuilder builder) {
			foreach (ExportableShadowClass shadow in CACHE.Values) {
				shadow.AppendTo(builder);
			}
		}

		public class ShadowField {

			public string Name { get; set; }

			public string Signature { get; set; }

			public void AppendTo(StringBuilder builder) {
				builder.AppendLine($"{Name} {Signature}");
			}

			public override bool Equals(object obj) {
				if (obj is ShadowField other) {
					return Name == other.Name && Signature == other.Signature;
				}
				return false;
			}

			public override int GetHashCode() {
				return Name.GetHashCode() ^ Signature.GetHashCode();
			}
		}

	}
}
