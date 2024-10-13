# JsonCfg.Net
JsonCfg.Net is a Library to enable proper usage of JSON as configuration file format. 
I created this library mostly to be able to parse JSON configs, manipulate them in code (like a migration) and then serialize again **without losing comments or most of the custom formatting**.
It is very similar to [Json.Net](https://github.com/JamesNK/Newtonsoft.Json) but adds the following features:
- Proper comment handling. Comments are parsed into JsonNodes and can be roundtripped to the serialization. 
- Formatting hints can be generated during and attached to the resulting nodes during deserialization. For example an Array formatted inline as [1,2,3] will have FormattingHint.Inline and will then be serialized as inline array if possible without breaking syntax. FormattingHints can also be set after deserialization on the nodes manually. 
- Trailing commas will be automatically preserved

The use-case for this library is **not** high performance serialization/deserialization.

# Getting started
We are using this library in production but the development stage is really early as I put this library together in a couple of hours and right now there was not a lot of testing so **use at your own risk**.
```cs
using JsonCfgNet;

var json = @"{
 //Comment
 ""hello"": ""world"" //I am a comment
}";

var node = JsonNode.Parse(json);
var jo = (JsonObject)node;
jo["test"] = JsonValue.FromObject(123);

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
// Prints
// {
//   //Comment
//   "hello": "world",  //I am a comment
//   "test": 123,
//   "new": { "prop": true }
// }
```

# TODO
- Add test cases
- There should be a formatting hint to format numbers as exponential

# Bind to POCOs
Converting to POCOs is not the goal of this library. 
To convert to POCOs you can use these conversion functions to convert to Json.Net JToken and then convert these to POCOs. 
As I said, the goal of this library is for usage with JSON configs and the assumption is that these do not have to be parsed all the time so the additional cycles to convert to JTokens do not matter.
```cs
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class JsonCfgNetExtensions
{
  public static JObject ToJObject(this JsonObject obj)
  {
      var res = new JObject();
      foreach(var prop in obj.Properties)
      {
          res[prop.Key] = ToJToken(prop.Value);
      }
      return res;
  }
  
  private static JArray ToJArray(JsonArray array)
  {
      var res = new JArray();
      foreach(var val in array.Items)
      {
          res.Add(ToJToken(val));
      }
      return res;
  }
  
  private static JToken ToJToken(JsonNode? value)
      => value switch {
          null => JValue.CreateNull(),
          JsonObject table => ToJObject(table),
          JsonArray array => ToJArray(array),  
          JsonValue val => JToken.Parse(val.StringValue),
          JsonProperty => throw new InvalidOperationException("Lone property cannot be converted"),
          JsonComment => throw new InvalidOperationException("Comment cannot be converted"),
          _ => throw new InvalidOperationException($"Unknown node type {value.GetType()}")
      };
        
  public static JsonNode ToJsonNode(object any)
  {
      var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(
        any, 
        Newtonsoft.Json.Formatting.Indented
      );
      return JsonNode.Parse(serialized);
  }
}
```
