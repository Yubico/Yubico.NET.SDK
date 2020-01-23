// Copyright 2021 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Formats.Cbor;
using System.Security.Cryptography.X509Certificates;

namespace Yubico.YubiKey.Fido2.Serialization
{
    internal static class Ctap2CborSerializer
    {
        /// <summary>
        /// Checks if property is a nullable reference OR a nullable value type
        /// </summary>
        private static bool IsNullableProperty(PropertyInfo property) =>
            property.CustomAttributes.Any(prop =>
               prop.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute"
            )
            ||
            property.PropertyType.CustomAttributes.Any(prop =>
               prop.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute"
            )
            ||
            (property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) != null);

        /// <summary>
        /// Deserializes CBOR-encoded bytes into the object specified by <c>T</c>
        /// </summary>
        /// <remarks>
        /// <p>
        /// Unexpected or misssing data will result in a <c>CborContentException</c>.
        /// </p>
        /// <p>
        /// Only the public properties of the type will be considered. If the CBOR data uses integer keys 
        /// the target type must be annotated with <c>CborLabelIdAttribute</c> for every public property.
        /// </p>
        /// <p>
        /// Properties not marked with the label attribute will be decoded assuming that keys are encoded as 
        /// 'camelCase' (rather than 'PascalCase') strings. No intermingling of string keys and integer keys 
        /// is supported; the type <c>T</c> must have all of it public properties annotated or none of them.
        /// </p>
        /// </remarks>
        /// <typeparam name="T">The type to deserialize the data into</typeparam>
        /// <param name="data">CBOR-encoded data</param>
        /// <returns>An instance of <c>T</c> deserialized from the data</returns>
        public static T Deserialize<T>(byte[] data)
        {
            var reader = new CborReader(data, CborConformanceMode.Ctap2Canonical);
            return Deserialize<T>(reader);
        }

        /// <summary>
        /// Deserializes data specifying an instance of <c>T</c> from the passed reader.
        /// </summary>
        /// <typeparam name="T">The expected type of the data in the reader</typeparam>
        /// <param name="reader">An initialized reader in Ctap2Canonical conformance mode</param>
        /// <returns>An instance of <c>T</c> deserialized from the reader</returns>
        public static T Deserialize<T>(CborReader reader) => Deserialize<T>(reader, null);

        private static object CallDeserialize(CborReader reader, Type t, PropertyInfo? propertyInfo)
        {
            // We use refelection to dynamically call Deserialize with the correct type parameter.
            // This is similar to how JsonSerializer works.

            MethodInfo method = typeof(Ctap2CborSerializer).GetMethod(
                nameof(Deserialize),
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new Type[] { typeof(CborReader), typeof(PropertyInfo) },
                null
            );

            return method.MakeGenericMethod(t).Invoke(null, new object?[] { reader, propertyInfo });
        }

