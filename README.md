<h1 align="center">Loveable Saves</h1>
<br>
<p align="center">
  <em>Loveable Saves is a library meant to facilitate the saving/loading of
    <br> objects into save files using the [Saveable] decorator.</em>
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
  <b> Requires <a href="https://www.newtonsoft.com/json">Json.NET</a> & <a href="https://unity.com/">Unity</a> </b>

  <br>
</p>
<h1 align="center">Documentation</h1>
<br>

### `[Saveable]`

    [Saveable] public string foo = "Works with fields";
    [Saveable] private MyClass bar = new MyClass("...Even if they use one of your classes!");
    
    public class MyClass{
        [Saveable] private string baz = "But only if you want them saved!";
        private string qux = "(I'm not saved...)";
        
        public MyClass(string baz){this.baz = baz}
    }
    
  <p align="center">Declares whether a field should be saved or not.</p>
  <br>
<h2 align="center"></h2>


### `(ISave).ToSave()` : `string[]`

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
    
  <p align="center">Add fields to be included in save file.<br>(Do not use unless you cannot edit the inherited class to include the [Saveable] decorator)</p>
  <br>
<h2 align="center"></h2>


### `Saveable.Save(object, path)` : `void`
    (object)      [object]: Object to be saved.
    (string)      [path]: Path of the JSON file to overwrite/create with object's save data.
    
  <p align="center">Saves an object's data to a JSON file.</p>
  <br>
<h2 align="center"></h2>


### `Saveable.Load(object, file)` : `void`
    (object)      [object]: Object to be overwritten by file's save data.
    (string)      [path]: File containing save data.
    
  <p align="center">Loads a save file's data.</p>
  <br>
<h2 align="center"></h2>


