/*
 * ═══════════════════════════════════════════════════════════════
 *                          UNITY FORGE
 *                    JSON Create Multiple Action
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
    [Tooltip("Create JSON with unlimited fields. Choose between variable arrays or text input!")]
    public class JSONCreateMultiple : FsmStateAction
    {
        [Title("Input Method")]
        [Tooltip("Choose how to provide keys and values")]
        public InputMethod inputMethod = InputMethod.Variables;

        [Title("Keys & Values (Variables)")]
        [Tooltip("Array of key names")]
        [ArrayEditor(VariableType.String)]
        public FsmArray keys;

        [Tooltip("Array of values (same order as keys)")]
        [ArrayEditor(VariableType.String)]
        public FsmArray values;

        [Title("Keys & Values (Text Input)")]
        [Tooltip("Type key-value pairs directly (format: key:value, one per line)")]
        [UIHint(UIHint.TextArea)]
        public FsmString keyValueText;

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

        [Tooltip("Skip empty/null keys")]
        public FsmBool skipEmptyKeys = true;

        [Tooltip("Treat empty values as empty strings (not skip them)")]
        public FsmBool allowEmptyValues = false;

        [Title("Status")]
        [Tooltip("Store number of fields actually added")]
        [UIHint(UIHint.Variable)]
        public FsmInt fieldsAdded;

        [Tooltip("Event sent when JSON is created")]
        public FsmEvent successEvent;

        [Tooltip("Event sent if no valid fields")]
        public FsmEvent emptyEvent;

        [Title("Debug")]
        [Tooltip("Log created JSON to console")]
        public FsmBool logJSON = false;

        [Tooltip("Log field processing details")]
        public FsmBool logDetails = false;

        public enum InputMethod
        {
            Variables,  // Use FSM variable arrays
            TextInput   // Type directly in text area
        }

        public override void Reset()
        {
            inputMethod = InputMethod.Variables;
            keys = null;
            values = null;
            keyValueText = null;
            storeJSON = null;
            prettyPrint = false;
            autoDetectTypes = true;
            skipEmptyKeys = true;
            allowEmptyValues = false;
            fieldsAdded = null;
            successEvent = null;
            emptyEvent = null;
            logJSON = false;
            logDetails = false;
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

                if (inputMethod == InputMethod.Variables)
                {
                    // Use variable arrays
                    if (keys == null || values == null)
                    {
                        if (logDetails.Value)
                        {
                            Debug.LogWarning("[JSON Create Multiple] Keys or Values array is not set");
                        }
                        SetEmptyJSON();
                        return;
                    }

                    int keyCount = keys.Length;
                    int valueCount = values.Length;
                    int maxCount = Mathf.Min(keyCount, valueCount);

                    if (logDetails.Value)
                    {
                        Debug.Log($"[JSON Create Multiple] Processing {maxCount} fields from arrays (Keys: {keyCount}, Values: {valueCount})");
                    }

                    for (int i = 0; i < maxCount; i++)
                    {
                        object keyObj = keys.Get(i);
                        object valueObj = values.Get(i);

                        string key = keyObj?.ToString() ?? "";
                        string value = valueObj?.ToString() ?? "";

                        if (skipEmptyKeys.Value && string.IsNullOrEmpty(key))
                        {
                            if (logDetails.Value)
                            {
                                Debug.Log($"[JSON Create Multiple] Skipping field {i} - empty key");
                            }
                            continue;
                        }

                        if (!allowEmptyValues.Value && string.IsNullOrEmpty(value))
                        {
                            if (logDetails.Value)
                            {
                                Debug.Log($"[JSON Create Multiple] Skipping field {i} ('{key}') - empty value");
                            }
                            continue;
                        }

                        fields.Add(new KeyValuePair { key = key, value = value });

                        if (logDetails.Value)
                        {
                            Debug.Log($"[JSON Create Multiple] Field {i}: '{key}' = '{value}'");
                        }
                    }
                }
                else // InputMethod.TextInput
                {
                    if (string.IsNullOrEmpty(keyValueText.Value))
                    {
                        if (logDetails.Value)
                        {
                            Debug.LogWarning("[JSON Create Multiple] Key-Value text is empty");
                        }
                        SetEmptyJSON();
                        return;
                    }

                    if (logDetails.Value)
                    {
                        Debug.Log("[JSON Create Multiple] Processing fields from text input");
                    }

                    ParseTextInput(keyValueText.Value, fields);
                }

                if (!fieldsAdded.IsNone)
                {
                    fieldsAdded.Value = fields.Count;
                }

                if (fields.Count == 0)
                {
                    if (logDetails.Value)
                    {
                        Debug.LogWarning("[JSON Create Multiple] No valid fields to create JSON");
                    }

                    if (!storeJSON.IsNone)
                    {
                        storeJSON.Value = prettyPrint.Value ? "{\n}" : "{}";
                    }

                    Fsm.Event(emptyEvent);
                    return;
                }

                string json = BuildJSON(fields);

                if (!storeJSON.IsNone)
                {
                    storeJSON.Value = json;
                }

                if (logJSON.Value)
                {
                    Debug.Log($"[JSON Create Multiple] Created JSON ({fields.Count} fields):\n{json}");
                }

                Fsm.Event(successEvent);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[JSON Create Multiple] Error creating JSON: {e.Message}");

                if (!storeJSON.IsNone)
                {
                    storeJSON.Value = "{}";
                }

                if (!fieldsAdded.IsNone)
                {
                    fieldsAdded.Value = 0;
                }
            }
        }

        private void ParseTextInput(string input, System.Collections.Generic.List<KeyValuePair> fields)
        {
            string[] lines = input.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                string trimmed = line.Trim();

                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//") || trimmed.StartsWith("#"))
                {
                    continue;
                }

                if (trimmed.Contains(":"))
                {
                    string[] parts = trimmed.Split(new[] { ':' }, 2);
                    string key = parts[0].Trim();
                    string value = parts.Length > 1 ? parts[1].Trim() : "";

                    if (skipEmptyKeys.Value && string.IsNullOrEmpty(key))
                    {
                        continue;
                    }

                    if (!allowEmptyValues.Value && string.IsNullOrEmpty(value))
                    {
                        continue;
                    }

                    fields.Add(new KeyValuePair { key = key, value = value });

                    if (logDetails.Value)
                    {
                        Debug.Log($"[JSON Create Multiple] Parsed: '{key}' = '{value}'");
                    }
                }
            }
        }

        private void SetEmptyJSON()
        {
            if (!storeJSON.IsNone)
            {
                storeJSON.Value = prettyPrint.Value ? "{\n}" : "{}";
            }

            if (!fieldsAdded.IsNone)
            {
                fieldsAdded.Value = 0;
            }

            Fsm.Event(emptyEvent);
        }

        private string BuildJSON(System.Collections.Generic.List<KeyValuePair> fields)
        {
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

                sb.Append("\"");
                sb.Append(EscapeString(field.key));
                sb.Append("\":");

                if (prettyPrint.Value)
                {
                    sb.Append(" ");
                }

                sb.Append(FormatValue(field.value));

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
                return "\"" + EscapeString(value) + "\"";
            }

            string trimmed = value.Trim();

            if (trimmed.ToLower() == "null")
            {
                return "null";
            }

            if (trimmed.ToLower() == "true")
            {
                return "true";
            }
            if (trimmed.ToLower() == "false")
            {
                return "false";
            }

            if (IsNumber(trimmed))
            {
                return trimmed;
            }

            if ((trimmed.StartsWith("{") && trimmed.EndsWith("}")) ||
                (trimmed.StartsWith("[") && trimmed.EndsWith("]")))
            {
                return trimmed;
            }

            return "\"" + EscapeString(value) + "\"";
        }

        private bool IsNumber(string value)
        {
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