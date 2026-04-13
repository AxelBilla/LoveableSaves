using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEngine;


namespace LoveableSaves {

    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public class Saveable : Attribute {
        public Saveable() {
        }

        private static List<FieldInfo> GetSaved(Type type) {
            List<FieldInfo> saved_fields = new List<FieldInfo>();
            do{
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                foreach (FieldInfo field in fields) {
                    Saveable isSaved = (Saveable)Attribute.GetCustomAttribute(field, typeof(Saveable));
                    if (isSaved != null) saved_fields.Add(field);
                }
                type = type.BaseType;
            } while (type != null);
            return saved_fields;
        }

        private static string Serialize(object obj, params string[] additional_fields) {
            Type type = obj.GetType();
            List<FieldInfo> saved_fields = GetSaved(type);

            string file = "";

            if (additional_fields.Length == 0 && obj is ISave) additional_fields = ((ISave)obj).ToSave();
            foreach (string field_name in additional_fields) {
                (MemberInfo info, object obj) member = TryGetFullNameMember(obj, field_name);
                string formated_value = Sanitize.Field(member.info.GetValue(member.obj));

                file += Wrap(field_name) + " : " + formated_value + ((field_name == additional_fields[additional_fields.Length - 1] && saved_fields.Count == 0) ? "\n": ",\n");
            }
            foreach (FieldInfo field in saved_fields) {
                object field_value = field.GetValue(obj);
                string formated_value = Sanitize.Field(field_value);

                file += Wrap(field.Name) + " : " + formated_value + ((field != saved_fields[saved_fields.Count - 1]) ? ",\n": "\n");

            }


            return (file != "") ? Wrap(file, "{\n", "}") : file;
        }

        public static void Save(object obj, string path) {
            string serialized_object = Serialize(obj);
            ToFile(path, serialized_object, "json");
        }

        public static void Load(object obj, string file) {
            Type type = obj.GetType();

            JObject saved_fields_file = JSON.Get(file);
            Dictionary<string, FieldInfo> saved_fields = new Dictionary<string, FieldInfo>();

            do{
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                foreach (FieldInfo field in fields) {
                    if (saved_fields_file.ContainsKey(field.Name)) {
                        // "private protected" fields technically creates a new field of the same name, so we have to ignore them.
                        if (!saved_fields.ContainsKey(field.Name)) saved_fields.Add(field.Name, field);
                    }
                }
                type = type.BaseType;
            } while (type != null);

            foreach (KeyValuePair<string, JToken> field in saved_fields_file) {
                if (saved_fields.ContainsKey(field.Key)) SetFields(obj, file, saved_fields[field.Key]);
                else TrySetFullNameMember(obj, field.Key, field.Value.ToString());
            }
        }

        private static void SetFields(object obj, JObject content, FieldInfo field) {
            SetFields(obj, content.ToString(), field);
        }

        private static void SetFields(object obj, string content, FieldInfo field) {
            JObject file = JSON.Get(content, field.Name);
            if (file != null && !file.ContainsKey(JSON.Values.SINGLE)) {
                foreach (KeyValuePair<string, JToken> v in file) {
                    FieldInfo sub_field = field.FieldType.GetField(v.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                    if (sub_field != null) SetFields(field.GetValue(obj), v.Value.ToString(), sub_field);
                    else TrySetFullNameMember(field.GetValue(obj), v.Key, v.Value.ToString());
                }
            }
            else {
                if (file != null && file.ContainsKey(JSON.Values.SINGLE)) content = file.GetValue(JSON.Values.SINGLE).ToString();
                SetField(obj, field, ConvertValue(field, content));
            }
        }

        private static void TrySetFullNameMember(object obj, string field_name, string value) {
            (MemberInfo info, object obj) member = TryGetFullNameMember(obj, field_name);
            member.info.SetValue(member.obj, ConvertValue(member.info, value));
        }

        private static (MemberInfo, object) TryGetFullNameMember(object obj, string field_name) {
            field_name = field_name.Trim();

            string[] field_members = field_name.Split('.');
            int i = (field_members[0] == "this") ? 1 : 0;

            MemberInfo member = obj.GetType().GetMember(field_members[i])[0];

            if (field_members.Length > 1) {
                for (i += 1; i < field_members.Length; i++) {
                    obj = member.GetValue(obj);
                    try {
                        member = obj.GetType().GetMember(field_members[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)[0];
                    } catch {
                        return (null, null);
                    }
                }
            }

            return (member, obj);
        }

        private static void SetField(object obj, FieldInfo field, object value) {
            field.SetValue(obj, value);
        }

        private static object ConvertValue(MemberInfo member, string value) {

            Type member_type = member.ResolveMemberType();

            if (value == "" || value == "[]" || value == "{}") return default;
            else if (value == "NULL") return null;
            else {
                if (member_type.IsGenericType && (member_type.GetGenericTypeDefinition() == typeof(List<>))) {
                    List<object> content_arr = new List<object>();
                    if (member_type.GetGenericArguments()[0] == typeof(string)) {
                        List<string> arr = JSON.Get<List<string>>(value);
                        foreach (string item in arr) {
                            content_arr.Add(item);
                        }
                    }
                    else {
                        List<JObject> items = JSON.Get<List<JObject>>(value);
                        foreach (JObject item in items) {
                            object i = JSON.Deserialize(item.ToString(), member_type.GetGenericArguments()[0]);
                            content_arr.Add(i);
                        }
                    }
                    return ConvertList(content_arr, member_type);
                }
                else if (member_type.IsEnum) {
                    return Enum.Parse(member_type, value);
                }
                else if (member_type.Namespace == typeof(UnityEngine.Object).Namespace) {
                    return ConvertToUnityObject(value, member_type);
                }
                else {
                    return Convert.ChangeType(value, member_type);
                }
            }
        }

        private static object ConvertToUnityObject(string value, Type type) {
            switch (type.Name) {
                case nameof(Vector3):
                    value = value.Trim('[', ']');
                    string[] a = value.Split(',');
                    return new Vector3(float.Parse(a[0]), float.Parse(a[1]), float.Parse(a[2]));
            }
            return null;
        }

        private static object ConvertList(List<object> value, Type type) {
            IList list = (IList)Activator.CreateInstance(type);
            foreach (var item in value) {
                list.Add(item);
            }
            return list;
        }

        private static string Wrap(string text, string wrapper = "\"") {
            if (wrapper == "`") {
                text = Sanitize.Escape(text);
                text = Sanitize.Newline(text);
                wrapper = "\"";
            }

            return Wrap(text, wrapper, wrapper);
        }

        private static string Wrap(string text, string first, string second) {
            if (Regex.IsMatch(text, @"^(\{|\[)")) return text;
            if (first == second) text = Sanitize.Replace(text, first, @"\" + first);

            return first + text + second;
        }

        private static class Sanitize {
            public static string Field(object content) {
                string formated_value;

                if (content == null) {
                    formated_value = "null";
                }
                else {
                    switch (content) {
                        case Vector3 _:
                            formated_value = content.ToString();
                            formated_value = Sanitize.Replace(formated_value, @"\(", @"[");
                            formated_value = Sanitize.Replace(formated_value, @"\)", @"]");
                            break;

                        case string _:
                            formated_value = Wrap(content.ToString(), "`");
                            break;

                        case IList _:
                            formated_value = "[";
                            IList listed_values = (IList)content;
                            for (int i = 0; i < listed_values.Count; i++) {
                                string list_string = Serialize(listed_values[i]);

                                if (list_string != "") list_string = Wrap(list_string);
                                else list_string = Wrap(listed_values[i].ToString());

                                formated_value += list_string;
                                if (i < listed_values.Count - 1) formated_value += "," + ((list_string.Contains("\n")) ? "\n": "");
                            }
                            formated_value += "]";
                            break;

                        default:
                            formated_value = Serialize(content);
                            if (formated_value != "") formated_value = Wrap(formated_value);
                            else formated_value = Wrap(content.ToString());
                            break;
                    }
                }

                return formated_value;
            }

            public static string Newline(string text) {
                return Replace(text, "\n", "\\n");
            }

            public static string Escape(string text) {
                return Replace(text, @"(?<!\\)\\(?!\\)", @"\\");
            }

            public static string Replace(string text, string to_replace, string replace_with) {
                return Regex.Replace(text, to_replace, replace_with);
            }
        }


        private static string ToFile(string path, string file, string extension = "") {
            if (extension != "") path += "." + extension;
            if (!System.IO.File.Exists(path)) System.IO.File.Create(path).Close();

            StreamWriter file_stream = System.IO.File.CreateText(path);
            file_stream.Write(file);
            file_stream.Close();

            return path;
        }
    }

}
