<h1 align="center">Loveable Saves</h1>
<br>
<p align="center">
  <em>Loveable Saves is a library meant to facilitate the saving/loading of objects,
    <br>into save files, using the [Saveable] decorator.</em>
  <br>
  <br>
  <a href="https://github.com/AxelBilla/Loveable-Saves/releases/"><img src="https://img.shields.io/github/release/AxelBilla/Loveable-Saves?include_prereleases=&sort=semver&color=blue" alt="GitHub release"></a>
  <a href="#license"><img src="https://img.shields.io/badge/License-CC_BY--SA_4.0-blue" alt="License"></a>
  <a href="https://github.com/AxelBilla/Loveable-Saves/issues"><img src="https://img.shields.io/github/issues/AxelBilla/Loveable-Saves" alt="issues - Loveable Save"></a>
  <br>
  <br>
  <br>
  <br>
  <br>
  <b> Requires <a href="https://www.newtonsoft.com/json">Json.NET</a></b>

  <br>
</p>
<h1 align="center">Documentation</h1>
<br>
<h1 align="center">HOW TO USE</h1>
<br>

## `[Saveable]`

    [Saveable] public string foo = "Works with fields";
    [Saveable] private MyClass bar = new MyClass("...Even if they use one of your classes!");
    
    public class MyClass{
        [Saveable] private string baz = "But only if you want them saved!";
        private string qux = "(I'm not saved...)";
        
        public MyClass(string baz){this.baz = baz}
    }
    
  
  > [!NOTE]
  > Declares whether a field should be saved or not.
  <br>


## `(ISave).ToSave()` : `string[]`

    public class MyClassWithInaccessibleInheritance : InaccessibleClass, ISave {
        private string foo = "Because sometimes, you just can't edit its parent.";
        
        public string[] ToSave(){
            return new string[]{
                "this.bar.baz",
                "qux"
            };
        }
    }
    
    public class InaccessibleClass{
        private Inner bar;
        public class Inner{
            private string baz = "Needs to be the full path!";
        }
        private string qux = "But you can omit 'this' if you want!";
    }
    
  
  > [!TIP]
  > Add fields to be included in save file.<br>(Do not use unless you cannot edit the inherited class to include the [Saveable] decorator)
  <br>


## `Saveable.Save(object, path)` : `void`
    (object)      [object]: Object to be saved.
    (string)      [path]: Path of the JSON file to overwrite/create with object's save data.
    
  
  > [!NOTE]
  > Saves an object's data to a JSON file.
  <br>


## `Saveable.Save(object)` : `string`
    (object)      [object]: Object to be saved.
    
  
  > [!NOTE]
  > Saves an object's data and returns it as a string.
  <br>


## `Saveable.Load(object, file)` : `void`
    (object)      [object]: Object to be overwritten by file's save data.
    (string)      [path]: File containing save data.
    
  
  > [!NOTE]
  > Loads a save file's data.
  <br>

<br>
<h1 align="center">HOW TO ADD A TYPE</h1>
<br>

## `Types.Set()` : `void`
    public static class Types {
        public static void Set(){

            Saveable.Implementation.Set<Foo>(
                (object value)=>{
                    return value.ToString();
                },
                (string value)=>{
                  return Foo.ConvenientDeserializationMethod(value);
                }
            );

            Saveable.Implementation.Set<Bar>(..., ...);
            Saveable.Implementation.Set<Baz>(..., ...);
            Saveable.Implementation.Set<Qux>(..., ...);

        }
    }
  
  > [!TIP]
  > Populates the type implementation data set, it is greatly recommended to isolate all (Saveable.Implementation)-related methods under this.
  <br>

## `Saveable.Implementation.Set<TYPE>(serialization, deserialization)` : `void`
    (Func<object, string>)   [serialization]: Function defining how to serialize an [object] of TYPE, and return its result as a [string].
    (Func<string, object>)   [deserialization]: Function defining how to deserialize a [string], and return its result as an [instance of TYPE].
    
  
  > [!NOTE]
  > Sets an implementation for a given type, by defining how to serialize/deserialize it.<br>(This can be used with supported types to alter their implementation, or to add support for unsupported types like Unity's Vector2.)
  <br>
<h2 align="center"></h2>


