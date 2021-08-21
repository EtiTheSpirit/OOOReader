#nullable enable
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using OOOReader.Reader;
using OOOReader.Utility.Attributes;
using OOOReader.Utility.Data;
using OOOReader.Utility.Extension;
using OOOReader.WithCustomReadBehavior;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace OOOReader.Clyde {
	public class ClydeFile : IDisposable {

		#region Preset Information

		private static readonly object NULL = new object();

		/// <summary>
		/// Must be in this exact order. These are the types for the "default" classes of Clyde's Binary Importer
		/// </summary>
		[Obsolete("The implementation of ShadowClass makes this useless, use BOOTSTRAP_CLASS_JAVA_NAMES instead.", true)]
		public static readonly Type[] BOOTSTRAP_CLASSES = {
			typeof(bool), typeof(byte), typeof(char), typeof(double),
			typeof(float), typeof(int), typeof(long), typeof(short)
		};

		/// <summary>
		/// Identical to <see cref="BOOTSTRAP_CLASSES"/>, but by Java fully-qualified name instead of C# type.
		/// </summary>
		public static readonly string[] BOOTSTRAP_CLASS_JAVA_NAMES = {
			"java.lang.Boolean", "java.lang.Byte", "java.lang.Character", "java.lang.Double",
			"java.lang.Float", "java.lang.Integer", "java.lang.Long", "java.lang.Short"
		};

		/// <summary>
		/// The internal JVM Bytecode names in a 1:1 mapping to <see cref="BOOTSTRAP_CLASS_JAVA_NAMES"/>
		/// </summary>
		// TODO: Not this? Maybe a dictionary
		public static readonly string[] BOOTSTRAP_CLASS_JVM_NAMES = {
			"Z", "B", "C", "D",
			"F", "I", "J", "S"
		};

		#endregion

		/// <summary>
		/// The header of a Clyde file. This is in big endian and shows in the order displayed in the constant value.
		/// </summary>
		[BigEndian]
		public const uint HEADER = 0xFACEAF0E;

		/// <summary>
		/// The underlying stream that this <see cref="ClydeFile"/> is reading from.
		/// </summary>
		public Stream BaseStream { get; }

		/// <summary>
		/// The version of this <see cref="ClydeFile"/>, which determines how IDs and segment lengths are written.
		/// </summary>
		public ClydeVersion Version { get; }

		/// <summary>
		/// Whether or not this <see cref="ClydeFile"/> has been compressed.
		/// </summary>
		public bool Compressed { get; }

		/// <summary>
		/// A <see cref="BinaryReader"/> wrapped around <see cref="BaseStream"/>
		/// </summary>
		private BinaryReader Reader { get; }

		/// <summary>
		/// The system responsible for reading object IDs and segment lengths.
		/// </summary>
		private IDReader IDReader { get; }

		/// <summary>
		/// Object IDs bound to a template for that type.
		/// </summary>
		private Dictionary<int, object> CachedObjects { get; } = new Dictionary<int, object>();

		/// <summary>
		/// Class IDs bound to their template class
		/// </summary>
		private Dictionary<int, object> CachedClasses { get; } = new Dictionary<int, object>();

		/// <summary>
		/// Field names bound to a template for their type.
		/// </summary>
		private Dictionary<ShadowClass, Dictionary<int, FieldData>> CachedFields { get; } = new Dictionary<ShadowClass, Dictionary<int, FieldData>>();

		/// <summary>
		/// A ephemeral dictionary of the current read fields operation. It is cleared as soon as the main reading cycle has completed for an object.
		/// </summary>
		private Dictionary<string, object?> CurrentReadFields { get; set; } = new Dictionary<string, object?>();

		/// <summary>
		/// A quick reference to the <see cref="ShadowClass"/> representing <c>java.lang.Object</c>
		/// </summary>
		public static ShadowClass ObjectClass { get; } = ShadowClass.GetOrCreateTemplate("java.lang.Object");

		/// <summary>
		/// A quick reference to the <see cref="ShadowClass"/> representing <c>java.lang.String</c>
		/// </summary>
		public static ShadowClass StringClass { get; } = ShadowClass.GetOrCreateTemplate("java.lang.String");

		private static Stream Decompress(Stream compressedIn) {
			MemoryStream mstr = new MemoryStream();
			InflaterInputStream decomp = new InflaterInputStream(compressedIn);
			decomp.CopyTo(mstr);
			mstr.Position = 0;
			return mstr;
		}

		/// <summary>
		/// Creates a new <see cref="ClydeFile"/> for the .dat file at the given path.
		/// </summary>
		/// <param name="filePath"></param>
		public ClydeFile(string filePath) : this(File.OpenRead(filePath)) { }

		/// <summary>
		/// Creates a new <see cref="ClydeFile"/> from the given <see cref="Stream"/>.
		/// </summary>
		/// <param name="input"></param>
		public ClydeFile(Stream input) {
			BaseStream = input;
			Reader = new BinaryReader(input);

			uint readHeader = Reader.ReadUInt32BE();
			if (readHeader != HEADER) throw new IOException($"Invalid header value! Expected 0x{HEADER:X8}, got 0x{readHeader:X8}");
			
			ClydeVersion version = (ClydeVersion)Reader.ReadUInt16BE();
			Compressed = Reader.ReadUInt16BE() == 0x1000;
			if (Compressed) {
				MemoryStream newStr = (MemoryStream)Decompress(input);
				BaseStream = newStr;
				Reader = new BinaryReader(newStr);
				input.Dispose();
				
				File.WriteAllBytes(".\\TEST_DECOMP.txt", newStr.ToArray());
			}

			IDReader = IDReader.For(Reader, version);

			CachedObjects[0] = NULL;
			int idx = 1;
			foreach (string className in BOOTSTRAP_CLASS_JAVA_NAMES) {
				CachedClasses[idx] = ShadowClass.GetOrCreateTemplate(className);
				idx++;
			}
		}

		/// <summary>
		/// Reads a field from the current read fields array <see cref="CurrentReadFields"/>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <param name="defValue"></param>
		/// <returns></returns>
		public T? Read<T>(string name, T? defValue = default) {
			if (CurrentReadFields.ContainsKey(name)) {
				object? value = CurrentReadFields[name];
				if (value == null) {
					return default; // Null or 0, whatever is appropriate.
				} else {
					// The value is not null.
					return (T)value;
				}
			}
			return defValue;
		}


		/// <summary>
		/// Identical to <see cref="Read{T}(string, T?)"/>, but sets a field named <paramref name="name"/> on the <see cref="ShadowClass"/> to the acquired value.
		/// </summary>
		/// <remarks>
		/// This is identical to doing <c><paramref name="into"/>.SetField(<paramref name="name"/>, <see cref="ClydeFile"/>.Read(<paramref name="name"/>, <paramref name="defValue"/>))</c>
		/// </remarks>
		/// <typeparam name="T"></typeparam>
		/// <param name="into">The <see cref="ShadowClass"/> to set the field on.</param>
		/// <param name="name">The name of the field (and data to read from the stream)</param>
		/// <param name="defValue">The default value if it's undefined.</param>
		public void ReadInto<T>(ShadowClass into, string name, T? defValue = default) {
			into.SetField(name, Read(name, defValue));
		}

		public object? ReadObject() {
			return Read(ObjectClass);
		}

		public void ReadEntriesInto(ShadowClass template, object[] container) {
			for (int idx = 0; idx < container.Length; idx++) {
				container[idx] = Read(template)!;
			}
		}

		private object? ReadValue(object shadowOrPresetObject, int id) {
#if DEBUG
			long pos = BaseStream.Position;
			Debug.WriteLine(pos);
			if (pos == 3408) Debugger.Break();
#endif
			if (shadowOrPresetObject is ShadowClass shadowInstance && !shadowInstance.IsTemplate) {
				shadowOrPresetObject = shadowInstance.TemplateType;
			}
			object workingClass = shadowOrPresetObject;
			bool isArgMap = shadowOrPresetObject is ShadowClass shdCls && shdCls.IsArgumentMap;
			bool isPrimitive = shadowOrPresetObject is ShadowClass shdCls2 && shdCls2.IsPrimitive;
			if ((shadowOrPresetObject is AbstractShadowClassBase shd && !shd.IsSealed && !isPrimitive) || isArgMap) {
				object read = ReadClass();
				if (read == NULL) {
					return null;
				}
				workingClass = read;
			}

			object? value = null;

			// OOO gets the actual class instance here, I can't do that because I don't have their engine classes here on hand.
			// The solution is to use my dump instead, which has all of the fields and their types (which were prepopulated into ShadowClass)
			// Their main use case is to see if there is a Streamer instance for the given type. That's easy to implement.
			if (workingClass is AbstractShadowClassBase shd2) {
				IStreamer streamer = IStreamer.GetStreamer(shd2);
				if (streamer != null) {
					value = streamer.Read(Reader);
					if (id != -1 && value != null) {
						CachedObjects[id] = value;
					}
					return value;
				}
			}

			int length = 0;
			if (workingClass is ShadowClassArray array) {
				length = IDReader.ReadNextSegmentLength();
				value = array.NewInstance(length);
			} else if (workingClass is ShadowClass mainShadow && !isArgMap) {
				// OOO never seemed to use immutable list/set/map/multisets in their format despite supporting it?
				// TODO: Should I support this?
				// For now: No
				// OOO *does* use an outer class for creating instances.
				// While we don't need this, it does do a call to Read so we need it.
				if (mainShadow.OuterClass != null && mainShadow.ShouldReadOuter) {
					Read(ObjectClass); // And then we proceed to do absolutely nothing with this lol
				}
				value = mainShadow.CloneTemplate();
			} else {
				bool ok = false;
				if (workingClass is List<object?> collection) {
					value = ReadEntries((value as List<object?>) ?? collection);
					ok = true;
				} else {
					Dictionary<object, object?>? map;
					if (workingClass is Dictionary<object, int> multiset) {
						value = ReadEntries((value as Dictionary<object, int>) ?? multiset);
						ok = true;
					} else if (workingClass is Dictionary<object, object?> || isArgMap) {
						map = workingClass as Dictionary<object, object?>;
						value = ReadEntries((value as Dictionary<object, object?>) ?? map ?? new Dictionary<object, object?>());
						ok = true;
					} else if (workingClass is Dictionary<object, List<int>> multimap) {
						throw new NotSupportedException("Multimaps are not supported at this time.");
					}
				}
				if (ok) return value;
				throw new InvalidOperationException("The working class was a non-shadow non-preset list/dictionary instance, and cannot be handled here.");
			}
			CachedObjects[id] = value;
			if (workingClass is ShadowClassArray array2) {
				ReadEntriesInto(array2.ElementType!, value != null ? (object[])value : new object[length]);
			} else {
				object? rep = ShadowClass.TryGetReplacementForSig(((AbstractShadowClassBase)value).Signature);
				if (rep != null) {
					if (rep is List<object?> collection) {
						value = ReadEntries((value as List<object?>) ?? collection);
					} else if (rep.GetType() == typeof(Dictionary<,>)) {
						if (rep is Dictionary<object, int> multiset) {
							value = ReadEntries((value as Dictionary<object, int>) ?? multiset);
						} else if (rep is Dictionary<object, object?> map) {
							value = ReadEntries((value as Dictionary<object, object?>) ?? map);
						}
					}
				} else {
					ShadowClass scValue = (ShadowClass)value;
					if (scValue.IsOOOExportable) {
						ReadFields(scValue, IDReader.ReadNextSegmentLength());
					}
				}
			}
			return value;
		}

		private void ReadFields(ShadowClass into, int numFields) {
			FindFields(into, numFields);

			// If one exists, go there afterwards.
			var readMethod = ReadFieldsProvider.GetReadFieldsMethod(into);
			readMethod?.Invoke(numFields, into, this);

			into.SetFields(CurrentReadFields);

			CurrentReadFields.Clear();
		}

		private List<object?> ReadEntries(List<object?> collection) {
			int amount = IDReader.ReadNextSegmentLength();
			for (int i = 0; i < amount; i++) {
				collection.Add(Read(ObjectClass));
			}
			return collection;
		}

		private Dictionary<object, object?> ReadEntries(Dictionary<object, object?> map) {
			int amount = IDReader.ReadNextSegmentLength();
			for (int i = 0; i < amount; i++) {
				map[Read(ObjectClass)!] = Read(ObjectClass);
			}
			return map;
		}

		private Dictionary<object, int> ReadEntries(Dictionary<object, int> multiset) {
			int amount = IDReader.ReadNextSegmentLength();
			for (int i = 0; i < amount; i++) {
				multiset[Read(ObjectClass)!] = IDReader.ReadNextSegmentLength();
			}
			return multiset;
		}

		internal void FindFields(ShadowClass into, int numFields) {
			Dictionary<string, object?> fields = new Dictionary<string, object?>();
			for (int idx = 0; idx < numFields; idx++) {
				ReadNextFieldInto(into, fields);
			}
			CurrentReadFields = fields;
		}

		[Obsolete("Reminder: This is useless with how ShadowClasses work. They are always populated.", true)]
		internal void ReadFieldsDefault(ShadowClass into) => throw new NotImplementedException();

		/// <summary>
		/// Only to be called by a ReadFields method.
		/// </summary>
		private void ReadNextFieldInto(ShadowClass into, Dictionary<string, object?> fields) {
			int id = IDReader.ReadID();
			if (!CachedFields.TryGetValue(into.TemplateType, out var _)) {
				CachedFields[into.TemplateType] = new Dictionary<int, FieldData>();
			}
			if (!CachedFields[into.TemplateType].TryGetValue(id, out FieldData? fieldTemplate)) {
				// Doesn't exist.
				object fieldNameRaw = Read(StringClass)!;
				string fieldName = (string)fieldNameRaw;
				object info = ReadClass();
				fieldTemplate = new FieldData(fieldName, info);
				CachedFields[into.TemplateType][id] = fieldTemplate;
			}
			fields[fieldTemplate.Name] = Read(fieldTemplate.Template);
		}

		/// <summary>
		/// Reads a class or value from the stream, which may be an instance of <see cref="AbstractShadowClassBase"/>.
		/// </summary>
		/// <returns></returns>
		private object ReadClass() {
			int classId = IDReader.ReadID();
			if (CachedClasses.TryGetValue(classId, out object? thing)) {
				return thing!;
			}
			string className = Reader.ReadUTF();
			byte flags = Reader.ReadByte();
			CachedClasses[classId] = ShadowClass.CreateFromOOOFormat(className, flags);
			return CachedClasses[classId];
		}

		private object? ReadAbstractShadow(AbstractShadowClassBase template) {
			if (template is ShadowClass shd && shd.IsPrimitive) {
				return ReadValue(template, -1);
			}
			int objId = IDReader.ReadID();
			if (CachedObjects.ContainsKey(objId)) {
				return CachedObjects[objId];
			}
			return ReadValue(template, objId);
		}

		/// <summary>
		/// A substitute to <see cref="Read(AbstractShadowClassBase)"/> that supports non-shadow instances.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		private object? Read(object other) {
			if (other is AbstractShadowClassBase template) return ReadAbstractShadow(template);
			int objId = IDReader.ReadID();
			if (CachedObjects.ContainsKey(objId)) {
				return CachedObjects[objId];
			}
			return ReadValue(other, objId);
		}

		/// <summary>
		/// Releases all the resources used by the <see cref="ClydeFile"/>.
		/// </summary>
		public void Dispose() {
			GC.SuppressFinalize(this);
			BaseStream.Dispose();
		}

		/// <summary>
		/// Represents a field read from the stream.
		/// </summary>
		private class FieldData {

			/// <summary>
			/// The name of this field.
			/// </summary>
			public string Name { get; }

			/// <summary>
			/// The type of this field, which will most likely be <see cref="AbstractShadowClassBase"/> but it could be another class such as a <see cref="List{T}"/> or <see cref="Dictionary{TKey, TValue}"/>.
			/// </summary>
			/// <remarks>
			/// This property's primary purpose is to store a dummy object of the desired type.
			/// </remarks>
			public object Template { get; }

			public FieldData(string name, object template) {
				Name = name;
				Template = template;
			}

		}
	}
}
