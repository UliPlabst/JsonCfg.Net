# JsonCfg.Net - Parse JSON preserving comments and custom formatting
JsonCfg.Net is a JSON serialization and deserialization library that aims to preserve comments and custom formatting (to a useful extent).  
It is very similar to [Json.Net](https://github.com/JamesNK/Newtonsoft.Json) but adds the following features:
- Proper comment handling. Comments are parsed into JsonNodes and can be roundtripped to the serialization. 
- Formatting hints can be generated during and attached to the resulting nodes during deserialization. For example an Array formatted inline as [1,2,3] will have FormattingHint.Inline and will then be serialized as inline array if possible without breaking syntax. FormattingHints can also be set after deserialization on the nodes manually. 
- Trailing commas will be automatically preserved  

I created this library mostly to be able to parse JSON configs, manipulate them in code (like in a config migration) and then serialize again **without losing comments or most of the custom formatting**. JSON as a configuration format is very suitable for this use-case as it has less amibguity in it's grammar than formats like TOML or YAML where objects can be written in a lot of different syntaxes.

The use-case for this library is **not** high performance serialization/deserialization.  
JsonCfg.Net aims to preserve custom formatting that makes sense but does not aim to handle all cases.
For example an object may be formatted as these two options:  
1) inline 
```json
{ "hello": "world", "hello2": "world2", "hello3": "world3" }
```
2) multiline
```json
{
  "hello": "world",
  "hello2": "world2",
  "hello3": "world3",
}
```
while other options like (mixed inline and multiline)
```json
{ "hello": "world", "hello2": "world2", 
  "hello3": "world3" 
}
```
have little value in supporting in my opinion.  
The same goes for comments. JsonCfg.Net supports inline comments using `/* comment */` and line comments using `//comment` but there are some current limitations to the formatting when it comes to inline comments and where you put them such that it is not guaranteed to always get a similar serialization to the original.

# Getting started
We are using this library in production but the development stage is really early as I put this library together in a couple of hours and right now there was not a lot of testing so **use at your own risk**.
```cs
using JsonCfgNet;

using JsonCfgNet;

var json = @"{
 //Comment
 ""hello"": ""world"", //I am a comment
 ""test"": /*i am an inline comment */ true,
}";

var node = JsonNode.Parse(json);
var jo = (JsonObject)node;
jo["value"] = JsonValue.FromObject(123);
var oldTest = jo["test"];
jo["test"] = JsonValue.FromObject(false);
jo["test"].Trivia = oldTest.Trivia; //copy the old comments to the new node

var newObject = new JsonObject();
newObject["prop"] = JsonValue.FromObject(true);
newObject.FormattingHint = FormattingHint.Inline;
jo["new"] = newObject;

var serializerSettings = new SerializerSettings
{
  WriteComments = true,
  Indent = "  ",
  WriteTrailingCommas = false,
  SpaceToInlineComments = 1,
  SpaceToLineComments = 1,
  SpacedBraces = true,
};
json = Serializer.Serialize(jo, serializerSettings);
Console.WriteLine(json);
// Prints:
// {
//   //Comment
//   "hello": "world", //I am a comment
//   "test": /*i am an inline comment */ false,
//   "value": 123,
//   "new": {
//     "prop": true
//   }
// }
```

# TODO
- There should be a formatting hint to format numbers as exponential

# Bind to POCOs
Converting to POCOs is not the goal of this library, use Json.Net or System.Text.Json.
