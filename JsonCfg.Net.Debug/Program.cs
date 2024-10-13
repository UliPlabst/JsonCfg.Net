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