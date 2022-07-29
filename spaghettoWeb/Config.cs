using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace spaghettoWeb
{
    internal class Config
    {
        [Comment("This option makes it so the opening and closing tags arent (>s and <) but rather <spaghetto> and </spaghetto> - Note that these tags still must be the only thing on a line!")]
        [ConfigBool()]
        [DefaultValue(false)]
        public bool UseHTMLTagsInsteadOfCustomPrefix { get; set; } = false;

        [Comment("The port the server listens to")]
        [ConfigInt(1, 65535)]
        [DefaultValue(8000)]
        public int Port { get; set; } = 8000;
    }

    internal class ConfigReader
    {
        public string cfg = "";

        public ConfigReader(string cfg)
        {
            this.cfg = cfg;
        }

        public T ReadInto<T>()
        {
            T t = Activator.CreateInstance<T>();

            string[] lines = cfg.Split(new string[] { "\r\n", "\n\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach(string line in lines)
            {
                if (line.StartsWith("#")) continue;

                string[] parts = line.Split('=');

                if(parts.Length == 2)
                {
                    System.Reflection.PropertyInfo? propertyInfo = typeof(T).GetProperty(parts[0]);

                    if (propertyInfo == null)
                    {
                        Console.WriteLine("[ConfigReader WARN] Unknown property " + parts[0] + " not applied");
                        continue;
                    }

                    List<ConfigAttribute> attributes = propertyInfo.GetAttributes<ConfigAttribute>();
                    DefaultValueAttribute dValAttr = propertyInfo.GetAttribute<DefaultValueAttribute>();

                    if (dValAttr == null) throw new Exception("Parsing config failed: Field doesn't have a DefaultValue Attribute");
                    if (attributes.Count > 1) throw new Exception("Parsing config failed: Field has more than one ConfigAttribute");
                    if (attributes.Count == 0) continue;

                    ConfigAttribute attr = attributes[0];
                    try
                    {
                        object res = attr.Parse(parts[1]);
                        propertyInfo.SetValue(t, res, null);
                    }catch (Exception ex)
                    {
                        propertyInfo.SetValue(t, dValAttr.Value, null);
                        Console.WriteLine("[ConfigReader WARN] Failed to parse property " + parts[0] + ": " + ex.Message + "; Using default value");
                    }
                }
            }

            return t;
        }

        public string GenerateWithData<T>(T data)
        {
            string template = "";

            PropertyInfo[] props = typeof(T).GetProperties();

            foreach (PropertyInfo prop in props)
            {
                List<ConfigAttribute> attributes = prop.GetAttributes<ConfigAttribute>();
                DefaultValueAttribute dValAttr = prop.GetAttribute<DefaultValueAttribute>();
                CommentAttribute cAttr = prop.GetAttribute<CommentAttribute>();


                if (dValAttr == null) throw new Exception("Creating template failed: Field doesn't have a DefaultValue Attribute");

                if (attributes.Count > 1) throw new Exception("Creating template failed: Field has more than one ConfigAttribute");
                if (attributes.Count == 0) continue;

                object? currentValue = prop.GetValue(data);

                template += $@"{(cAttr != null ? "# " + cAttr.comment + "\n" : "")}{attributes[0].GenerateComment()}
{prop.Name}={(currentValue != null ? currentValue : dValAttr.Value)}

";
            }

            return template;
        }

        public string CreateTemplate<T>()
        {
            string template = "";

            PropertyInfo[] props = typeof(T).GetProperties();

            foreach(PropertyInfo prop in props)
            {
                List<ConfigAttribute> attributes = prop.GetAttributes<ConfigAttribute>();
                DefaultValueAttribute dValAttr = prop.GetAttribute<DefaultValueAttribute>();
                CommentAttribute cAttr = prop.GetAttribute<CommentAttribute>();


                if (dValAttr == null) throw new Exception("Creating template failed: Field doesn't have a DefaultValue Attribute");

                if (attributes.Count > 1) throw new Exception("Creating template failed: Field has more than one ConfigAttribute");
                if (attributes.Count == 0) continue;

                template += $@"{(cAttr != null ? "# " + cAttr.comment + "\n" : "")}{attributes[0].GenerateComment()}
{prop.Name}={dValAttr.Value}

";
            }

            return template;
        }

    }
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DefaultValueAttribute : System.Attribute
    {
        public object Value { get; set; }

        public DefaultValueAttribute(object Value)
        {
            this.Value = Value;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Field |
                       System.AttributeTargets.Property)]
    public abstract class ConfigAttribute : System.Attribute
    {
        public abstract Type ResultingType { get; }

        public abstract object Parse(string str);
        public abstract string GenerateComment();
    }

    public class ConfigIntAttribute : ConfigAttribute
    {
        public int min, max;
        public override Type ResultingType => typeof(int);

        public ConfigIntAttribute(int min = int.MinValue, int max = int.MaxValue)
        {
            this.min = min;
            this.max = max;
        }

        public override object Parse(string str)
        {
            if (!int.TryParse(str, out int value)) throw new Exception("Option is not a valid integer");
            if (value < min) throw new Exception("Option is below the minimum value");
            if (value > max) throw new Exception("Option is above the maximum value");
            return value;
        }

        public override string GenerateComment()
        {
            return $@"# Integer value (Range {min} ~ {max})";
        }
    }

    public class ConfigBoolAttribute : ConfigAttribute
    {
        public override Type ResultingType => typeof(bool);

        public ConfigBoolAttribute()
        {
        }

        public override object Parse(string str)
        {
            if (str.ToLower() != "false" && str.ToLower() != "true") throw new Exception("Option (\"" + str + "\") is not a valid boolean value (true/false)");
            return (str.ToLower() == "true" ? true : false);
        }

        public override string GenerateComment()
        {
            return "# Boolean value (Values true/false)";
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Field |
                       System.AttributeTargets.Property)]
    public class CommentAttribute : System.Attribute
    {
        public string comment;

        public CommentAttribute(string comment)
        {
            this.comment = comment;
        }
    }
}
