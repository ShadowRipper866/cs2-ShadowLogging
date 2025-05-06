using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Text;

namespace ShadowLogger.Config
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RangeAttribute : Attribute
    {
        public int Min { get; }
        public int Max { get; }
        public int Default { get; }
        public string Message { get; }

        public RangeAttribute(int min, int max, int defaultValue, string message)
        {
            Min = min;
            Max = max;
            Default = defaultValue;
            Message = message;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class CommentAttribute : Attribute
    {
        public string Comment { get; }

        public CommentAttribute(string comment)
        {
            Comment = comment;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class BreakLineAttribute : Attribute
    {
        public string BreakLine { get; }

        public BreakLineAttribute(string breakLine)
        {
            BreakLine = breakLine;
        }
    }
    public static class Configs
    {
        public static class Shared {
            public static string? CookiesModule { get; set; }
        }
        private static readonly string ConfigDirectoryName = "config";
        private static readonly string ConfigFileName = "config.json";
        private static string? _configFilePath;
        private static ConfigData? _configData;

        private static readonly JsonSerializerOptions SerializationOptions = new()
        {
            Converters =
            {
                new JsonStringEnumConverter()
            },
            WriteIndented = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        public static bool IsLoaded()
        {
            return _configData is not null;
        }

        public static ConfigData GetConfigData()
        {
            if (_configData is null)
            {
                throw new Exception("Config not yet loaded.");
            }
            
            return _configData;
        }

        public static ConfigData Load(string modulePath)
        {
            var configFileDirectory = Path.Combine(modulePath, ConfigDirectoryName);
            if(!Directory.Exists(configFileDirectory))
            {
                Directory.CreateDirectory(configFileDirectory);
            }

            _configFilePath = Path.Combine(configFileDirectory, ConfigFileName);
            if (File.Exists(_configFilePath))
            {
                _configData = JsonSerializer.Deserialize<ConfigData>(File.ReadAllText(_configFilePath), SerializationOptions);
                _configData!.Validate();
            }
            else
            {
                _configData = new ConfigData();
                _configData.Validate();
            }

            if (_configData is null)
            {
                throw new Exception("Failed to load configs.");
            }

            SaveConfigData(_configData);
            
            return _configData;
        }

        private static void SaveConfigData(ConfigData configData)
        {
            if (_configFilePath is null)
                throw new Exception("Config not yet loaded.");

            string json = JsonSerializer.Serialize(configData, SerializationOptions);
            json = Regex.Unescape(json);

            var lines = json.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var newLines = new List<string>();

            foreach (var line in lines)
            {
                var match = Regex.Match(line, @"^\s*""(\w+)""\s*:.*");
                bool isPropertyLine = false;
                PropertyInfo? propInfo = null;

                if (match.Success)
                {
                    string propName = match.Groups[1].Value;
                    propInfo = typeof(ConfigData).GetProperty(propName);

                    var breakLineAttr = propInfo?.GetCustomAttribute<BreakLineAttribute>();
                    if (breakLineAttr != null)
                    {
                        string breakLine = breakLineAttr.BreakLine;

                        if (breakLine.Contains("{space}"))
                        {
                            breakLine = breakLine.Replace("{space}", "").Trim();

                            if (breakLineAttr.BreakLine.StartsWith("{space}"))
                            {
                                newLines.Add("");
                            }

                            newLines.Add("// " + breakLine);
                            newLines.Add("");
                        }
                        else
                        {
                            newLines.Add("// " + breakLine);
                        }
                    }

                    var commentAttr = propInfo?.GetCustomAttribute<CommentAttribute>();
                    if (commentAttr != null)
                    {
                        var commentLines = commentAttr.Comment.Split('\n');
                        foreach (var commentLine in commentLines)
                        {
                            newLines.Add("// " + commentLine.Trim());
                        }
                    }

                    isPropertyLine = true;
                }

                newLines.Add(line);

                if (isPropertyLine && propInfo?.GetCustomAttribute<CommentAttribute>() != null)
                {
                    newLines.Add("");
                }
            }

            var adjustedLines = new List<string>();
            foreach (var line in newLines)
            {
                adjustedLines.Add(line);
                if (Regex.IsMatch(line, @"^\s*\],?\s*$"))
                {
                    adjustedLines.Add("");
                }
            }

            File.WriteAllText(_configFilePath, string.Join(Environment.NewLine, adjustedLines), Encoding.UTF8);
        }

        public class ConfigData
        {
            private string? _Version;
            private string? _Link;
            [BreakLine("----------------------------[ ↓ Plugin Info ↓ ]----------------------------{space}")]
            public string Version
            {
                get => _Version!;
                set
                {
                    _Version = value;
                    if (_Version != ShadowChatLogger.Instance.ModuleVersion)
                    {
                        Version = ShadowChatLogger.Instance.ModuleVersion;
                    }
                }
            }

            public string Link
            {
                get => _Link!;
                set
                {
                    _Link = value;
                    if (_Link != "https://github.com/ShadowRipper866")
                    {
                        Link = "https://github.com/ShadowRipper866";
                    }
                }
            }

            [BreakLine("{space}----------------------------[ ↓ Locally Config ↓ ]----------------------------{space}")]
            [Comment("Save Chat Messages Locally (In ../Chat-Logger-ShadowRipper/logs/)?\n1 = Yes, But Log When Player Chat Direct\n2 = Yes, But Log And Send All Messages When Round End (Recommended For Performance)\n3 = Yes, But Log And Send All Messages When Map End (Recommended For Performance)\n0 = No, Disable")]
            [Range(0, 3, 1, "[Chat Logger] Locally_Enable: is invalid, setting to default value (1) Please Choose From 0 To 3.\n[Chat Logger] 1 = Yes, But Log When Player Chat Direct\n[Chat Logger] 2 = Yes, But Log And Send All Messages When Round End (Recommended For Performance)\n[Chat Logger] 3 = Yes, But Log And Send All Messages When Map End (Recommended For Performance)\n[Chat Logger] 0 = No, Disable This Feature")]
            public int Locally_Enable { get; set; }

            [Comment("Required [Locally_Enable = 1/2/3]\nLog Messages Only:\n1 = Both Public Chat And Team Chat\n2 = Public Chat Only\n3 = Team Chat Only")]
            [Range(1, 3, 1, "[Chat Logger] Locally_LogMessagesOnly: is invalid, setting to default value (1) Please Choose From 1 To 3.\n[Chat Logger] 1 = Both Public Chat And Team Chat\n[Chat Logger] 2 = Public Chat Only\n[Chat Logger] 3 = Team Chat Only")]
            public int Locally_LogMessagesOnly { get; set; }

            [Comment("Required [Locally_Enable = 1/2/3]\nLog These Flags Messages Only And Ignore Log Others\nExample:\n!76561198206086993,@css/include,#css/include,include\n\"\" = To Log Everyone")]
            public string Locally_IncludeTheseFlagsMessagesOnly { get; set; }

            [Comment("Required [Locally_Enable = 1/2/3]\nDont Log These Flags Messages And Log Others\nExample:\n!76561198206086993,@css/exclude,#css/exclude,exclude\n\"\" = To Exclude Everyone")]
            public string Locally_ExcludeFlagsMessages { get; set; }

            [Comment("Required [Locally_Enable = 1/2/3]\nDont Log Messages If It Start With\n\"\" = Disable This Feature")]
            public string Locally_ExcludeMessagesStartWith { get; set; }

            [Comment("Required [Locally_Enable = 1/2/3]\nDont Log Messages If It Contains Less Than X Letters\n0 = Disable This Feature")]
            public int Locally_ExcludeMessagesContainsLessThanXLetters { get; set; }

            [Comment("Required [Locally_Enable = 1/2/3]\nDont Log Messages If It Duplicates Previous Message?\ntrue = Yes\nfalse = No")]
            public bool Locally_ExcludeMessagesDuplicate { get; set; }

            [Comment("Required [Locally_Enable = 1/2/3]\nHow Do You Like The Message Format\n{DATE} = [Locally_DateFormat]\n{TIME} = [Locally_TimeFormat]\n{PLAYER_NAME} = Player Name\n{PLAYER_MESSAGE} = Player Message\n{PLAYER_TEAM} = Check If Player Wrote In Chat Team Or Public Chat [TEAM]\n{PLAYER_STEAMID} = STEAM_0:1:122910632\n{PLAYER_STEAMID3} = U:1:245821265\n{PLAYER_STEAMID32} = 245821265\n{PLAYER_STEAMID64} = 76561198206086993\n{PLAYER_IP} = 123.45.67.89\n\"\" = Disable This Feature")]
            public string Locally_MessageFormat { get; set; }

            [Comment("Required [Locally_Enable = 1/2/3]\nHow Do You Like Date Format\nExamples:\ndd MM yyyy = 25 12 2023\nMM/dd/yy = 12/25/23\nMM-dd-yyyy = 12-25-2025")]
            public string Locally_DateFormat { get; set; }

            [Comment("Required [Locally_Enable = 1/2/3]\nHow Do You Like Time Format\nExamples:\nHH:mm = 14:30\nhh:mm a = 02:30 PM\nHH:mm:ss = 14:30:45")]
            public string Locally_TimeFormat { get; set; }

            [Comment("Required [Locally_Enable = 1/2/3]\nAuto Delete File Logs That Pass Than X Old Days\n0 = Disable This Feature")]
            public int Locally_AutoDeleteLogsMoreThanXdaysOld { get; set; }


            [BreakLine("{space}----------------------------[ ↓ Discord Config ↓ ]----------------------------{space}")]


            [Comment("Discord WebHook\nExample: https://discord.com/api/webhooks/XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX\n\"\" = Disable This Feature")]
            public string Discord_WebHook { get; set; }

            [Comment("Required [Discord_Style 2/3/4/5]\nHow Would You Side Color Message To Be Use This Site (https://htmlcolorcodes.com/color-picker) For Color Pick")]
            public string Discord_SideColor { get; set; }

            [Comment("Required [Discord_WebHook]\nLog Messages Only:\n1 = Both Public Chat And Team Chat\n2 = Public Chat Only\n3 = Team Chat Only")]
            [Range(1, 3, 1, "[Chat Logger] Discord_LogMessagesOnly: is invalid, setting to default value (1) Please Choose From 1 To 3.\n[Chat Logger] 1 = Both Public Chat And Team Chat\n[Chat Logger] 2 = Public Chat Only\n[Chat Logger] 3 = Team Chat Only")]
            public int Discord_LogMessagesOnly { get; set; }

            public int Discord_Style { get; set; }

            [Comment("Required [Discord_WebHook]\nLog These Flags Messages Only And Ignore Log Others\nExample:\n!76561198206086993,@css/include,#css/include,include\n\"\" = To Log Everyone")]
            public string Discord_IncludeTheseFlagsMessagesOnly { get; set; }

            [Comment("Required [Discord_WebHook]\nDont Log These Flags Messages And Log Others\nExample:\n!76561198206086993,@css/exclude,#css/exclude,exclude\n\"\" = To Exclude Everyone")]
            public string Discord_ExcludeFlagsMessages { get; set; }

            [Comment("Required [Discord_WebHook]\nDont Log Messages If It Start With\n\"\" = Disable This Feature")]
            public string Discord_ExcludeMessagesStartWith { get; set; }

            [Comment("Required [Discord_WebHook]\nDont Log Messages If It Contains Less Than X Letters\n0 = Disable This Feature")]
            public int Discord_ExcludeMessagesContainsLessThanXLetters { get; set; }

            [Comment("Required [Discord_WebHook]\nLog duplicate massages?")]
            public bool Discord_ExcludeMessagesDuplicate { get; set; }
            public string Discord_UsersWithNoAvatarImage { get; set; }
            [Comment("Required [Discord_WebHook]\nDate format")]
            public string Discord_DateFormat { get; set; }

            [Comment("Required [Discord_WebHook]\nTime format")]
            public string Discord_TimeFormat { get; set; }

            public string Discord_MessageFormat{get; set;}




            [BreakLine("{space}----------------------------[ ↓ Utilities  ↓ ]----------------------------{space}")]

            [Comment("Enable Debug Plugin In Server Console (Helps You To Debug Issues You Facing)?\ntrue = Yes\nfalse = No")]
            public bool EnableDebug { get; set; }
            
            public ConfigData()
            {
                Version = ShadowChatLogger.Instance.ModuleVersion;
                Link = "https://github.com/oqyh/ShadowRipper866";

                Locally_Enable = 1;
                Locally_LogMessagesOnly = 1;
                Locally_IncludeTheseFlagsMessagesOnly = "";
                Locally_ExcludeFlagsMessages = "@css/exclude,#css/exclude";
                Locally_ExcludeMessagesStartWith = "!./";
                Locally_ExcludeMessagesContainsLessThanXLetters = 0;
                Locally_ExcludeMessagesDuplicate = false;
                Locally_MessageFormat = "{PLAYER_MESSAGE}";
                Locally_DateFormat = "dd-MM-yyyy";
                Locally_TimeFormat = "HH:mm:ss";
                Locally_AutoDeleteLogsMoreThanXdaysOld = 7;
                
                Discord_WebHook = "";
                Discord_SideColor = "00FFFF";
                Discord_LogMessagesOnly = 1;
                Discord_UsersWithNoAvatarImage = "https://avatars.fastly.steamstatic.com/fef49e7fa7e1997310d705b2a6158ff8dc1cdfeb_full.jpg";
                Discord_Style = 4;
                Discord_IncludeTheseFlagsMessagesOnly = "";
                Discord_ExcludeFlagsMessages = "@css/exclude,#css/exclude";
                Discord_ExcludeMessagesStartWith = "!./";
                Discord_ExcludeMessagesContainsLessThanXLetters = 0;
                Discord_ExcludeMessagesDuplicate = false;
                Discord_MessageFormat = "{PLAYER_MESSAGE}";
                Discord_DateFormat = "dd-MM-yyyy";
                Discord_TimeFormat = "HH:mm:ss";

                EnableDebug = false;
            }
            public void Validate()
            {
                foreach (var prop in GetType().GetProperties())
                {
                    var rangeAttr = prop.GetCustomAttribute<RangeAttribute>();
                    if (rangeAttr != null && prop.PropertyType == typeof(int))
                    {
                        int value = (int)prop.GetValue(this)!;
                        if (value < rangeAttr.Min || value > rangeAttr.Max)
                        {
                            prop.SetValue(this, rangeAttr.Default);
                            Helper.DebugMessage(rangeAttr.Message,false);
                        }
                    }
                }
            }
        }
    }
}