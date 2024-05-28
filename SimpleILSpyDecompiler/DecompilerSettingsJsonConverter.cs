using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;

namespace SimpleILSpyDecompiler;

public class DecompilerSettingsJsonConverter : JsonConverter<DecompilerSettings>
{
  private readonly Dictionary<string, List<PropertyInfo>> _groupedPropertiesInfo = typeof(DecompilerSettings)
    .GetProperties()
    .Where(x => x.GetCustomAttribute<BrowsableAttribute>()?.Browsable != false)
    .GroupBy(x => x.GetCustomAttribute<CategoryAttribute>()?.Category.Replace("DecompilerSettings.", "") ?? "")
    .OrderBy(x => !x.Key.StartsWith("C#"))
    .ThenBy(x => x.Key.StartsWith("C#") ? float.Parse(x.Key.Split('/')[0].Trim().Split(" ")[1]) : 0.0)
    .ThenBy(x => x.Key)
    .ToDictionary(x => x.Key, x => x.ToList());

  private readonly List<PropertyInfo> _formattingOptionsPropertiesInfo =
    typeof(CSharpFormattingOptions).GetProperties().ToList();

  public override DecompilerSettings? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    if (reader.TokenType != JsonTokenType.StartObject)
    {
      throw new JsonException();
    }

    DecompilerSettings settings = new DecompilerSettings();
    string category = "";
    while (reader.Read())
    {
      if (reader.TokenType == JsonTokenType.EndObject)
      {
        if (category == "")
          break;
        category = "";
        continue;
      }

      if (reader.TokenType != JsonTokenType.PropertyName)
        throw new JsonException();

      string propName = reader.GetString()!;
      reader.Read();

      if (_groupedPropertiesInfo.ContainsKey(propName))
      {
        category = propName;
        continue;
      }

      if (propName == nameof(DecompilerSettings.CSharpFormattingOptions))
      {
        if (reader.TokenType != JsonTokenType.StartObject)
          throw new JsonException();

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
          if (reader.TokenType != JsonTokenType.PropertyName)
            throw new JsonException();

          string formatPropName = reader.GetString()!;
          reader.Read();

          PropertyInfo? formattingPropertyInfo =
            _formattingOptionsPropertiesInfo.FirstOrDefault(x => x.Name == formatPropName);
          if (formattingPropertyInfo is null)
            throw new JsonException();

          switch (formattingPropertyInfo.PropertyType.Name)
          {
            case nameof(String):
              formattingPropertyInfo.GetSetMethod()?.Invoke(settings.CSharpFormattingOptions, [reader.GetString()]);
              break;
            case nameof(Boolean):
              formattingPropertyInfo.GetSetMethod()?.Invoke(settings.CSharpFormattingOptions, [reader.GetBoolean()]);
              break;
            case nameof(Byte):
              formattingPropertyInfo.GetSetMethod()?.Invoke(settings.CSharpFormattingOptions, [reader.GetByte()]);
              break;
            case nameof(Int16):
              formattingPropertyInfo.GetSetMethod()?.Invoke(settings.CSharpFormattingOptions, [reader.GetInt16()]);
              break;
            case nameof(Int32):
              formattingPropertyInfo.GetSetMethod()?.Invoke(settings.CSharpFormattingOptions, [reader.GetInt32()]);
              break;
            case nameof(Int64):
              formattingPropertyInfo.GetSetMethod()?.Invoke(settings.CSharpFormattingOptions, [reader.GetInt64()]);
              break;
          }
        }

        continue;
      }

      if (string.IsNullOrWhiteSpace(category))
        throw new JsonException();

      PropertyInfo? propertyInfo = _groupedPropertiesInfo[category].FirstOrDefault(x => x.Name == propName);
      if (propertyInfo is null)
        throw new JsonException();

      if (reader.TokenType != JsonTokenType.False && reader.TokenType != JsonTokenType.True)
        throw new JsonException();

      propertyInfo.GetSetMethod()?.Invoke(settings, [reader.GetBoolean()]);
    }

    return settings;
  }

  public override void Write(Utf8JsonWriter writer, DecompilerSettings value, JsonSerializerOptions options)
  {
    writer.WriteStartObject();

    foreach (var propertiesInfoGroup in _groupedPropertiesInfo)
    {
      writer.WriteStartObject(propertiesInfoGroup.Key);

      foreach (var propertyInfo in propertiesInfoGroup.Value)
      {
        var propValue = propertyInfo.GetGetMethod()?.Invoke(value, [])!;
        if (propertyInfo.PropertyType.Name == nameof(Boolean))
          writer.WriteBoolean(propertyInfo.Name, (bool)propValue);
      }

      writer.WriteEndObject();
    }

    writer.WriteStartObject(nameof(DecompilerSettings.CSharpFormattingOptions));
    foreach (PropertyInfo propertyInfo in _formattingOptionsPropertiesInfo)
    {
      var propValue = propertyInfo.GetGetMethod()?.Invoke(value.CSharpFormattingOptions, []);
      if (propertyInfo.PropertyType.IsEnum)
      {
        writer.WriteString(propertyInfo.Name, propValue?.ToString());
      }
      else
      {
        switch (propertyInfo.PropertyType.Name)
        {
          case nameof(String):
            writer.WriteString(propertyInfo.Name, (string?)propValue);
            break;
          case nameof(Boolean):
            writer.WriteBoolean(propertyInfo.Name, (bool)propValue!);
            break;
          case nameof(Byte):
            writer.WriteNumber(propertyInfo.Name, (byte)propValue!);
            break;
          case nameof(Int16):
            writer.WriteNumber(propertyInfo.Name, (short)propValue!);
            break;
          case nameof(Int32):
            writer.WriteNumber(propertyInfo.Name, (int)propValue!);
            break;
          case nameof(Int64):
            writer.WriteNumber(propertyInfo.Name, (long)propValue!);
            break;
        }
      }
    }

    writer.WriteEndObject();

    writer.WriteEndObject();
  }
}
