using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LoveableSaves{

    public static class JSON{
        public static JObject Get(string file){
            return Get<JObject>(file);
        }
        public static T Get<T>(string file){
            return Deserialize<T>(file);
        }
        public static JObject Get(string file, string key){
            JObject data = Get(file);
            if(data == null) return data;
            if(data.ContainsKey(key)) {
                JObject value = (Get(data.GetValue(key).ToString()));
                if(value==null) {
                    JToken res = data.GetValue(key);
                    value = new JObject();
                    value.Add(JSON.Values.SINGLE, res);
                }
                return value;
            }
            else return data;
        }
        public static JObject Get(JObject entry, string key){
            return Get(entry.ToString(), key);
        }
        public static T Deserialize<T>(string file) {
            try {
                return JsonConvert.DeserializeObject<T>(file);
            } catch{
                return default;
            }
        }
        public static object Deserialize(string file, Type type) {
            try {
                return JsonConvert.DeserializeObject(file, type);
            } catch{
                return default;
            }
        }
    
        public static class Values{
            public static string SINGLE = "[SINGLE]";
        }
    }
    
}
