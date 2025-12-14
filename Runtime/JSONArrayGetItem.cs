/*
 * ═══════════════════════════════════════════════════════════════
 *                          UNITY FORGE
 *                    JSON Array Get Item Action
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
    [Tooltip("Gets a specific item from a JSON array by index. Can extract array by key or use full JSON if it's already an array.")]
    public class JSONArrayGetItem : FsmStateAction
    {
        [RequiredField]
        [Tooltip("The JSON string containing the array")]
        [UIHint(UIHint.Variable)]
        public FsmString jsonString;

        [Tooltip("Key name of the array (leave empty if JSON is already an array)")]
        public FsmString arrayKey;

        [RequiredField]
        [Tooltip("Index of the item to get (0 = first item, 1 = second item, etc.)")]
        public FsmInt itemIndex;

        [RequiredField]
        [Tooltip("Store the item as string")]
        [UIHint(UIHint.Variable)]
        public FsmString storeItem;

        [Tooltip("Store whether item was found")]
        [UIHint(UIHint.Variable)]
        public FsmBool itemFound;

        [Tooltip("Store the total array count")]
        [UIHint(UIHint.Variable)]
        public FsmInt totalCount;

        [Tooltip("Event to send if item is found")]
        public FsmEvent foundEvent;

        [Tooltip("Event to send if item is not found (index out of range)")]
        public FsmEvent notFoundEvent;

        [Tooltip("Event to send if array is empty")]
        public FsmEvent emptyArrayEvent;

        [Tooltip("Log details to console")]
        public FsmBool logDetails = false;

        public override void Reset()
        {
            jsonString = null;
            arrayKey = null;
            itemIndex = 0;
            storeItem = null;
            itemFound = null;
            totalCount = null;
            foundEvent = null;
            notFoundEvent = null;
            emptyArrayEvent = null;
            logDetails = false;
        }

        public override void OnEnter()
        {
            GetArrayItem();
            Finish();
        }

        private void GetArrayItem()
        {
            if (string.IsNullOrEmpty(jsonString.Value))
            {
                if (logDetails.Value)
                {
                    Debug.LogWarning("[JSON Array Get Item] JSON string is empty or null");
                }
                SetNotFound();
                return;
            }

            if (itemIndex.Value < 0)
            {
                if (logDetails.Value)
                {
                    Debug.LogWarning("[JSON Array Get Item] Index cannot be negative");
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
                            Debug.LogWarning($"[JSON Array Get Item] Array key '{arrayKey.Value}' not found");
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

                // Get the item at specified index
                string item = GetItemAtIndex(arrayString, itemIndex.Value, out int count);

                // Store total count
                if (!totalCount.IsNone)
                {
                    totalCount.Value = count;
                }

                if (count == 0)
                {
                    if (logDetails.Value)
                    {
                        Debug.LogWarning("[JSON Array Get Item] Array is empty");
                    }
                    SetNotFound();
                    Fsm.Event(emptyArrayEvent);
                    return;
                }

                if (!string.IsNullOrEmpty(item))
                {
                    if (!storeItem.IsNone)
                    {
                        storeItem.Value = item;
                    }

                    if (!itemFound.IsNone)
                    {
                        itemFound.Value = true;
                    }

                    if (logDetails.Value)
                    {
                        string preview = item.Length > 100 ? item.Substring(0, 100) + "..." : item;
                        Debug.Log($"[JSON Array Get Item] Item at index {itemIndex.Value}: {preview}");
                    }

                    Fsm.Event(foundEvent);
                }
                else
                {
                    if (logDetails.Value)
                    {
                        Debug.LogWarning($"[JSON Array Get Item] Index {itemIndex.Value} is out of range (Array has {count} items)");
                    }
                    SetNotFound();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[JSON Array Get Item] Error: {e.Message}");
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

        private string GetItemAtIndex(string arrayString, int targetIndex, out int totalCount)
        {
            totalCount = 0;
            arrayString = arrayString.Trim();

            // Check if it's actually an array
            if (string.IsNullOrEmpty(arrayString) || !arrayString.StartsWith("[") || !arrayString.EndsWith("]"))
            {
                return "";
            }

            // Empty array check
            string content = arrayString.Substring(1, arrayString.Length - 2).Trim();
            if (string.IsNullOrEmpty(content))
            {
                return "";
            }

            int currentIndex = 0;
            int depth = 0;
            bool inString = false;
            bool escaped = false;
            int itemStart = 1; // Start after opening bracket

            // Parse through the array
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
                    // Found a comma at top level - extract this item
                    if (currentIndex == targetIndex)
                    {
                        totalCount = CountTotalItems(arrayString);
                        return arrayString.Substring(itemStart, i - itemStart).Trim();
                    }

                    currentIndex++;
                    itemStart = i + 1;
                }
            }

            // Get the last item (or only item if no commas)
            totalCount = CountTotalItems(arrayString);

            if (currentIndex == targetIndex)
            {
                return arrayString.Substring(itemStart, arrayString.Length - 1 - itemStart).Trim();
            }

            return ""; // Index out of range
        }

        private int CountTotalItems(string arrayString)
        {
            arrayString = arrayString.Trim();

            if (string.IsNullOrEmpty(arrayString) || !arrayString.StartsWith("[") || !arrayString.EndsWith("]"))
            {
                return 0;
            }

            string content = arrayString.Substring(1, arrayString.Length - 2).Trim();
            if (string.IsNullOrEmpty(content))
            {
                return 0;
            }

            int count = 0;
            int depth = 0;
            bool inString = false;
            bool escaped = false;

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

            return count + 1;
        }

        private void SetNotFound()
        {
            if (!storeItem.IsNone)
            {
                storeItem.Value = "";
            }

            if (!itemFound.IsNone)
            {
                itemFound.Value = false;
            }

            Fsm.Event(notFoundEvent);
        }
    }
}