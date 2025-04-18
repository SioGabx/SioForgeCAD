﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace SioForgeCAD.JSONParser
{
    // Really simple JSON parser in ~300 lines
    // - Attempts to parse JSON files with minimal GC allocation
    // - Nice and simple "[1,2,3]".FromJson<List<int>>() API
    // - Classes and structs can be parsed too!
    //      class Foo { public int Value; }
    //      "{\"Value\":10}".FromJson<Foo>()
    // - Can parse JSON without type information into Dictionary<string,object> and List<object> e.g.
    //      "[1,2,3]".FromJson<object>().GetType() == typeof(List<object>)
    //      "{\"Value\":10}".FromJson<object>().GetType() == typeof(Dictionary<string,object>)
    // - No JIT Emit support to support AOT compilation on iOS
    // - Attempts are made to NOT throw an exception if the JSON is corrupted or invalid: returns null instead.
    // - Only public fields and property setters on classes/structs will be written to
    //
    // Limitations:
    // - No JIT Emit support to parse structures quickly
    // - Limited to parsing <2GB JSON files (due to int.MaxValue)
    // - Parsing of abstract classes or interfaces is NOT supported and will throw an exception.
    public static class JSONParser
    {
        [ThreadStatic] static Stack<List<string>> splitArrayPool;
        [ThreadStatic] static StringBuilder stringBuilder;
        [ThreadStatic] static Dictionary<Type, Dictionary<string, FieldInfo>> fieldInfoCache;
        [ThreadStatic] static Dictionary<Type, Dictionary<string, PropertyInfo>> propertyInfoCache;

        public static T FromJson<T>(this string json)
        {
            // Initialize, if needed, the ThreadStatic variables
            if (propertyInfoCache == null)
            {
                propertyInfoCache = new Dictionary<Type, Dictionary<string, PropertyInfo>>();
            }

            if (fieldInfoCache == null)
            {
                fieldInfoCache = new Dictionary<Type, Dictionary<string, FieldInfo>>();
            }

            if (stringBuilder == null)
            {
                stringBuilder = new StringBuilder();
            }

            if (splitArrayPool == null)
            {
                splitArrayPool = new Stack<List<string>>();
            }

            //Remove all whitespace not within strings to make parsing simpler
            stringBuilder.Length = 0;
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                if (c == '"')
                {
                    i = AppendUntilStringEnd(true, i, json);
                    continue;
                }
                if (char.IsWhiteSpace(c))
                {
                    continue;
                }

                stringBuilder.Append(c);
            }

            //Parse the thing!
            return (T)ParseValue(typeof(T), stringBuilder.ToString());
        }

        static int AppendUntilStringEnd(bool appendEscapeCharacter, int startIdx, string json)
        {
            stringBuilder.Append(json[startIdx]);
            for (int i = startIdx + 1; i < json.Length; i++)
            {
                if (json[i] == '\\')
                {
                    if (appendEscapeCharacter)
                    {
                        stringBuilder.Append(json[i]);
                    }

                    stringBuilder.Append(json[i + 1]);
                    i++;//Skip next character as it is escaped
                }
                else if (json[i] == '"')
                {
                    stringBuilder.Append(json[i]);
                    return i;
                }
                else
                {
                    stringBuilder.Append(json[i]);
                }
            }
            return json.Length - 1;
        }

        //Splits { <value>:<value>, <value>:<value> } and [ <value>, <value> ] into a list of <value> strings
        static List<string> Split(string json)
        {
            List<string> splitArray = splitArrayPool.Count > 0 ? splitArrayPool.Pop() : new List<string>();
            splitArray.Clear();
            if (json.Length == 2)
            {
                return splitArray;
            }

            int parseDepth = 0;
            stringBuilder.Length = 0;
            for (int i = 1; i < json.Length - 1; i++)
            {
                switch (json[i])
                {
                    case '[':
                    case '{':
                        parseDepth++;
                        break;
                    case ']':
                    case '}':
                        parseDepth--;
                        break;
                    case '"':
                        i = AppendUntilStringEnd(true, i, json);
                        continue;
                    case ',':
                    case ':':
                        if (parseDepth == 0)
                        {
                            splitArray.Add(stringBuilder.ToString());
                            stringBuilder.Length = 0;
                            continue;
                        }
                        break;
                }

                stringBuilder.Append(json[i]);
            }

            splitArray.Add(stringBuilder.ToString());

            return splitArray;
        }

        internal static object ParseValue(Type type, string json)
        {
            if (type == typeof(string))
            {
                if (json.Length <= 2)
                {
                    return string.Empty;
                }

                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 1; i < json.Length - 1; ++i)
                {
                    if (json[i] == '\\' && i + 1 < json.Length - 1)
                    {
                        int j = "\"\\nrtbf/".IndexOf(json[i + 1]);
                        if (j >= 0)
                        {
                            stringBuilder.Append("\"\\\n\r\t\b\f/"[j]);
                            ++i;
                            continue;
                        }
                        if (json[i + 1] == 'u' && i + 5 < json.Length - 1)
                        {
                            if (UInt32.TryParse(json.Substring(i + 2, 4), System.Globalization.NumberStyles.AllowHexSpecifier, null, out uint c))
                            {
                                stringBuilder.Append((char)c);
                                i += 5;
                                continue;
                            }
                        }
                    }
                    stringBuilder.Append(json[i]);
                }
                return stringBuilder.ToString();
            }
            if (type == typeof(int))
            {
                _ = int.TryParse(json, out int result);
                return result;
            }
            if (type == typeof(byte))
            {
                _ = byte.TryParse(json, out byte result);
                return result;
            }
            if (type == typeof(float))
            {
                _ = float.TryParse(json, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float result);
                return result;
            }
            if (type == typeof(double))
            {
                _ = double.TryParse(json, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double result);
                return result;
            }
            if (type == typeof(bool))
            {
                return json.Equals("true", StringComparison.CurrentCultureIgnoreCase);
            }
            if (json == "null")
            {
                return null;
            }
            if (type.IsArray)
            {
                Type arrayType = type.GetElementType();
                if (json[0] != '[' || json[json.Length - 1] != ']')
                {
                    return null;
                }

                List<string> elems = Split(json);
                Array newArray = Array.CreateInstance(arrayType, elems.Count);
                for (int i = 0; i < elems.Count; i++)
                {
                    newArray.SetValue(ParseValue(arrayType, elems[i]), i);
                }

                splitArrayPool.Push(elems);
                return newArray;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type listType = type.GetGenericArguments()[0];
                if (json[0] != '[' || json[json.Length - 1] != ']')
                {
                    return null;
                }

                List<string> elems = Split(json);
                var list = (IList)type.GetConstructor(new Type[] { typeof(int) }).Invoke(new object[] { elems.Count });
                for (int i = 0; i < elems.Count; i++)
                {
                    list.Add(ParseValue(listType, elems[i]));
                }

                splitArrayPool.Push(elems);
                return list;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                Type keyType, valueType;
                {
                    Type[] args = type.GetGenericArguments();
                    keyType = args[0];
                    valueType = args[1];
                }

                // Vérifier si la clé est une string ou une énumération
                bool isEnumKey = keyType.IsEnum;
                if (!isEnumKey && keyType != typeof(string))
                {
                    //Refuse to parse dictionary keys that aren't of type string
                    return null;
                }
                //Must be a valid dictionary element
                if (json.Length == 0)
                {
                    return null;
                }

                if (json[0] != '{' || json[json.Length - 1] != '}')
                {
                    return null;
                }
                //The list is split into key/value pairs only, this means the split must be divisible by 2 to be valid JSON
                List<string> elems = Split(json);
                if (elems.Count % 2 != 0)
                {
                    return null;
                }

                var dictionary = (IDictionary)type.GetConstructor(new Type[] { typeof(int) }).Invoke(new object[] { elems.Count / 2 });
                for (int i = 0; i < elems.Count; i += 2)
                {
                    if (elems[i].Length <= 2)
                    {
                        continue;
                    }

                    //string keyValue = elems[i].Substring(1, elems[i].Length - 2);
                    string keyValue = elems[i].Substring(1, elems[i].Length - 2);
                    object key;

                    if (isEnumKey)
                    {
                        try
                        {
                            key = Enum.Parse(keyType, keyValue, true); // Conversion string -> Enum
                        }
                        catch
                        {
                            return null; // Clé invalide pour un Enum
                        }
                    }
                    else
                    {
                        key = keyValue; // La clé est déjà une string
                    }
                    object val = ParseValue(valueType, elems[i + 1]);
                    dictionary.Add(key, val);
                }
                return dictionary;
            }
            if (type == typeof(object))
            {
                return ParseAnonymousValue(json);
            }
            if (json[0] == '{' && json[json.Length - 1] == '}')
            {
                return ParseObject(type, json);
            }

            return null;
        }

        static object ParseAnonymousValue(string json)
        {
            if (json.Length == 0)
            {
                return null;
            }

            if (json[0] == '{' && json[json.Length - 1] == '}')
            {
                List<string> elems = Split(json);
                if (elems.Count % 2 != 0)
                {
                    return null;
                }

                var dict = new Dictionary<string, object>(elems.Count / 2);
                for (int i = 0; i < elems.Count; i += 2)
                {
                    dict.Add(elems[i].Substring(1, elems[i].Length - 2), ParseAnonymousValue(elems[i + 1]));
                }

                return dict;
            }
            if (json[0] == '[' && json[json.Length - 1] == ']')
            {
                List<string> items = Split(json);
                var finalList = new List<object>(items.Count);
                for (int i = 0; i < items.Count; i++)
                {
                    finalList.Add(ParseAnonymousValue(items[i]));
                }

                return finalList;
            }
            if (json[0] == '"' && json[json.Length - 1] == '"')
            {
                string str = json.Substring(1, json.Length - 2);
                return str.Replace("\\", string.Empty);
            }
            if (char.IsDigit(json[0]) || json[0] == '-')
            {
                if (json.Contains("."))
                {
                    _ = double.TryParse(json, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double result);
                    return result;
                }
                else
                {
                    _ = int.TryParse(json, out int result);
                    return result;
                }
            }
            if (json == "true")
            {
                return true;
            }

            if (json == "false")
            {
                return false;
            }
            // handles json == "null" as well as invalid JSON
            return null;
        }

        static object ParseObject(Type type, string json)
        {
            object instance = FormatterServices.GetUninitializedObject(type);

            //The list is split into key/value pairs only, this means the split must be divisible by 2 to be valid JSON
            List<string> elems = Split(json);
            if (elems.Count % 2 != 0)
            {
                return instance;
            }

            if (!fieldInfoCache.TryGetValue(type, out Dictionary<string, FieldInfo> nameToField))
            {
                nameToField = type.GetFields().Where(field => field.IsPublic).ToDictionary(field => field.Name);
                fieldInfoCache.Add(type, nameToField);
            }
            if (!propertyInfoCache.TryGetValue(type, out Dictionary<string, PropertyInfo> nameToProperty))
            {
                nameToProperty = type.GetProperties().ToDictionary(p => p.Name);
                propertyInfoCache.Add(type, nameToProperty);
            }

            for (int i = 0; i < elems.Count; i += 2)
            {
                if (elems[i].Length <= 2)
                {
                    continue;
                }

                string key = elems[i].Substring(1, elems[i].Length - 2);
                string value = elems[i + 1];

                if (nameToField.TryGetValue(key, out FieldInfo fieldInfo))
                {
                    fieldInfo.SetValue(instance, ParseValue(fieldInfo.FieldType, value));
                }
                else if (nameToProperty.TryGetValue(key, out PropertyInfo propertyInfo))
                {
                    propertyInfo.SetValue(instance, ParseValue(propertyInfo.PropertyType, value), null);
                }
            }

            return instance;
        }
    }
}
