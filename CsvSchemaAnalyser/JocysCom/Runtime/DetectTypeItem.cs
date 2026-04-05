using System;
using System.Collections.Generic;
using System.Linq;

namespace JocysCom.ClassLibrary.Runtime
{
	/// <summary>
	/// Runtime type inference utility for textual data.
	/// Infers the narrowest .NET type, string length range, decimal precision/scale,
	/// ASCII-only constraint, and requiredness. Bridges detected types to SQL Server column definitions
	/// via Data.SqlHelper.
	/// </summary>
	public class DetectTypeItem
	{
		public DetectTypeItem()
		{
			Log = new List<string>();
			// All types are available from the start.
			// List will be narrowed down later.
			AvailableTypes = _TypeCodes.ToList();
			IsAscii = true;
			IsRequired = true;
		}

		/// <summary>
		/// Creates a new item by inferring the narrowest .NET type capable of representing all provided string values.
		/// </summary>
		/// <param name="values">String values to analyze for type inference.</param>
		/// <returns>A new <see cref="DetectTypeItem"/> with detection results populated.</returns>
		public static DetectTypeItem DetectType(string[] values)
		{
			var item = new DetectTypeItem();
			DetectType(ref item, values);
			return item;
		}

		public string Name { get; set; }
		public Type Type { get; set; }
		public int MinLength { get; set; }
		public int MaxLength { get; set; }
		public int DecimalPrecision { get; set; }
		public int DecimalScale { get; set; }
		public bool IsAscii { get; set; }
		public bool IsRequired { get; set; }
		public List<string> Log { get; set; }

		public List<TypeCode> AvailableTypes { get; set; }

		public new string ToString()
		{
			return $"Type={Type,-16} Name={Name,-26} Min={MinLength,3}, Max={MaxLength,3}, ASCII={(IsAscii ? 1 : 0)}, Required={(IsRequired ? 1 : 0)}";
		}

		/// <summary>
		/// Generates a C# property declaration string with this item's detected type, nullability,
		/// and an inline comment indicating min/max length and ASCII constraint.
		/// </summary>
		public string ToCSharpString()
		{
			var type = $"{Type?.Name}{(IsRequired ? " " : "?")}";
			var name = $"{Name};";
			return $"{type,-9} {name,-27} // Min={MinLength,3}, Max={MaxLength,3}{(Type == typeof(string) && IsAscii ? ", ASCII" : "")}".Trim();
		}

		/// <summary>
		/// Constructs a SQL Server column definition string using Data.SqlHelper mapping.
		/// Includes column name, data type with length or precision/scale, and nullability.
		/// Appends a trailing comma.
		/// </summary>
		public string ToSqlString()
		{
			var code = AvailableTypes.FirstOrDefault();
			var sqlType = Data.SqlHelper.GetSqlDataType(code, MinLength, MaxLength, !IsAscii);
			//var isUnicode = Data.SqlHelper.HaveSize(sqlType.ToString());
			var haveSize = Data.SqlHelper.HaveSize(sqlType.ToString());
			// [ControlName]   VARCHAR (256) NOT NULL,
			var columnName = $"[{Name}]";
			var columnType = $"{sqlType}";
			var s = $"{columnName,-24}";
			if (haveSize)
			{
				columnType += code == TypeCode.Decimal
					? $"({DecimalScale}, {DecimalPrecision})"
					: MaxLength == -1 ? "(MAX)" : $"({MaxLength})";
			}
			s += $"{columnType,-12}";
			if (IsRequired)
				s += " NOT NULL";
			s = s.Trim() + ",";
			return s;
		}

		/// <summary>
		/// Type codes to check. Order is important: from least to most flexible type.
		/// </summary>
		private static TypeCode[] _TypeCodes = new TypeCode[]
		{
			TypeCode.Boolean,
			TypeCode.Byte,
			TypeCode.SByte,
			TypeCode.Int16,
			TypeCode.Int32,
			TypeCode.Int64,
			TypeCode.UInt16,
			TypeCode.UInt32,
			TypeCode.UInt64,
			TypeCode.Single,
			TypeCode.Char,
			TypeCode.DateTime,
			TypeCode.Double,
			TypeCode.Decimal,
			TypeCode.String,
			// TypeCode.DBNull,
			// TypeCode.Empty,
			// TypeCode.Object,
		};

		/// <summary>
		/// Detect leading zero, because time in databases could be stored as string "0123".
		/// </summary>
		private static bool HaveLeadingZero(string s)
		{
			if (string.IsNullOrEmpty(s))
				return false;
			return s.Length > 1 && s.StartsWith("0");
		}

