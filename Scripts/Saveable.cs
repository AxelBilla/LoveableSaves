using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;


namespace LoveableSaves {

    [AttributeUsage(AttributeTargets.All, Inherited = true)]
    public class Saveable : Attribute {
        public Saveable() {
        }

        private static List<MemberInfo> GetSaved(Type type) {
            List<MemberInfo> saved_fields = new List<MemberInfo>();
            do{
                PropertyInfo[] props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                foreach (PropertyInfo prop in props) {
                    Saveable isSaved = (Saveable)Attribute.GetCustomAttribute(prop, typeof(Saveable));
                    if (isSaved != null) saved_fields.Add(prop);
                }
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
            List<MemberInfo> saved_fields = GetSaved(type);

            string file = "";

            if (additional_fields.Length == 0 && obj is ISave) additional_fields = ((ISave)obj).ToSave();
            foreach (string field_name in additional_fields) {
                try {
                    (MemberInfo info, object obj) member = TryGetFullNameMember(obj, field_name);
                    string formated_value = Sanitize.Field(member.info.GetValue(member.obj));

                    file += Wrap(field_name) + " : " + formated_value + ((field_name == additional_fields[additional_fields.Length - 1] && saved_fields.Count == 0) ? "\n": ",\n");
                } catch {
                    throw Errors.InvalidField(field_name, obj);
                }
            }
            foreach (MemberInfo field in saved_fields) {
                try {
                    object field_value = field.GetValue(obj);
                    string formated_value = Sanitize.Field(field_value);

                    file += Wrap(field.Name) + " : " + formated_value + ((field != saved_fields[saved_fields.Count - 1]) ? ",\n": "\n");
                } catch {
                    throw Errors.InvalidField(field.Name, obj);
                }
            }

            return (file != "") ? Wrap(file, "{\n", "}") : obj?.ToString();
        }

        public static void Save(object obj, string path) {
            string serialized_object = Serialize(obj);
            ToFile(path, serialized_object, "json");
        }

        public static string Save(object obj) {
            return Serialize(obj);
        }

        public static void Load(object obj, string file) {
            Type type = obj.GetType();

            JObject saved_fields_file = JSON.Get(file);
            Dictionary<string, MemberInfo> saved_fields = new Dictionary<string, MemberInfo>();

            do{
                PropertyInfo[] props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                foreach (PropertyInfo prop in props) {
                    if (saved_fields_file.ContainsKey(prop.Name)) {
                        // "private protected" fields technically create a new field of the same name, so we have to ignore them.
                        if (!saved_fields.ContainsKey(prop.Name)) saved_fields.Add(prop.Name, prop);
                    }
                }
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                foreach (FieldInfo field in fields) {
                    if (saved_fields_file.ContainsKey(field.Name)) {
                        // "private protected" fields technically create a new field of the same name, so we have to ignore them.
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

        private static void SetFields(object obj, JObject content, MemberInfo field) {
            SetFields(obj, content.ToString(), field);
        }

        private static void SetFields(object obj, string content, MemberInfo field) {
            JObject file = JSON.Get(content, field.Name);

            if (file != null && !file.ContainsKey(JSON.Values.SINGLE) && !(field.ResolveMemberType().IsGenericType && typeof(IDictionary).IsAssignableFrom(field.ResolveMemberType().GetGenericTypeDefinition())) ) {
                foreach (KeyValuePair<string, JToken> v in file) {
                    FieldInfo sub_field = field.ResolveMemberType().GetField(v.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

                    if (sub_field != null) SetFields(field.GetValue(obj), v.Value.ToString(), sub_field);
                    else {
                        PropertyInfo sub_prop = field.ResolveMemberType().GetProperty(v.Key, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                        if (sub_prop != null) SetFields(field.GetValue(obj), v.Value.ToString(), sub_prop);
                        else TrySetFullNameMember(field.GetValue(obj), v.Key, v.Value.ToString());
                    }
                }
            }
            else {

                if (file != null && file.ContainsKey(JSON.Values.SINGLE)) content = file.GetValue(JSON.Values.SINGLE).ToString();
                SetField(obj, field, ConvertValue(field, content));
            }
        }

        private static void TrySetFullNameMember(object obj, string field_name, string value) {
            (MemberInfo info, object obj) member = TryGetFullNameMember(obj, field_name);

            if (member.info == null) throw Errors.MissingField(field_name);
            else {
                try {
                    member.info.SetValue(member.obj, ConvertValue(member.info, value));
                } catch {
                    throw Errors.InvalidValue(member.info, value);
                }
            }
        }

        private static (MemberInfo, object) TryGetFullNameMember(object obj, string field_name) {
            field_name = field_name.Trim();

            string[] field_members = field_name.Split('.');
            int i = (field_members[0] == "this") ? 1 : 0;

            MemberInfo member = null;
            try {
                member = obj.GetType().GetMember(field_members[i])[0];
            } catch {
                throw Errors.InvalidField(field_name, obj);
            }

            if (field_members.Length > 1 && member!=null) {
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

        private static void SetField(object obj, MemberInfo field, object value) {
            try {
                field.SetValue(obj, value);
            } catch {
                throw new Exception("Invalid Value, Field \"" + field.Name + "\" expected a value of type <" + field.ResolveMemberType().Name + ">, but received \"" + value + "\"");
            }
        }

        private static object ConvertValue(MemberInfo member, string value) {

            Type member_type = member.ResolveMemberType();

            if (value == "" || value == "[]" || value == "{}") return default;
            else if (value == "NULL") return null;
            else {
                try {
                    if (Implementation.Has(member_type)) {
                        return Implementation.Deserialize(member_type, value);
                    }
                    else if (member_type.IsGenericType && typeof(IList).IsAssignableFrom(member_type.GetGenericTypeDefinition())) {
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
                    else if (member_type.IsGenericType && (typeof(IDictionary).IsAssignableFrom(member_type.GetGenericTypeDefinition())) ) {
                        Dictionary<object, object> content_dict = new Dictionary<object, object>();
                        JObject items = JSON.Get<JObject>(value);

                        Type key_type = member_type.GetGenericArguments()[0];
                        Type value_type = member_type.GetGenericArguments()[1];

                        foreach(var item in items) {

                            object deserialized_key = (key_type == typeof(string)) ? item.Key.ToString() : Create(key_type, item.Key.ToString());
                            object deserialized_value = (value_type == typeof(string)) ? item.Value.ToString() : Create(value_type, item.Value.ToString());


                            if(!content_dict.ContainsKey(deserialized_key)) content_dict.Add(deserialized_key, deserialized_value);
                            else throw Errors.InvalidDuplicateKey(item.Key.ToString(), member.Name);

                        }

                        return ConvertDict(content_dict, member_type);
                    }
                    else if (member_type == typeof(Type)) {
                        return Type.GetType(value);
                    }
                    else if (member_type.IsEnum) {
                        return Enum.Parse(member_type, value);
                    }
                    else {
                        return Convert.ChangeType(value, member_type);
                    }

                } catch {
                    throw Errors.InvalidValue(member, value);
                }
            }
        }

        private static object ConvertList(List<object> values, Type type) {
            IList list = (IList)Activator.CreateInstance(type);
            foreach (var item in values) {
                list.Add(ConvertValue(type.GetGenericArguments()[0], values.ToString()));
            }
            return list;
        }
        private static object ConvertDict(IDictionary values, Type type) {
            IDictionary list = (IDictionary)Activator.CreateInstance(type);
            foreach (var key in values.Keys) {
                list.Add(key, values[key]);
            }

            return list;
        }

        private static object Create(Type type, string content){

            if(GetSaved(type).Count<=0) return ConvertValue(type, content);

                object obj = Activator.CreateInstance(type);
                Load(obj, content);
                return obj;

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

        public static class Sanitize {
            public static string Field(object content) {
                string formated_value;

                if (content == null) {
                    formated_value = "null";
                }
                else if (Implementation.Has(content.GetType())) {
                    return Implementation.Serialize(content.GetType(), content);
                }
                else {
                    switch (content) {
                        case string _:
                            formated_value = Wrap(content.ToString(), "`");
                            break;

                        case Type _:
                            formated_value = ((Type)content).AssemblyQualifiedName;
                            if (formated_value != "") formated_value = Wrap(formated_value);
                            else formated_value = Wrap(content.ToString());
                            break;

                        case IDictionary _:
                            formated_value = "{\n";
                            IDictionary dict_values = (IDictionary)content;
                            int y = 0;
                            foreach (var k in dict_values.Keys) {
                                string dict_string = Wrap(Serialize(k).ToString())+":"+Wrap(Serialize(dict_values[k]).ToString());

                                formated_value += dict_string;
                                if (y < dict_values.Count - 1) formated_value += "," + ((dict_string.Contains("\n")) ? "\n": "");
                                y++;
                            }
                            formated_value += "}";
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

            try {
                if (!System.IO.File.Exists(path)) System.IO.File.Create(path).Close();

                StreamWriter file_stream = System.IO.File.CreateText(path);
                file_stream.Write(file);
                file_stream.Close();

            } catch {
                throw Errors.CouldNotWriteToFile(path);
            }

            return path;
        }


        public static class Implementation {
            private static Dictionary<Type, (Func<object, string> serialize, Func<string, object> deserialize)> CustomTypeImplementation = new Dictionary<Type, (Func<object, string>, Func<string, object>)>();

            public static void Set<T>(Func<object, string> serialization, Func<string, object> deserialization) {
                CustomTypeImplementation[typeof(T)] = (serialization, deserialization);
            }

            public static (Func<object, string> serialize, Func<string, object> deserialize) Get(Type type) {
                if (CustomTypeImplementation.Count <= 0) LoveableSaves.Types.Set();

                return CustomTypeImplementation[type];
            }

            public static bool Has(Type type) {
                if (CustomTypeImplementation.Count <= 0) LoveableSaves.Types.Set();

                return CustomTypeImplementation.ContainsKey(type);
            }

            public static string Serialize(Type type, object value) {
                return Get(type).serialize(value);
            }

            public static object Deserialize(Type type, string value) {
                return Get(type).deserialize(value);
            }
        }

        private static class Errors {
            public static Exception InvalidValue(MemberInfo member, string value) {
                return new Exception("Invalid Value, field \"" + member.Name + "\" expected a value of type <" + member.ResolveMemberType().Name + ">, but received \"" + value + "\"");
            }

            public static Exception MissingField(string field_name) {
                return new Exception("Missing Field, field \"" + field_name + "\" could not be found");
            }

            public static Exception CouldNotWriteToFile(string file_path){
                return new Exception("Could Not Write to File, file at \"" + file_path + "\" could not be overwritten");

            }

            public static Exception InvalidField(string field_name, object obj){
                return new Exception("Invalid Field, field \"" + field_name+ "\" could not be found within \""+obj+"\"");
            }

            public static Exception InvalidDuplicateKey(string key_name, object obj){
                return new Exception("Invalid Duplicate, key \"" + key_name+ "\" already exists in \""+obj+"\"");
            }

            public static Exception MissingDefaultConstructor(string type_name){
                return new Exception("Missing Default Constructor, \""+type_name+"\" is missing a default constructor.");
            }
        }
    }

}


