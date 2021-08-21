using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OOODumper.LazySRGReimpl {
	public class ClassDumper : IDisposable {

		private StringWriter Writer { get; }

		public ClassDumper(StringWriter writer) {
			Writer = writer;
		}

		public void WriteTypeAndMembers(Type type) {
			string fullName = type.FullName.Replace("+", "$"); // Do NOT replace . with /
			if (fullName.StartsWith("__") || fullName.Length == 0) return;
			if (fullName.EndsWith("__<CallerID>")) return;
			if (type.Name.EndsWith("__Enum")) return;
			if (type.IsInterface) {
				Writer.Write("IF");
			} else if (type.IsAssignableTo(typeof(Attribute))) {
				Writer.Write("AN");
			} else if (type.BaseType == typeof(java.lang.Enum)) {
				Writer.Write("EN");
			} else if (type.IsClass) {
				Writer.Write("CL");
			} else {
				Console.WriteLine("Unknown type signature for " + type);
			}
			if (fullName == "System.Object") fullName = "java.lang.Object";
			if (type.IsSealed) {
				Writer.Write("f");
			} else {
				Writer.Write("-");
			}
			Writer.Write(fullName);

			if (type.BaseType != null && type.BaseType != typeof(object) && type.BaseType != typeof(java.lang.Object) && type.BaseType != typeof(ikvm.@internal.AnnotationAttributeBase) && type.BaseType != typeof(java.lang.Enum) && type.BaseType != typeof(Enum)) {
				string otherFullName = type.BaseType.FullName.Replace("+", "$");
				if (otherFullName == "System.Object") otherFullName = "java.lang.Object";
				if (otherFullName.StartsWith("__") || otherFullName.Length == 0) return;
				Writer.Write(":" + otherFullName);
			}

			Type[] interfaces = type.GetInterfaces();
			foreach (Type t in interfaces) {
				if (t.FullName.StartsWith("ikvm")) continue;
				if (t.FullName.StartsWith("System")) continue;
				string name = t.FullName.Replace('+', '$');
				if (name.EndsWith("$__Interface")) {
					name = name.Replace("$__Interface", "");
				}
				Writer.Write("+" + name);
			}

			Writer.WriteLine();

			FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			foreach (FieldInfo field in fields) {
				if (field.IsStatic) continue;
				if (field.Name == "value__") continue;
				WriteField(field);
			}
			/*
			PropertyInfo[] props = type.GetProperties();
			foreach (PropertyInfo field in props) {
				WriteField(field);
			}*/
		}

		public void WriteField(MemberInfo field) {
			Writer.Write('\t');
			Writer.Write(field.Name);
			Writer.Write(' ');

			Type t;
			if (field is FieldInfo fi) {
				t = fi.FieldType;
			} else if (field is PropertyInfo pi) {
				t = pi.PropertyType;
			} else {
				return;
			}
			while (t.IsArray) {
				t = t.GetElementType()!;
				Writer.Write('[');
			}
			if (t == typeof(bool)) {
				Writer.Write('Z');
			} else if (t == typeof(byte)) {
				Writer.Write('B');
			} else if (t == typeof(char)) {
				Writer.Write('C');
			} else if (t == typeof(short)) {
				Writer.Write('S');
			} else if (t == typeof(int)) {
				Writer.Write('I');
			} else if (t == typeof(long)) {
				Writer.Write('J');
			} else if (t == typeof(float)) {
				Writer.Write('F');
			} else if (t == typeof(double)) {
				Writer.Write('D');
			} else if (t == typeof(string)) {
				Writer.Write("Ljava.lang.String;");
			} else {
				Writer.Write('L');
				string name = t.FullName;
				if (name == "System.Object") name = "java.lang.Object";
				Writer.Write(name.Replace("+", "$"));
				Writer.Write(';');
			}
			Writer.WriteLine();
		}

		public void Close() {
			Writer.Close();
		}

		public void Flush() {
			Writer.Flush();
		}

		public void Dispose() {
			GC.SuppressFinalize(this);
			Writer.Dispose();
		}
	}
}
