/*
 * ═══════════════════════════════════════════════════════════════
 *                          UNITY FORGE
 *                    JSON Create Simple Action
 * ═══════════════════════════════════════════════════════════════
 * 
 * Author: Unity Forge
 * Github: https://github.com/unityforgedev
 * 
 */

using UnityEngine;
using System.Text;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("JSON")]
    [Tooltip("Create a simple JSON object from key-value pairs. Perfect for building API request bodies.")]
    public class JSONCreateSimple : FsmStateAction
    {
        [Title("Key-Value Pairs")]
        [Tooltip("Enter key-value pairs (format: key:value, one per line)")]
        [UIHint(UIHint.TextArea)]
        public FsmString keyValuePairs;

        [Title("Individual Fields (Optional)")]
        [Tooltip("Field 1 - Key name")]
        public FsmString key1;
        [Tooltip("Field 1 - Value")]
        public FsmString value1;

        [Tooltip("Field 2 - Key name")]
        public FsmString key2;
        [Tooltip("Field 2 - Value")]
        public FsmString value2;

        [Tooltip("Field 3 - Key name")]
        public FsmString key3;
        [Tooltip("Field 3 - Value")]
        public FsmString value3;

        [Tooltip("Field 4 - Key name")]
        public FsmString key4;
        [Tooltip("Field 4 - Value")]
        public FsmString value4;

        [Tooltip("Field 5 - Key name")]
        public FsmString key5;
        [Tooltip("Field 5 - Value")]
        public FsmString value5;

        [Title("Output")]
        [RequiredField]
        [Tooltip("Store the created JSON string")]
        [UIHint(UIHint.Variable)]
        public FsmString storeJSON;

        [Title("Options")]
        [Tooltip("Pretty print with indentation")]
        public FsmBool prettyPrint = false;

        [Tooltip("Auto-detect value types (numbers, booleans, null)")]
        public FsmBool autoDetectTypes = true;

        [Tooltip("Event sent when JSON is created")]
        public FsmEvent successEvent;

        [Title("Debug")]
        [Tooltip("Log created JSON to console")]
        public FsmBool logJSON = false;

        public override void Reset()
        {
            keyValuePairs = null;
            key1 = null;
            value1 = null;
            key2 = null;
            value2 = null;
            key3 = null;
            value3 = null;
            key4 = null;
            value4 = null;
            key5 = null;
            value5 = null;
            storeJSON = null;
            prettyPrint = false;
            autoDetectTypes = true;
            successEvent = null;
            logJSON = false;
        }

        public override void OnEnter()
        {
            CreateJSON();
            Finish();
        }

        private void CreateJSON()
        {
            try
            {
                var fields = new System.Collections.Generic.List<KeyValuePair>();

                // Parse key-value pairs from text area
                if (!string.IsNullOrEmpty(keyValuePairs.Value))
                {
                    ParseKeyValuePairs(keyValuePairs.Value, fields);
                }

                // Add individual fields
                AddField(fields, key1, value1);
                AddField(fields, key2, value2);
                AddField(fields, key3, value3);
                AddField(fields, key4, value4);
                AddField(fields, key5, value5);

                // Build JSON
                string json = BuildJSON(fields);

                if (!storeJSON.IsNone)
                {
                    storeJSON.Value = json;
                }

                if (logJSON.Value)
                {
                    Debug.Log($"[JSON Create] Created JSON:\n{json}");
                }

                Fsm.Event(successEvent);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[JSON Create] Error creating JSON: {e.Message}");
            }
        }

        private void ParseKeyValuePairs(string input, System.Collections.Generic.List<KeyValuePair> fields)
        {
            string[] lines = input.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//") || trimmed.StartsWith("#"))
                {
                    continue; // Skip empty lines and comments
                }

                if (trimmed.Contains(":"))
                {
                    string[] parts = trimmed.Split(new[] { ':' }, 2);
                    string key = parts[0].Trim();
                    string value = parts.Length > 1 ? parts[1].Trim() : "";

                    if (!string.IsNullOrEmpty(key))
                    {
                        fields.Add(new KeyValuePair { key = key, value = value });
                    }
                }
            }
        }

        private void AddField(System.Collections.Generic.List<KeyValuePair> fields, FsmString key, FsmString value)
        {
            if (!string.IsNullOrEmpty(key.Value))
            {
                fields.Add(new KeyValuePair { key = key.Value, value = value.Value ?? "" });
            }
        }

        private string BuildJSON(System.Collections.Generic.List<KeyValuePair> fields)
        {
            if (fields.Count == 0)
            {
                return prettyPrint.Value ? "{\n}" : "{}";
            }

            var sb = new StringBuilder();
            string indent = prettyPrint.Value ? "  " : "";
            string newline = prettyPrint.Value ? "\n" : "";

            sb.Append("{");
            sb.Append(newline);

            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];

                if (prettyPrint.Value)
                {
                    sb.Append(indent);
                }

                // Add key
                sb.Append("\"");
                sb.Append(EscapeString(field.key));
                sb.Append("\":");

                if (prettyPrint.Value)
                {
                    sb.Append(" ");
                }

                // Add value
                sb.Append(FormatValue(field.value));

                // Add comma if not last item
                if (i < fields.Count - 1)
                {
                    sb.Append(",");
                }

                sb.Append(newline);
            }

            sb.Append("}");

            return sb.ToString();
        }

        private string FormatValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "\"\"";
            }

            if (!autoDetectTypes.Value)
            {
                // Always return as string
                return "\"" + EscapeString(value) + "\"";
            }

            // Auto-detect type
            string trimmed = value.Trim();

            // Check for null
            if (trimmed.ToLower() == "null")
            {
                return "null";
            }

            // Check for boolean
            if (trimmed.ToLower() == "true")
            {
                return "true";
            }
            if (trimmed.ToLower() == "false")
            {
                return "false";
            }

            // Check for number
            if (IsNumber(trimmed))
            {
                return trimmed;
            }

            // Check for JSON object or array
            if ((trimmed.StartsWith("{") && trimmed.EndsWith("}")) ||
                (trimmed.StartsWith("[") && trimmed.EndsWith("]")))
            {
                return trimmed; // Return as-is (nested JSON)
            }

            // Default to string
            return "\"" + EscapeString(value) + "\"";
        }

        private bool IsNumber(string value)
        {
            // Check if it's a valid number
            return double.TryParse(value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out _);
        }

        private string EscapeString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        private class KeyValuePair
        {
            public string key;
            public string value;
        }
    }
}