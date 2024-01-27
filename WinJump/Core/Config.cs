using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace WinJump.Core;

/// <summary>
/// Handles loading the configuration file
/// </summary>
internal sealed class Config {
    public static readonly string LOCATION = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".winjump");

    [JsonProperty("move-window-to")]
    public required List<JumpWindowToDesktop> MoveWindowTo { get; set; }

    [JsonProperty("toggle-groups")]
    public required List<ToggleGroup> ToggleGroups { get; set; }

    [JsonProperty("jump-to")]
    public required List<JumpTo> JumpTo { get; set; }

    [JsonProperty("jump-current-goes-to-last")]
    public required bool JumpCurrentGoesToLast { get; set; }

    [JsonProperty("change-desktops-with-scroll")]
    public required bool ChangeDesktopsWithScroll { get; set; }

    public static Config Load() {
        try {
            EnsureCreated();

            string content = File.ReadAllText(LOCATION);

            var config = JsonConvert.DeserializeObject<Config>(content);

            if(config == null) {
                throw new Exception("Failed to deserialize config file");
            }

            // Check for jump tos with duplicate shortcuts
            for(int i = 0; i < config.JumpTo.Count; i++) {
                var shortcut = config.JumpTo[i].Shortcut;
                for(int j = i + 1; j < config.JumpTo.Count; j++) {
                    if(config.JumpTo[j].Shortcut.IsEqual(shortcut)) {
                        throw new Exception("Duplicate jump to shortcut");
                    }

                    if(config.JumpTo[i].Desktop <= 0) {
                        throw new Exception("Invalid desktop number");
                    }
                }
            }

            // Check for toggle groups with duplicate shortcuts
            for(int i = 0; i < config.ToggleGroups.Count; i++) {
                var shortcut = config.ToggleGroups[i].Shortcut;
                for(int j = i + 1; j < config.ToggleGroups.Count; j++) {
                    if(config.ToggleGroups[j].Shortcut.IsEqual(shortcut)) {
                        throw new Exception("Duplicate toggle group shortcut");
                    }

                    if(config.ToggleGroups[i].Desktops.Any((d) => d <= 0)) {
                        throw new Exception("Invalid desktop number");
                    }
                }
            }

            // Check for move windows with duplicate shortcuts
            for(int i = 0; i < config.MoveWindowTo.Count; i++) {
                var shortcut = config.MoveWindowTo[i].Shortcut;
                for(int j = i + 1; j < config.MoveWindowTo.Count; j++) {
                    if(config.MoveWindowTo[j].Shortcut.IsEqual(shortcut)) {
                        throw new Exception("Duplicate move window shortcut");
                    }

                    if(config.MoveWindowTo[i].Desktop <= 0) {
                        throw new Exception("Invalid desktop number");
                    }
                }
            }

            return config;
        } catch(Exception) {
            return Default();
        }
    }

    public static void EnsureCreated() {
        if(File.Exists(LOCATION)) return;

        var config = Default();
        string content = JsonConvert.SerializeObject(config, Formatting.Indented);
        File.WriteAllText(LOCATION, content);
    }

    /// <summary>
    /// Creates the default config file
    /// </summary>
    /// <returns>Default configuration</returns>
    private static Config Default() {
        var jumpTo = new List<JumpTo>();

        uint desktop = 1;
        for(var k = Keys.D0; k <= Keys.D9; k++) {
            jumpTo.Add(new JumpTo {
                Shortcut = new Shortcut {
                    ModifierKeys = ModifierKeys.Alt,
                    Keys = k,
                },
                Desktop = k == Keys.D0 ? 10 : desktop++
            });
        }

        return new Config {
            MoveWindowTo = jumpTo.Select(x => new JumpWindowToDesktop {
                Shortcut = new Shortcut {
                    Keys = x.Shortcut.Keys,
                    ModifierKeys = x.Shortcut.ModifierKeys | ModifierKeys.Shift
                },
                Desktop = x.Desktop
            }).ToList(),
            JumpTo = jumpTo,
            ToggleGroups = [],
            JumpCurrentGoesToLast = true,
            ChangeDesktopsWithScroll = false
        };
    }
}

public sealed class JumpWindowToDesktop {
    [JsonConverter(typeof(ShortcutConverter))]
    public required Shortcut Shortcut { get; set; }

    public required uint Desktop { get; set; }
}

public sealed class ToggleGroup {
    [JsonConverter(typeof(ShortcutConverter))]
    public required Shortcut Shortcut { get; set; }

    public required List<int> Desktops { get; set; }

    public bool IsEqual(ToggleGroup other) {
        return Shortcut.IsEqual(other.Shortcut);
    }
}

public sealed class JumpTo {
    [JsonConverter(typeof(ShortcutConverter))]
    [JsonProperty("shortcut")]
    public required Shortcut Shortcut { get; set; }

    [JsonProperty("desktop")]
    public required uint Desktop { get; set; }

    public bool IsEqual(JumpTo other) {
        return Shortcut.IsEqual(other.Shortcut);
    }
}

public sealed class Shortcut {
    private static readonly Dictionary<string, ModifierKeys> LOOKUP = new() {
        {"ctrl", ModifierKeys.Control},
        {"alt", ModifierKeys.Alt},
        {"shift", ModifierKeys.Shift},
        {"win", ModifierKeys.Win}
    };

    public ModifierKeys ModifierKeys { get; set; }
    public Keys Keys { get; set; }

    public bool IsEqual(Shortcut other) {
        return ModifierKeys == other.ModifierKeys && Keys == other.Keys;
    }

    public bool ModifiersEqual(Shortcut other) {
        return ModifierKeys == other.ModifierKeys;
    }

    // Parsing stuff

    // Parses the shortcut from a string
    public static Shortcut FromString(string expression) {
        var stack = new Queue<string>(expression.Split('+'));

        ModifierKeys modifiers = 0;

        while(stack.Count > 0) {
            string token = stack.Dequeue();

            if(LOOKUP.TryGetValue(token, out var value)) {
                modifiers |= value;
            } else {
                return new Shortcut {
                    ModifierKeys = modifiers,
                    Keys = (Keys) Enum.Parse(typeof(Keys), token, true)
                };
            }
        }

        return new Shortcut {
            ModifierKeys = modifiers,
            Keys = Keys.None
        };
    }

    public override string ToString() {
        string modifiers = string.Join("+", Enum.GetValues<ModifierKeys>().Where(key => ModifierKeys.HasFlag(key))
            .Select(key => LOOKUP.First(lookup => lookup.Value == key).Key));

        return modifiers + "+" + Keys.ToString().ToLower();
    }
}

public sealed class ShortcutConverter : JsonConverter {
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
        if(value is not Shortcut shortcut) {
            throw new Exception("Can't serialize null shortcut");
        }

        serializer.Serialize(writer, shortcut.ToString());
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer) {
        string expression = reader.Value?.ToString() ?? throw new Exception("Invalid shortcut");
        return Shortcut.FromString(expression);
    }

    public override bool CanConvert(Type objectType) {
        return objectType == typeof(string) || objectType == typeof(Shortcut);
    }
}