		/// <summary>
		/// Infers the narrowest .NET type that can represent all provided string values.
		/// Iterates predefined TypeCodes (strictest to most flexible), eliminating incompatible types.
		/// Updates this instance's AvailableTypes, Type (first compatible), MinLength, MaxLength,
		/// DecimalPrecision, DecimalScale, IsAscii, and IsRequired properties.
		/// </summary>
		/// <param name="item">DetectTypeItem instance to populate; if null, a new instance is created.</param>
		/// <param name="values">String values to analyze; throws ArgumentNullException if null.</param>
		public static void DetectType(ref DetectTypeItem item, params string[] values)
		{
			if (values is null)
				throw new ArgumentNullException(nameof(values));
			if (item is null)
				item = new DetectTypeItem();
			// Order matters. Strictest on the top. First available type will be returned.
			// If all values can be parsed to Int16 then it can be parsed to Int32 and Int64 too.
			//Convert.ChangeType(value, colType);
			for (int i = 0; i < values.Length; i++)
			{
				var value = values[i];
				if (string.IsNullOrEmpty(value))
				{
					item.IsRequired = false;
					continue;
				}
				// If minimum length not set then use available, otherwise get smaller.
				item.MinLength = item.MinLength == 0 ? value.Length : Math.Min(item.MinLength, value.Length);
				// Determine maximum length.
				item.MaxLength = Math.Max(item.MaxLength, value.Length);
				// Value is not ASCII if character code is outside of 128.
				item.IsAscii &= value.All(x => x < 128);
				// Test against available types.
				var tcs = item.AvailableTypes.ToArray();
				foreach (var tc in tcs)
				{
					var remove = false;
					switch (tc)
					{
						case TypeCode.Boolean:
							bool resultBool;
							if (!bool.TryParse(value, out resultBool))
								remove = true;
							break;
						case TypeCode.Byte:
							byte resultByte;
							if (!byte.TryParse(value, out resultByte))
								remove = true;
							break;
						case TypeCode.Char:
							char resultChar;
							if (!char.TryParse(value, out resultChar))
								remove = true;
							break;
						case TypeCode.DateTime:
							DateTime resultDateTime;
							if (!DateTime.TryParse(value, out resultDateTime))
								remove = true;
							break;
						case TypeCode.Decimal:
							decimal resultDecimal;
							if (!decimal.TryParse(value, out resultDecimal))
								remove = true;
							var d = (System.Data.SqlTypes.SqlDecimal)resultDecimal;
							item.DecimalPrecision = Math.Max(item.DecimalPrecision, d.Precision);
							item.DecimalScale = Math.Max(item.DecimalScale, d.Scale);
							break;
						case TypeCode.Double:
							double resultDouble;
							if (!double.TryParse(value, out resultDouble))
								remove = true;
							break;
						case TypeCode.Int16:
							short resultShort;
							if (!short.TryParse(value, out resultShort) && !HaveLeadingZero(value))
								remove = true;
							break;
						case TypeCode.Int32:
							int resultInt;
							if (!int.TryParse(value, out resultInt) && !HaveLeadingZero(value))
								remove = true;
							break;
						case TypeCode.Int64:
							long resultLong;
							if (!long.TryParse(value, out resultLong) && !HaveLeadingZero(value))
								remove = true;
							break;
						case TypeCode.SByte:
							sbyte resultSByte;
							if (!sbyte.TryParse(value, out resultSByte) && !HaveLeadingZero(value))
								remove = true;
							break;
						case TypeCode.Single:
							float resultFloat;
							if (!float.TryParse(value, out resultFloat) && !HaveLeadingZero(value))
								remove = true;
							break;
						case TypeCode.UInt16:
							ushort resultUShort;
							if (!ushort.TryParse(value, out resultUShort) && !HaveLeadingZero(value))
								remove = true;
							break;
						case TypeCode.UInt32:
							uint resultUInt;
							if (!uint.TryParse(value, out resultUInt) && !HaveLeadingZero(value))
								remove = true;
							break;
						case TypeCode.UInt64:
							ulong resultULong;
							if (!ulong.TryParse(value, out resultULong) && !HaveLeadingZero(value))
								remove = true;
							break;
						default:
							break;
					}
					if (remove)
					{
						item.Log.Add(string.Format($"Removed {tc,-8} - Value: {value}"));
						item.AvailableTypes.Remove(tc);
						item.Type = item.AvailableTypes.Count == 0 ? null : Type.GetType("System." + item.AvailableTypes[0]);
					}
				}
			}
		}

	}
}