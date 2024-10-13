namespace JsonCfgNet;

public static class JsonNodeExtensions
{
    public static JsonObject AsObject(this JsonNode node) => (JsonObject)node;
    public static JsonArray AsArray(this JsonNode node) => (JsonArray)node;
    public static JsonProperty AsProperty(this JsonNode node) => (JsonProperty)node;
}