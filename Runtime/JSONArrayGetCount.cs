/*
 * ═══════════════════════════════════════════════════════════════
 *                          UNITY FORGE
 *                    JSON Array Get Count Action
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
    [Tooltip("Gets the count of items in a JSON array. Can extract array by key or use full JSON if it's already an array.")]
    public class JSONArrayGetCount : FsmStateAction
    {
        [RequiredField]
        [Tooltip("The JSON string containing the array")]
        [UIHint(UIHint.Variable)]
        public FsmString jsonString;

        [Tooltip("Key name of the array (leave empty if JSON is already an array)")]
        public FsmString arrayKey;

        [RequiredField]
        [Tooltip("Store the array count")]
        [UIHint(UIHint.Variable)]
        public FsmInt arrayCount;

        [Tooltip("Store whether array was found")]
        [UIHint(UIHint.Variable)]
        public FsmBool arrayFound;

        [Tooltip("Store the array as string (for further processing)")]
        [UIHint(UIHint.Variable)]
        public FsmString storeArrayString;

        [Tooltip("Event to send if array is found")]
        public FsmEvent foundEvent;

        [Tooltip("Event to send if array is not found or empty")]
        public FsmEvent notFoundEvent;

        [Tooltip("Event to send if array is empty (count = 0)")]
        public FsmEvent emptyArrayEvent;

        [Tooltip("Log details to console")]
        public FsmBool logDetails = false;

        public override void Reset()
        {
            jsonString = null;
            arrayKey = null;
            arrayCount = null;
            arrayFound = null;
            storeArrayString = null;
            foundEvent = null;
            notFoundEvent = null;
            emptyArrayEvent = null;
            logDetails = false;
        }

        public override void OnEnter()
        {
            CountArray();
            Finish();
        }

        private void CountArray()
        {
            if (string.IsNullOrEmpty(jsonString.Value))
            {
                if (logDetails.Value)
                {
                    Debug.LogWarning("[JSON Array Count] JSON string is empty or null");
                }
                SetNotFound();
                return;
            }

            try
            {
                string arrayString = "";

                // If arrayKey is provided, extract the array first
                if (!string.IsNullOrEmpty(arrayKey.Value))
                {
                    arrayString = ExtractArray(jsonString.Value, arrayKey.Value);

                    if (string.IsNullOrEmpty(arrayString))
                    {
                        if (logDetails.Value)
                        {
                            Debug.LogWarning($"[JSON Array Count] Array key '{arrayKey.Value}' not found");
                        }
                        SetNotFound();
                        return;
                    }
                }
                else
                {
                    // Use the full JSON string as array
                    arrayString = jsonString.Value.Trim();
                }

                // Count items in the array
                int count = CountArrayItems(arrayString);

                if (!arrayCount.IsNone)
                {
                    arrayCount.Value = count;
                }

                if (!arrayFound.IsNone)
                {
                    arrayFound.Value = true;
                }

                if (!storeArrayString.IsNone)
                {
                    storeArrayString.Value = arrayString;
                }

                if (logDetails.Value)
                {
                    string keyInfo = string.IsNullOrEmpty(arrayKey.Value) ? "root array" : $"key '{arrayKey.Value}'";
                    Debug.Log($"[JSON Array Count] Found {count} items in {keyInfo}");
                }

                // Send appropriate event
                if (count == 0)
                {
                    Fsm.Event(emptyArrayEvent);
                }
                else
                {
                    Fsm.Event(foundEvent);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[JSON Array Count] Error: {e.Message}");
                SetNotFound();
            }
        }

        private string ExtractArray(string json, string key)
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

            if (valueStart >= json.Length || json[valueStart] != '[')
            {
                return ""; // Not an array
            }

            // Extract the full array
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

        private int CountArrayItems(string arrayString)
        {
            arrayString = arrayString.Trim();

            // Check if it's actually an array
            if (string.IsNullOrEmpty(arrayString) || !arrayString.StartsWith("[") || !arrayString.EndsWith("]"))
            {
                return 0;
            }

            // Empty array check
            string content = arrayString.Substring(1, arrayString.Length - 2).Trim();
            if (string.IsNullOrEmpty(content))
            {
                return 0;
            }

            int count = 0;
            int depth = 0;
            bool inString = false;
            bool escaped = false;

            // Count commas at depth 0 (top level of array)
            for (int i = 1; i < arrayString.Length - 1; i++)
            {
                char c = arrayString[i];

                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (inString)
                {
                    continue;
                }

                if (c == '[' || c == '{')
                {
                    depth++;
                }
                else if (c == ']' || c == '}')
                {
                    depth--;
                }
                else if (c == ',' && depth == 0)
                {
                    count++;
                }
            }

            // Count is commas + 1 (if there's any content)
            return count + 1;
        }

        private void SetNotFound()
        {
            if (!arrayCount.IsNone)
            {
                arrayCount.Value = 0;
            }

            if (!arrayFound.IsNone)
            {
                arrayFound.Value = false;
            }

            if (!storeArrayString.IsNone)
            {
                storeArrayString.Value = "";
            }

            Fsm.Event(notFoundEvent);
        }
    }
}