#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using OOOReader.Reader;
using OOOReader.Utility.Attributes;
using OOOReader.Utility.Data;
using OOOReader.Utility.Extension;
using OOOReader.ValueTypes;
using OOOReader.WithCustomReadFields;

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
		private Dictionary<int, object> CachedValues { get; } = new Dictionary<int, object>();

		/// <summary>
		/// Field names bound to a template for their type.
		/// </summary>
		private Dictionary<int, FieldData> CachedFields { get; } = new Dictionary<int, FieldData>();

		/// <summary>
		/// A ephemeral dictionary of the current read fields operation. It is cleared as soon as the main reading cycle has completed for an object.
		/// </summary>
		private Dictionary<string, object?> CurrentReadFields { get; set; } = new Dictionary<string, object?>();

		public ClydeFile(Stream input) {
			BaseStream = input;
			Reader = new BinaryReader(input);

			uint readHeader = Reader.ReadUInt32BE();
			if (readHeader != HEADER) throw new IOException($"Invalid header value! Expected 0x{HEADER:X8}, got 0x{readHeader:X8}");
			
			ClydeVersion version = (ClydeVersion)Reader.ReadUInt16BE();
			IDReader = IDReader.For(Reader, version);

			Compressed = Reader.ReadUInt16BE() == 0x1000;
			if (Compressed) {
				Reader = new BinaryReader(new InflaterInputStream(Reader.BaseStream));
			}

			CachedValues[0] = NULL;
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

		public object? ReadObject() {
			return Read(ShadowClass.GetOrCreate("java.lang.Object"));
		}

		public void ReadEntriesInto(ShadowClass template, object[] container) {
			for (int idx = 0; idx < container.Length; idx++) {
				container[idx] = Read(template)!;
			}
		}

		private object? ReadValue(AbstractShadowClassBase shadow, int id) {
			if (shadow is ShadowClass shadowInstance && !shadowInstance.IsTemplate) {
				shadow = shadowInstance.TemplateType!;
			}
			AbstractShadowClassBase workingClass = shadow;
			if (!shadow.IsSealed) {
				object read = ReadClass();
				if (read == NULL) {
					return null;
				}
				workingClass = (AbstractShadowClassBase)read;
			}
			// OOO gets the actual class instance here, I can't do that because I don't have their engine classes here on hand.
			// The solution is to use my dump instead, which has all of the fields and their types (which were prepopulated into ShadowClass)
			// Their main use case is to see if there is a Streamer instance for the given type. That's easy to implement.
			IStreamer streamer = IStreamer.GetStreamer(workingClass.Signature);
			object? value;
			if (streamer != null) {
				value = streamer.Read(Reader);
				if (id != -1 && value != null) {
					CachedValues[id] = value;
				}
				return value;
			}

			int length = 0;
			if (workingClass is ShadowClassArray array) {
				length = IDReader.ReadNextSegmentLength();
				value = array.NewInstance(length);
			} else if (workingClass is ShadowClass mainShadow) {
				// OOO never seemed to use immutable list/set/map/multisets in their format despite supporting it?
				// TODO: Should I support this?
				// For now: No
				value = mainShadow.CloneTemplate();
			} else {
				throw new InvalidOperationException("Working class instance was " + workingClass.GetType());
			}
			CachedValues[id] = value;
			if (workingClass is ShadowClassArray array2) {
				ReadEntriesInto(array2.ElementType!, value != null ? (object[])value : new object[length]);
			} else {
				// Collection, Map (not impl)
				// Read fields time!
				// In this case, we can completely skip the object marshaller.

				ReadFields((ShadowClass)value, IDReader.ReadNextSegmentLength());
			}
			return value;
		}

		private void ReadFields(ShadowClass cls, int numFields) {
			ReadFieldsDefault(cls, numFields);

			// If one exists, go there afterwards.
			var readMethod = ReadFieldsProvider.GetReadFieldsMethod(cls);
			readMethod?.Invoke(numFields, cls, this);

			CurrentReadFields.Clear();
		}

		internal void ReadFieldsDefault(ShadowClass cls, int numFields) {
			Dictionary<string, object?> fields = new Dictionary<string, object?>();
			for (int idx = 0; idx < numFields; idx++) {
				ReadNextFieldInto(fields);
			}
			cls.SetFields(fields);
			CurrentReadFields = fields;
		}

		/// <summary>
		/// Only to be called by a ReadFields method.
		/// </summary>
		private void ReadNextFieldInto(Dictionary<string, object?> fields) {
			int id = IDReader.ReadID();
			FieldData? fieldTemplate;
			if (!CachedFields.TryGetValue(id, out fieldTemplate)) {
				// Doesn't exist.
				fieldTemplate = new FieldData((string)Read(ShadowClass.GetOrCreate("java.lang.String"))!, (ShadowClass)ReadClass());
				CachedFields[id] = fieldTemplate;
			}
			fields[fieldTemplate.Name] = Read(fieldTemplate.Template);
		}

		/// <summary>
		/// Reads a class or value from the stream, which may be an instance of <see cref="AbstractShadowClassBase"/>.
		/// </summary>
		/// <returns></returns>
		private object ReadClass() {
			int classId = IDReader.ReadID();
			if (CachedValues.TryGetValue(classId, out object? thing)) {
				return thing!;
			}
			string className = Reader.ReadUTF();
			byte flags = Reader.ReadByte();
			CachedValues[classId] = ShadowClass.CreateFromOOOFormat(className, flags);
			return CachedValues[classId];
		}

		private object? Read(ShadowClass template) {
			if (template.IsPrimitive) {
				return ReadValue(template, -1);
			}
			int objId = IDReader.ReadID();
			if (CachedValues.ContainsKey(objId)) {
				return CachedValues[objId];
			}
			return ReadValue(template, objId);
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
			/// The type of this field.
			/// </summary>
			public ShadowClass Template { get; }

			public FieldData(string name, ShadowClass template) {
				Name = name;
				Template = template;
			}

		}
	}
}
