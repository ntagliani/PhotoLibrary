using System;
using System.Text.Json;
using System.IO;using System.Text.Json.Nodes;

namespace PhotoLibrary.Settings
{
    public interface ISettings
    {      
        public int Version { get; set; }
        public void Load()
        {
            var staticSettings = AddInManager.Instance.GetInstance<StaticSettings>();
            string settingsPath = Path.Combine(staticSettings.ConfigFolder, $"{GetType().Name}.json");

            if (!File.Exists(settingsPath))
            {
                Save();
                return;
            }
            string settingsText = File.ReadAllText(settingsPath);
            JsonNode node = JsonNode.Parse(settingsText);
            int loadedVersion = node[nameof(Version)].GetValue<int>();
            if (loadedVersion != Version)
            {
                PatchSettings(loadedVersion, node);
            }
            else
            {
                foreach (var property in GetType().GetProperties())
                {
                    switch (node[property.Name].GetValueKind())
                    {
                        default:
                            throw new ArgumentException("Unexpected json type encoutered");
                        case JsonValueKind.Number:
                            {

                                if (property.PropertyType == typeof(double))
                                {
                                    var value = node[property.Name].AsValue().GetValue<double>();
                                    property.SetValue(this, value);
                                }
                                else if (property.PropertyType == typeof(int))
                                {
                                    var value = node[property.Name].AsValue().GetValue<int>();
                                    property.SetValue(this, value);
                                }
                                else if (property.PropertyType == typeof(float))
                                {
                                    var value = node[property.Name].AsValue().GetValue<float>();
                                    property.SetValue(this, value);
                                }
                                else
                                {
                                    throw new ArgumentException("Unexpected setting type encoutered");
                                }
                                break;
                            }
                        case JsonValueKind.String:
                            {
                                var value = node[property.Name].AsValue().GetValue<string>();
                                property.SetValue(this, value);
                                break;
                            }
                    }
                }
            }
        }

        protected void PatchSettings(int loadedVersion, JsonNode loaded) { }
        public void Save()
        {
            var staticSettings = AddInManager.Instance.GetInstance<StaticSettings>();
            string settingsPath = Path.Combine(staticSettings.ConfigFolder, $"{GetType().Name}.json");

            using FileStream fs = File.Create(settingsPath);
            using var writer = new Utf8JsonWriter(fs, options: new JsonWriterOptions() { Indented = true });

            using JsonDocument deserialized = JsonSerializer.SerializeToDocument(this, GetType());
            
            deserialized.WriteTo(writer);
        }
    }
}
