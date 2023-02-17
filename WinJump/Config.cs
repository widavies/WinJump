using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace WinJump {
    internal sealed class Config {
        [JsonProperty("toggle-groups")]
        public List<ToggleGroup> ToggleGroups { get; set; }

        [JsonProperty("jump-to")]
        public List<JumpTo> JumpTo { get; set; }

        public static Config FromFile(string path) {
            try {
                string content = File.ReadAllText(path);

                var config = JsonConvert.DeserializeObject<Config>(content);

                // Check for jump tos with duplicate shortcuts
                for (int i = 0; i < config.JumpTo.Count; i++) {
                    var shortcut = config.JumpTo[i].Shortcut;
                    for (int j = i + 1; j < config.JumpTo.Count; j++) {
                        if (config.JumpTo[j].Shortcut.IsEqual(shortcut)) {
                            throw new Exception("Duplicate jump to shortcut");
                        }
                    }
                }

                // Check for toggle groups with duplicate shortcuts
                for (int i = 0; i < config.ToggleGroups.Count; i++) {
                    var shortcut = config.ToggleGroups[i].Shortcut;
                    for (int j = i + 1; j < config.ToggleGroups.Count; j++) {
                        if (config.ToggleGroups[j].Shortcut.IsEqual(shortcut)) {
                            throw new Exception("Duplicate toggle group shortcut");
                        }
                    }
                }

                return config;
            } catch (Exception) {
                return Default();
            }
        }

        private static Config Default() {
            var jumpTo = new List<JumpTo>();

            for (var k = Keys.D0; k <= Keys.D9; k++) {
                jumpTo.Add(new JumpTo() {
                    Shortcut = new Shortcut() {
                        ModifierKeys = ModifierKeys.Win,
                        Keys = k
                    }
                });
            }

            return new Config {
                JumpTo = jumpTo,
                ToggleGroups = new List<ToggleGroup>()
            };
        }
    }

    public sealed class ToggleGroup {
        [JsonConverter(typeof(ShortcutConverter))]
        public Shortcut Shortcut { get; set; }

        public List<int> Desktops { get; set; }

        public bool IsEqual(ToggleGroup other) {
            return Shortcut.IsEqual(other.Shortcut);
        }
    }

    public sealed class JumpTo {
        [JsonConverter(typeof(ShortcutConverter))]
        public Shortcut Shortcut { get; set; }

        public int Desktop { get; set; }

        public bool IsEqual(JumpTo other) {
            return Shortcut.IsEqual(other.Shortcut);
        }
    }

    public sealed class Shortcut {
        public ModifierKeys ModifierKeys { get; set; }
        public Keys Keys { get; set; }

        public bool IsEqual(Shortcut other) {
            return ModifierKeys == other.ModifierKeys && Keys == other.Keys;
        }
    }

    public sealed class ShortcutConverter : JsonConverter {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer) {
            string expression = reader.Value?.ToString() ?? throw new Exception("Invalid shortcut");

            var stack = new Queue<string>(expression.Split('+'));

            ModifierKeys modifiers = 0;

            var lookup = new Dictionary<string, ModifierKeys> {
                {"ctrl", ModifierKeys.Control},
                {"alt", ModifierKeys.Alt},
                {"shift", ModifierKeys.Shift},
                {"win", ModifierKeys.Win}
            };

            while (stack.Count > 0) {
                string token = stack.Dequeue();

                if (lookup.ContainsKey(token)) {
                    modifiers |= lookup[token];
                } else {
                    return new Shortcut {
                        ModifierKeys = modifiers,
                        Keys = (Keys) Enum.Parse(typeof(Keys), token, true)
                    };
                }
            }

            throw new Exception($"Invalid shortcut: {expression}");
        }

        public override bool CanConvert(Type objectType) {
            return objectType == typeof(string);
        }
    }
}