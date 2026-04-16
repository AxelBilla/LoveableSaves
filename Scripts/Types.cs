using UnityEngine;


namespace LoveableSaves {
    public static class Types {
        public static void Set(){

            Saveable.Implementation.Set<Vector3>(
                    (object value)=>{
                        string formated_value = value.ToString();
                        formated_value = Saveable.Sanitize.Replace(formated_value, @"\(", @"[");
                        formated_value = Saveable.Sanitize.Replace(formated_value, @"\)", @"]");
                        return formated_value;
                    },
                    (string value)=>{
                        value = value.Trim('[', ']');
                        string[] a = value.Split(',');
                        return new Vector3(float.Parse(a[0]), float.Parse(a[1]), float.Parse(a[2]));
                    }
            );

            Saveable.Implementation.Set<Vector2>(
                    (object value)=>{
                        string formated_value = value.ToString();
                        formated_value = Saveable.Sanitize.Replace(formated_value, @"\(", @"[");
                        formated_value = Saveable.Sanitize.Replace(formated_value, @"\)", @"]");
                        return formated_value;
                    },
                    (string value)=>{
                        value = value.Trim('[', ']');
                        string[] a = value.Split(',');
                        return new Vector2(float.Parse(a[0]), float.Parse(a[1]));
                    }
            );


        }
    }
}
