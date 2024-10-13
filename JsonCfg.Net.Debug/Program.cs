// See https://aka.ms/new-console-template for more information
using JsonCfgNet;

var json = @"{
    // Comment at the start
    ""person"": {
        ""name"": ""Alice"",
        ""age"": 25 /* Inline age comment */,
        ""hobbies"": [  ""reading"" /*asd*/ , /*asd*/ ""hiking"" ]  // Hobbies listed here
    },
    ""meta"": { /* Metadata block */
        /*preprop*/ ""created"" /*postprop*/: /*preval*/ ""2024-10-01"" /*postval*/, // Creation date
        ""updated"": ""2024-10-12""  // Last updated date
    }
}";

json = File.ReadAllText("D:/Projects/primus/resources/config.json");

var node = JsonNode.Parse(json);
// var jo = (JsonObject)node;

// jo["meta"].AsObject()["author"] = JsonValue.FromObject("Tester");  // Add new metadata

var serializerSettings = new SerializerSettings
{
    WriteComments = true,
    Indent = "  ",
    WriteTrailingCommas = false,
    SpaceToInlineComments = 2,
    SpacedBraces = true,
};

var serializedJson = Serializer.Serialize(node, serializerSettings);
Console.WriteLine(serializedJson);