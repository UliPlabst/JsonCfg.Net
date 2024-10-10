// See https://aka.ms/new-console-template for more information
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
    SpaceToInlineComments = 2,
    SpacedBraces = true,
};
json = Serializer.Serialize(jo, serializerSettings);
Console.WriteLine(json);
