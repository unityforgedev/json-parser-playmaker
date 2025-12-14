/*
 * ═══════════════════════════════════════════════════════════════
 *                          UNITY FORGE
 *                    JSON Get Value Action
 * ═══════════════════════════════════════════════════════════════
 * 
 * Author: Unity Forge
 * Github: https://github.com/unityforgedev
 * 
 */

using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("JSON")]
    [Tooltip("Extracts a value from JSON string by key name. Supports multiple output types.")]
    public class JSONGetValue : FsmStateAction
    {
        [RequiredField]
        [Tooltip("The JSON string to parse")]
        [UIHint(UIHint.Variable)]
        public FsmString jsonString;

        [RequiredField]
        [Tooltip("The key name to extract (e.g., 'login', 'id', 'name')")]
        public FsmString keyName;

        [Title("Output Type")]
        [Tooltip("Choose the output variable type")]
        public OutputType outputType = OutputType.String;

        [Title("Store Results")]
        [Tooltip("Store the extracted value as STRING")]
        [UIHint(UIHint.Variable)]
        public FsmString storeAsString;

        [Tooltip("Store the extracted value as INT")]
        [UIHint(UIHint.Variable)]
        public FsmInt storeAsInt;

        [Tooltip("Store the extracted value as FLOAT")]
        [UIHint(UIHint.Variable)]
        public FsmFloat storeAsFloat;

        [Tooltip("Store the extracted value as BOOL")]
        [UIHint(UIHint.Variable)]
        public FsmBool storeAsBool;

        [Title("Status")]
        [Tooltip("Store whether the key was found")]
        [UIHint(UIHint.Variable)]
        public FsmBool keyFound;

        [Tooltip("Event to send if key is found")]
        public FsmEvent foundEvent;

        [Tooltip("Event to send if key is not found")]
        public FsmEvent notFoundEvent;

        [Title("Debug")]
        [Tooltip("Log extraction details to console")]
        public FsmBool logDetails = false;

        public enum OutputType
        {
            String,
            Int,
            Float,
            Bool,
            Auto
        }

        public override void Reset()
        {
            jsonString = null;
            keyName = null;
            outputType = OutputType.String;
            storeAsString = null;
            storeAsInt = null;
            storeAsFloat = null;
            storeAsBool = null;
            keyFound = null;
            foundEvent = null;
            notFoundEvent = null;
            logDetails = false;
        }

        public override void OnEnter()
        {
            ExtractValue();
            Finish();
        }

        private void ExtractValue()
        {
            if (string.IsNullOrEmpty(jsonString.Value))
            {
                if (logDetails.Value)
                {
                    Debug.LogWarning("[JSON Get Value] JSON string is empty or null");
                }

                SetKeyNotFound();
                return;
            }

            if (string.IsNullOrEmpty(keyName.Value))
            {
                if (logDetails.Value)
                {
                    Debug.LogWarning("[JSON Get Value] Key name is empty or null");
                }

                SetKeyNotFound();
                return;
            }

            try
            {
                string value = ExtractJSONValue(jsonString.Value, keyName.Value);

                if (!string.IsNullOrEmpty(value))
                {
                    // Store value based on output type
                    StoreValue(value);

                    if (!keyFound.IsNone)
                    {
                        keyFound.Value = true;
                    }

                    if (logDetails.Value)
                    {
                        Debug.Log($"[JSON Get Value] Key '{keyName.Value}' = '{value}' (Type: {outputType})");
                    }

                    Fsm.Event(foundEvent);
                }
                else
                {
                    if (logDetails.Value)
                    {
                        Debug.LogWarning($"[JSON Get Value] Key '{keyName.Value}' not found in JSON");
                    }

                    SetKeyNotFound();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[JSON Get Value] Error parsing JSON: {e.Message}");
                SetKeyNotFound();
            }
        }

        private void StoreValue(string value)
        {
            switch (outputType)
            {
                case OutputType.String:
                    if (!storeAsString.IsNone)
                    {
                        storeAsString.Value = value;
                    }
                    break;

                case OutputType.Int:
                    if (!storeAsInt.IsNone)
                    {
                        if (int.TryParse(value, out int intValue))
                        {
                            storeAsInt.Value = intValue;
                        }
                        else
                        {
                            Debug.LogWarning($"[JSON Get Value] Cannot convert '{value}' to Int");
                            storeAsInt.Value = 0;
                        }
                    }
                    break;

                case OutputType.Float:
                    if (!storeAsFloat.IsNone)
                    {
                        if (float.TryParse(value, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float floatValue))
                        {
                            storeAsFloat.Value = floatValue;
                        }
                        else
                        {
                            Debug.LogWarning($"[JSON Get Value] Cannot convert '{value}' to Float");
                            storeAsFloat.Value = 0f;
                        }
                    }
                    break;

                case OutputType.Bool:
                    if (!storeAsBool.IsNone)
                    {
                        string lowerValue = value.ToLower();
                        if (lowerValue == "true" || lowerValue == "1")
                        {
                            storeAsBool.Value = true;
                        }
                        else if (lowerValue == "false" || lowerValue == "0" || lowerValue == "null")
                        {
                            storeAsBool.Value = false;
                        }
                        else
                        {
                            // Non-empty strings are considered true, empty/null are false
                            storeAsBool.Value = !string.IsNullOrEmpty(value);
                        }
                    }
                    break;

                case OutputType.Auto:
                    // Try to detect and store in all possible types
                    if (!storeAsString.IsNone)
                    {
                        storeAsString.Value = value;
                    }

                    if (!storeAsInt.IsNone && int.TryParse(value, out int autoInt))
                    {
                        storeAsInt.Value = autoInt;
                    }

                    if (!storeAsFloat.IsNone && float.TryParse(value, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float autoFloat))
                    {
                        storeAsFloat.Value = autoFloat;
                    }

                    if (!storeAsBool.IsNone)
                    {
                        string lowerValue = value.ToLower();
                        if (lowerValue == "true" || lowerValue == "1")
                        {
                            storeAsBool.Value = true;
                        }
                        else if (lowerValue == "false" || lowerValue == "0" || lowerValue == "null")
                        {
                            storeAsBool.Value = false;
                        }
                        else
                        {
                            storeAsBool.Value = !string.IsNullOrEmpty(value);
                        }
                    }
                    break;
            }
        }

        private void SetKeyNotFound()
        {
            // Clear all output variables
            if (!storeAsString.IsNone)
            {
                storeAsString.Value = "";
            }

            if (!storeAsInt.IsNone)
            {
                storeAsInt.Value = 0;
            }

            if (!storeAsFloat.IsNone)
            {
                storeAsFloat.Value = 0f;
            }

            if (!storeAsBool.IsNone)
            {
                storeAsBool.Value = false;
            }

            if (!keyFound.IsNone)
            {
                keyFound.Value = false;
            }

            Fsm.Event(notFoundEvent);
        }

        private string ExtractJSONValue(string json, string key)
        {
            // Find the key in JSON
            string searchPattern = $"\"{key}\"";
            int keyIndex = json.IndexOf(searchPattern);

            if (keyIndex == -1)
            {
                return "";
            }

            // Find the colon after the key
            int colonIndex = json.IndexOf(':', keyIndex);
            if (colonIndex == -1)
            {
                return "";
            }

            // Skip whitespace after colon
            int valueStart = colonIndex + 1;
            while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart]))
            {
                valueStart++;
            }

            if (valueStart >= json.Length)
            {
                return "";
            }

            // Check the type of value
            char firstChar = json[valueStart];

            // STRING VALUE (quoted)
            if (firstChar == '"')
            {
                valueStart++; // Skip opening quote
                int valueEnd = valueStart;

                // Find closing quote (handle escaped quotes)
                while (valueEnd < json.Length)
                {
                    if (json[valueEnd] == '"' && (valueEnd == 0 || json[valueEnd - 1] != '\\'))
                    {
                        break;
                    }
                    valueEnd++;
                }

                if (valueEnd < json.Length)
                {
                    return json.Substring(valueStart, valueEnd - valueStart);
                }
                return "";
            }
            // NULL VALUE
            else if (json.Substring(valueStart).StartsWith("null"))
            {
                return "null";
            }
            // BOOLEAN VALUE
            else if (json.Substring(valueStart).StartsWith("true"))
            {
                return "true";
            }
            else if (json.Substring(valueStart).StartsWith("false"))
            {
                return "false";
            }
            // OBJECT VALUE
            else if (firstChar == '{')
            {
                int braceCount = 1;
                int i = valueStart + 1;

                while (i < json.Length && braceCount > 0)
                {
                    if (json[i] == '{') braceCount++;
                    else if (json[i] == '}') braceCount--;
                    i++;
                }

                return json.Substring(valueStart, i - valueStart);
            }
            // ARRAY VALUE
            else if (firstChar == '[')
            {
                int bracketCount = 1;
                int i = valueStart + 1;

                while (i < json.Length && bracketCount > 0)
                {
                    if (json[i] == '[') bracketCount++;
                    else if (json[i] == ']') bracketCount--;
                    i++;
                }

                return json.Substring(valueStart, i - valueStart);
            }
            // NUMBER VALUE
            else if (char.IsDigit(firstChar) || firstChar == '-')
            {
                int valueEnd = valueStart;

                // Read until we hit a delimiter (comma, brace, bracket, or whitespace)
                while (valueEnd < json.Length)
                {
                    char c = json[valueEnd];
                    if (c == ',' || c == '}' || c == ']' || char.IsWhiteSpace(c))
                    {
                        break;
                    }
                    valueEnd++;
                }

                return json.Substring(valueStart, valueEnd - valueStart).Trim();
            }

            return "";
        }
    }
}