/*
 * ═══════════════════════════════════════════════════════════════
 *                          UNITY FORGE
 *                    JSON Get Multiple Values Action
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
    [Tooltip("Extract multiple values from JSON at once using key names array.")]
    public class JSONGetMultipleValues : FsmStateAction
    {
        [RequiredField]
        [Tooltip("The JSON string to parse")]
        [UIHint(UIHint.Variable)]
        public FsmString jsonString;

        [RequiredField]
        [Tooltip("Array of key names to extract")]
        [ArrayEditor(VariableType.String)]
        public FsmArray keyNames;

        [RequiredField]
        [Tooltip("Array to store extracted values (same order as keys)")]
        [ArrayEditor(VariableType.String)]
        [UIHint(UIHint.Variable)]
        public FsmArray outputValues;

        [Title("Status")]
        [Tooltip("Store how many keys were found")]
        [UIHint(UIHint.Variable)]
        public FsmInt keysFound;

        [Tooltip("Store array of booleans indicating which keys exist")]
        [ArrayEditor(VariableType.Bool)]
        [UIHint(UIHint.Variable)]
        public FsmArray keyExistsFlags;

        [Title("Options")]
        [Tooltip("Stop on first missing key")]
        public FsmBool stopOnMissing = false;

        [Tooltip("Use empty string for missing keys (instead of 'NOT_FOUND')")]
        public FsmBool emptyForMissing = true;

        [Title("Events")]
        [Tooltip("Event sent when all keys are extracted")]
        public FsmEvent successEvent;

        [Tooltip("Event sent if any key is missing")]
        public FsmEvent missingKeyEvent;

        [Tooltip("Event sent if all keys are found")]
        public FsmEvent allFoundEvent;

        [Title("Debug")]
        [Tooltip("Log extraction details")]
        public FsmBool logDetails = false;

        public override void Reset()
        {
            jsonString = null;
            keyNames = null;
            outputValues = null;
            keysFound = null;
            keyExistsFlags = null;
            stopOnMissing = false;
            emptyForMissing = true;
            successEvent = null;
            missingKeyEvent = null;
            allFoundEvent = null;
            logDetails = false;
        }

        public override void OnEnter()
        {
            ExtractMultipleValues();
            Finish();
        }

        private void ExtractMultipleValues()
        {
            if (string.IsNullOrEmpty(jsonString.Value))
            {
                if (logDetails.Value)
                {
                    Debug.LogWarning("[JSON Get Multiple] JSON string is empty");
                }
                SetAllMissing();
                return;
            }

            if (keyNames == null || keyNames.Length == 0)
            {
                if (logDetails.Value)
                {
                    Debug.LogWarning("[JSON Get Multiple] Key names array is empty");
                }
                SetAllMissing();
                return;
            }

            try
            {
                int totalKeys = keyNames.Length;
                int foundCount = 0;
                bool anyMissing = false;

                // Resize output arrays to match key count
                outputValues.Resize(totalKeys);
                if (!keyExistsFlags.IsNone)
                {
                    keyExistsFlags.Resize(totalKeys);
                }

                if (logDetails.Value)
                {
                    Debug.Log($"[JSON Get Multiple] Extracting {totalKeys} keys...");
                }

                // Extract each key
                for (int i = 0; i < totalKeys; i++)
                {
                    object keyObj = keyNames.Get(i);
                    string keyName = keyObj?.ToString() ?? "";

                    if (string.IsNullOrEmpty(keyName))
                    {
                        if (logDetails.Value)
                        {
                            Debug.LogWarning($"[JSON Get Multiple] Key at index {i} is empty");
                        }
                        outputValues.Set(i, emptyForMissing.Value ? "" : "NOT_FOUND");
                        if (!keyExistsFlags.IsNone)
                        {
                            keyExistsFlags.Set(i, false);
                        }
                        anyMissing = true;
                        continue;
                    }

                    // Extract value
                    string value = ExtractJSONValue(jsonString.Value, keyName);

                    if (!string.IsNullOrEmpty(value))
                    {
                        outputValues.Set(i, value);
                        if (!keyExistsFlags.IsNone)
                        {
                            keyExistsFlags.Set(i, true);
                        }
                        foundCount++;

                        if (logDetails.Value)
                        {
                            string preview = value.Length > 50 ? value.Substring(0, 50) + "..." : value;
                            Debug.Log($"[JSON Get Multiple] ✓ '{keyName}' = '{preview}'");
                        }
                    }
                    else
                    {
                        outputValues.Set(i, emptyForMissing.Value ? "" : "NOT_FOUND");
                        if (!keyExistsFlags.IsNone)
                        {
                            keyExistsFlags.Set(i, false);
                        }
                        anyMissing = true;

                        if (logDetails.Value)
                        {
                            Debug.LogWarning($"[JSON Get Multiple] ✗ '{keyName}' NOT FOUND");
                        }

                        // Stop if option is enabled
                        if (stopOnMissing.Value)
                        {
                            if (logDetails.Value)
                            {
                                Debug.LogWarning("[JSON Get Multiple] Stopped due to missing key");
                            }
                            break;
                        }
                    }
                }

                // Store found count
                if (!keysFound.IsNone)
                {
                    keysFound.Value = foundCount;
                }

                if (logDetails.Value)
                {
                    Debug.Log($"[JSON Get Multiple] Complete: {foundCount}/{totalKeys} keys found");
                }

                // Trigger appropriate events
                if (foundCount == totalKeys)
                {
                    Fsm.Event(allFoundEvent);
                }
                else if (anyMissing)
                {
                    Fsm.Event(missingKeyEvent);
                }

                Fsm.Event(successEvent);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[JSON Get Multiple] Error: {e.Message}");
                SetAllMissing();
            }
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

                // Read until we hit a delimiter
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

        private void SetAllMissing()
        {
            int count = keyNames != null ? keyNames.Length : 0;

            if (count > 0)
            {
                outputValues.Resize(count);
                if (!keyExistsFlags.IsNone)
                {
                    keyExistsFlags.Resize(count);
                }

                for (int i = 0; i < count; i++)
                {
                    outputValues.Set(i, emptyForMissing.Value ? "" : "NOT_FOUND");
                    if (!keyExistsFlags.IsNone)
                    {
                        keyExistsFlags.Set(i, false);
                    }
                }
            }

            if (!keysFound.IsNone)
            {
                keysFound.Value = 0;
            }

            Fsm.Event(missingKeyEvent);
        }
    }
}