        private static bool IsDictionary(Type type) =>
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);

        private static T Deserialize<T>(CborReader reader, PropertyInfo? propertyInfo)
        {
            if (reader is null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            return targetType switch
            {
                var _ when IsSimpleType(targetType)     => (T)ReadSimpleValue(reader, targetType, propertyInfo),
                var _ when targetType.IsArray           => DeserializeArray<T>(reader),
                var _ when IsDictionary(targetType)     => DeserializeDictionary<T>(reader),
                _                                       => DeserializePlainObject<T>(reader),
            };
        }

        private static T DeserializeArray<T>(CborReader reader)
        {
            // The target type is an array
            Type targetType = typeof(T);

            // Retrieve the element type
            Type elementType = targetType.GetElementType();

            int? elemCount = reader.ReadStartArray() ?? 
                throw new CborContentException(ExceptionMessages.Ctap2CborIndefiniteLength);

            // Construct and fill an instance of elementType[]
            var resultArray = (T)targetType.GetConstructor(new[] { typeof(int) }).Invoke(new object[] { elemCount.Value });
            MethodInfo setValueMethod = targetType.GetMethod(nameof(Array.SetValue), new[] { elementType, typeof(int) });

            for (int i = 0; i < elemCount.Value; i++)
            {
                _ = setValueMethod.Invoke(resultArray, new[] { CallDeserialize(reader, elementType, null), i });
            }

            reader.ReadEndArray();

            return resultArray;
        }

        private static T DeserializeDictionary<T>(CborReader reader)
        {
            // The target type is a Dictionary
            Type targetType = typeof(T);

            // Retrieve the key and value types
            Type keyType = targetType.GetGenericArguments()[0];
            Type valueType = targetType.GetGenericArguments()[1];

            // Construct an empty version of targetType
            var result = (T)CallEmptyContructor(targetType);

            // Recurse, reading keys and values alternatingly
            int? elemCount = reader.ReadStartMap()
                ?? throw new CborContentException(ExceptionMessages.Ctap2CborIndefiniteLength);

            for (int i = 0; i < elemCount.Value; i++)
            {
                object key = CallDeserialize(reader, keyType, null);
                object value = CallDeserialize(reader, valueType, null);
                _ = targetType.GetMethod(nameof(IDictionary.Add)).Invoke(result, new[] { key, value });
            }
            reader.ReadEndMap();
            return result;
        }

        private static T DeserializePlainObject<T>(CborReader reader)
        {
            Type targetType = typeof(T);

            // This is a plain object; construct the result object
            var result = (T)CallEmptyContructor(targetType);

            // Check if we are using known CBOR label ids
            bool usingIds = targetType.GetProperties().Any(pi => !(pi.GetCustomAttribute(typeof(CborLabelIdAttribute)) is null));

            if (usingIds)
            {
                // Use integer label IDs as keys
                var cmr = new CborMapReader<int>(reader, (reader) => Deserialize<int>(reader, null));

                // Deserialize the value of each present property
                cmr.StartReading();
                foreach (PropertyInfo pi in GetPresentPropertiesById(targetType, cmr))
                {
                    Type propertyType = pi.PropertyType;
                    object value = CallDeserialize(reader, propertyType, pi);

                    pi.SetValue(result, value);
                }
                cmr.StopReading();
            }
            else
            {
                // Use strings as keys
                var cmr = new CborMapReader<string>(reader, (reader) => Deserialize<string>(reader, null));

                // Deserialize the value of each present property
                cmr.StartReading();
                foreach (PropertyInfo pi in GetPresentPropertiesByName(targetType, cmr))
                {
                    Type propertyType = pi.PropertyType;
                    object value = CallDeserialize(reader, propertyType, pi);

                    pi.SetValue(result, value);
                }
                cmr.StopReading();
            }

            return result;
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "CTAP spec requires lowercase")]
        private static string SerializePropertyName(PropertyInfo propertyInfo)
        {
            CborPropertyNameAttribute? customPropertyName = propertyInfo.GetCustomAttribute<CborPropertyNameAttribute>();
            if (customPropertyName is null)
            {
                return propertyInfo.Name[0..1].ToLowerInvariant() + propertyInfo.Name[1..];
            }
            else
            {
                return customPropertyName.Name;
            }
        }

        /// <summary>
        /// Returns a list of properties and IDs sorted by their CBOR label ID
        /// </summary>
        /// <param name="targetType"></param>
        /// <returns></returns>
        private static IEnumerable<(PropertyInfo p, int labelId)> GetPropertiesById(Type targetType)
        {
            PropertyInfo[] props = targetType.GetProperties();
            var propTuplesByLabelId = props
                .Select(p => (p, p.GetCustomAttribute<CborLabelIdAttribute>().LabelId))
                .ToList();
            propTuplesByLabelId.Sort((x, y) => x.LabelId.CompareTo(y.LabelId));
            return propTuplesByLabelId;
        }


        private static IEnumerable<PropertyInfo> GetPresentPropertiesById(Type targetType, CborMapReader<int> cmr)
        {
            // Yield each property that is present
            foreach ((PropertyInfo pi, int labelId) in GetPropertiesById(targetType))
            {
                bool isRequired = !IsNullableProperty(pi);

                if (isRequired)
                {
                    cmr.ReadLabel(labelId, pi.Name);
                }
                else if (!cmr.TryReadLabel(labelId))
                {
                    continue;
                }

                yield return pi;
            }
        }

        /// <summary>
        /// Returns a list of properties and name sorted by their CBOR-serialized name
        /// </summary>
        private static IEnumerable<(PropertyInfo pi, string serializedName)> GetPropertiesByName(Type targetType)
        {
            List<(PropertyInfo p, string Name)> propTuplesByName = targetType
                .GetProperties()
                .Select(p => (p, SerializePropertyName(p)))
                .ToList();

            propTuplesByName.Sort((x, y) => x.Name.Length != y.Name.Length
                ? x.Name.Length.CompareTo(y.Name.Length)
                : string.Compare(x.Name, y.Name, StringComparison.Ordinal));

            return propTuplesByName;
        }

        private static IEnumerable<PropertyInfo> GetPresentPropertiesByName(Type targetType, CborMapReader<string> cmr)
        {
            // Yield each property that is present
            foreach ((PropertyInfo pi, string serializedName) in GetPropertiesByName(targetType))
            {
                bool isRequired = !IsNullableProperty(pi);

                if (isRequired)
                {
                    cmr.ReadLabel(serializedName, pi.Name);
                }
                else if (!cmr.TryReadLabel(serializedName))
                {
                    continue;
                }

                yield return pi;
            }
        }

        private static object CallEmptyContructor(Type t)
        {
            ConstructorInfo constructor = t.GetConstructor(Type.EmptyTypes);
            return constructor.Invoke(Array.Empty<object>());
        }

        private static T[] ReadArray<T>(CborReader reader, Func<CborReader, T> readValue)
        {
            int? elemCount = reader.ReadStartArray()
                ?? throw new CborContentException(ExceptionMessages.Ctap2CborIndefiniteLength);

            T[] result = Enumerable.Range(0, elemCount.Value)
                .Select(e => readValue(reader))
                .ToArray();

            reader.ReadEndArray();

            return result;
        }

        /// <summary>
        /// Serializes data in the given object to CBOR.
        /// </summary>
        /// <remarks>
        /// A simple object (byte[], int, uint, string, or bool), array, dictionary, or plain object can be serialized.
        /// Only public properties of plain objects are used. Label IDs for integers as map keys are only available if 
        /// each property has been annotated using <see cref="CborLabelIdAttribute"/>.
        /// </remarks>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] Serialize(object? data)
        {
            var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
            Serialize(writer, data);
            return writer.Encode();
        }

        /// <summary>
        /// Serializes data in the given object using the given CborWriter.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="writer"></param>
        public static void Serialize(CborWriter writer, object? data) =>
            Serialize(writer, data, null);

        private static void Serialize(CborWriter writer, object? data, PropertyInfo? propertyInfo)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            switch (data)
            {
                case null:
                    writer.WriteNull();
                    break;
                case var d when IsSimpleValue(d):
                    WriteSimpleValue(writer, d, propertyInfo);
                    break;
                case Array array:
                    WriteArray(writer, array, propertyInfo); 
                    break;
                case IDictionary dict:
                    WriteDictionary(writer, dict, propertyInfo); 
                    break;
                default:
                    WritePlainObject(writer, data);
                    break;
            };
        }

        private static void WritePlainObject(CborWriter writer, object data)
        {
            // Check if we are using CBOR integer ids as map keys
            bool usingIds = data.GetType().GetProperties().Any(pi => !(pi.GetCustomAttribute(typeof(CborLabelIdAttribute)) is null));
            if (usingIds)
            {
                var propTuplesByLabelId = GetPropertiesById(data.GetType())
                    .Where(tuple => !(tuple.p.GetValue(data) is null))
                    .ToList();

                writer.WriteStartMap(propTuplesByLabelId.Count);
                foreach ((PropertyInfo pi, int labelId) in propTuplesByLabelId)
                {
                    // write key
                    writer.WriteInt32(labelId);

                    // write value
                    object value = pi.GetValue(data);
                    Serialize(writer, value, pi);
                }
                writer.WriteEndMap();
            }
            else
            {
                var propTuplesByName = GetPropertiesByName(data.GetType())
                    .Where(((PropertyInfo pi, string serializedName) tuple) =>
                        !(tuple.pi.GetValue(data) is null)
                    )
                    .ToList();

                writer.WriteStartMap(propTuplesByName.Count);
                foreach ((PropertyInfo pi, string Name) in propTuplesByName)
                {
                    // write key
                    writer.WriteTextString(SerializePropertyName(pi));

                    // write value
                    object value = pi.GetValue(data);
                    Serialize(writer, value, pi);
                }
                writer.WriteEndMap();
            }
        }

        private static bool IsPropertySerializedAsUnsigned(PropertyInfo propertyInfo) =>
            !(propertyInfo.GetCustomAttribute(typeof(CborSerializeAsUnsignedAttribute)) is null);

        private static bool IsSimpleType(Type t) =>
            t == typeof(byte[])
            || t == typeof(string)
            || t == typeof(int)
            || t == typeof(uint)
            || t == typeof(bool)
            || t == typeof(Uri)
            || t == typeof(X509Certificate2)
            || (t.IsEnum && (Enum.GetUnderlyingType(t) == typeof(int) || Enum.GetUnderlyingType(t) == typeof(uint)));

        private static object ReadSimpleValue(CborReader reader, Type t, PropertyInfo? propertyInfo)
        {
            if (t == typeof(byte[]))
            {
                return reader.ReadByteString();
            }
            else if (t == typeof(string))
            {
                return reader.ReadTextString();
            }
            else if (t == typeof(int))
            {
                if (!(propertyInfo is null) && IsPropertySerializedAsUnsigned(propertyInfo))
                {
                    return unchecked((int)reader.ReadUInt32());
                }
                else
                {
                    return reader.ReadInt32();
                }
            }
            else if (t == typeof(uint))
            {
                return reader.ReadUInt32();
            }
            else if (t == typeof(bool))
            {
                return reader.ReadBoolean();
            }
            else if (t == typeof(Uri))
            {
                return new Uri(reader.ReadTextString());
            }
            else if (t == typeof(X509Certificate2))
            {
                return new X509Certificate2(reader.ReadByteString());
            }
            else if (t.IsEnum && (Enum.GetUnderlyingType(t) == typeof(int) || Enum.GetUnderlyingType(t) == typeof(uint)))
            {
                if (Enum.GetUnderlyingType(t) == typeof(int))
                {
                    return Enum.ToObject(t, reader.ReadInt32());
                }
                else if (Enum.GetUnderlyingType(t) == typeof(uint))
                {
                    return Enum.ToObject(t, reader.ReadUInt32());
                }
            }

            throw new CborContentException(ExceptionMessages.Ctap2CborUnexpectedValue);
        }

        private static bool IsSimpleValue(object val) =>
            val switch
            {
                byte[] _ => true,
                string _ => true,
                int _ => true,
                uint _ => true,
                bool _ => true,
                Uri _ => true,
                X509Certificate2 _ => true,
                Enum e =>
                    Enum.GetUnderlyingType(e.GetType()) == typeof(int)
                    || Enum.GetUnderlyingType(e.GetType()) == typeof(uint),
                _ => false
            };

        private static void WriteSimpleValue(CborWriter writer, object value, PropertyInfo? propertyInfo)
        {
            Action writeValueAction = value switch
            {
                byte[] ba => () => writer.WriteByteString(ba),
                string v => () => writer.WriteTextString(v),
                int i => () =>
                {
                    if (!(propertyInfo is null) && IsPropertySerializedAsUnsigned(propertyInfo))
                    {
                        writer.WriteUInt32(unchecked((uint)i));
                    }
                    else
                    {
                        writer.WriteInt32(i);
                    }
                }
                ,
                uint ui => () => writer.WriteUInt32(ui),
                bool b => () => writer.WriteBoolean(b),
                Uri uri => () => writer.WriteTextString(uri.ToString()),
                X509Certificate2 x509Certificate => () => writer.WriteByteString(x509Certificate.RawData),
                Enum e => () =>
                {
                    Type t = Enum.GetUnderlyingType(e.GetType());
                    if (t == typeof(int))
                    {
                        writer.WriteInt32(((IConvertible)e).ToInt32(CultureInfo.InvariantCulture));
                    }
                    else if (t == typeof(uint))
                    {
                        writer.WriteUInt32(((IConvertible)e).ToUInt32(CultureInfo.InvariantCulture));
                    }
                }
                ,
                _ => () => { }
            };
            writeValueAction();
        }

        private static void WriteArray(CborWriter writer, Array array, PropertyInfo? propertyInfo)
        {
            writer.WriteStartArray(array.Length);
            foreach (object item in array)
            {
                Serialize(writer, item, propertyInfo);
            }
            writer.WriteEndArray();
        }

        private static void WriteDictionary(CborWriter writer, IDictionary dict, PropertyInfo? propertyInfo)
        {
            writer.WriteStartMap(dict.Count);
            foreach (object k in dict.Keys)
            {
                Serialize(writer, k, propertyInfo);
                Serialize(writer, dict[k], propertyInfo);
            }
            writer.WriteEndMap();
        }
    }
}
