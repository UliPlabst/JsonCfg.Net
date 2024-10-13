namespace JsonCfgNet.Tests;

using NUnit.Framework;

[TestFixture]
public class SerializerTests
{
    // This method will be implemented by the user to validate the JSON structure
    private bool IsJsonValid(string json)
    {
        try
        {
            Newtonsoft.Json.Linq.JToken.Parse(json);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    [Test]
    public void TestSimpleJsonWithComments()
    {
        var json = @"{
          // This is a comment
          ""name"": ""John"",
          ""age"": 30,  // Inline comment here
          ""city"": ""New York""
        }";

        var node = JsonNode.Parse(json);
        var jo = (JsonObject)node;

        jo["country"] = JsonValue.FromObject("USA");  // Add a new field

        var serializerSettings = new SerializerSettings
        {
            WriteComments = true,
            Indent = "  ",
            WriteTrailingCommas = false,
            SpaceToInlineComments = 2,
            SpacedBraces = true,
        };

        var serializedJson = Serializer.Serialize(jo, serializerSettings);
        Assert.IsTrue(IsJsonValid(serializedJson));

        Console.WriteLine(serializedJson);
    }

    [Test]
    public void TestJsonWithInlineObjectFormatting()
    {
        var json = @"{
          ""config"": {
            ""enable"": true, /* Inline comment */
            ""timeout"": 1000  // Timeout value in ms
          },
          ""settings"": { /* Inline settings */
            ""version"": ""1.0.0"",
            ""debug"": false
          }
        }";

        var node = JsonNode.Parse(json);
        var jo = (JsonObject)node;

        jo["settings"].AsObject()["debug"] = JsonValue.FromObject(true);  // Change value

        var serializerSettings = new SerializerSettings
        {
            WriteComments = true,
            Indent = "  ",
            WriteTrailingCommas = false,
            SpaceToInlineComments = 2,
            SpacedBraces = true,
        };

        var serializedJson = Serializer.Serialize(jo, serializerSettings);
        Assert.IsTrue(IsJsonValid(serializedJson));

        Console.WriteLine(serializedJson);
    }

    [Test]
    public void TestJsonWithArrayAndComments()
    {
        var json = @"{
          ""items"": [
            1,  // First item
            2,  // Second item
            3   /* Third item */
          ],
          ""active"": true // Active status
        }";

        var node = JsonNode.Parse(json);
        var jo = (JsonObject)node;

        var arrayNode = jo["items"].AsArray();
        arrayNode.Add(JsonValue.FromObject(4));  // Add a new item

        var serializerSettings = new SerializerSettings
        {
            WriteComments = true,
            Indent = "  ",
            WriteTrailingCommas = false,
            SpaceToInlineComments = 2,
            SpacedBraces = true,
        };

        var serializedJson = Serializer.Serialize(jo, serializerSettings);
        Assert.IsTrue(IsJsonValid(serializedJson));

        Console.WriteLine(serializedJson);
    }

    [Test]
    public void TestJsonWithComplexCommentsAndFormatting()
    {
        var json = @"{
          // Comment at the start
          ""person"": {
            ""name"": ""Alice"",
            ""age"": 25 /* Inline age comment */,
            ""hobbies"": [ ""reading"", ""hiking"" ]  // Hobbies listed here
          },
          ""meta"": { /* Metadata block */
            ""created"": ""2024-10-01"", // Creation date
            ""updated"": ""2024-10-12""  // Last updated date
          }
        }";

        var node = JsonNode.Parse(json);
        var jo = (JsonObject)node;

        jo["meta"].AsObject()["author"] = JsonValue.FromObject("Tester");  // Add new metadata

        var serializerSettings = new SerializerSettings
        {
            WriteComments = true,
            Indent = "  ",
            WriteTrailingCommas = false,
            SpaceToInlineComments = 2,
            SpacedBraces = true,
        };

        var serializedJson = Serializer.Serialize(jo, serializerSettings);
        Assert.IsTrue(IsJsonValid(serializedJson));

        Console.WriteLine(serializedJson);
    }
}
