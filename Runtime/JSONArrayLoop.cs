/*
 * ═══════════════════════════════════════════════════════════════
 *                          UNITY FORGE
 *                    JSON Array Loop Action
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
    [Tooltip("Loop through all items in a JSON array. Triggers events for each item and when complete.")]
    public class JSONArrayLoop : FsmStateAction
    {
        [RequiredField]
        [Tooltip("The JSON string containing the array")]
        [UIHint(UIHint.Variable)]
        public FsmString jsonString;

        [Tooltip("Key name of the array (leave empty if JSON is already an array)")]
        public FsmString arrayKey;

        [Title("Current Item Output")]
        [RequiredField]
        [Tooltip("Store current item as string")]
        [UIHint(UIHint.Variable)]
        public FsmString currentItem;

        [Tooltip("Store current index (0, 1, 2...)")]
        [UIHint(UIHint.Variable)]
        public FsmInt currentIndex;

        [Title("Loop Info")]
        [Tooltip("Store total number of items")]
        [UIHint(UIHint.Variable)]
        public FsmInt totalCount;

        [Tooltip("Store if loop has more items")]
        [UIHint(UIHint.Variable)]
        public FsmBool hasMoreItems;

        [Title("Events")]
        [RequiredField]
        [Tooltip("Event sent for each item (loop back to process item)")]
        public FsmEvent loopEvent;

        [RequiredField]
        [Tooltip("Event sent when loop is complete")]
        public FsmEvent finishedEvent;

        [Tooltip("Event sent if array is empty or not found")]
        public FsmEvent emptyEvent;

        [Title("Loop Control")]
        [Tooltip("Start from this index (useful for resuming)")]
        public FsmInt startIndex = 0;

        [Tooltip("Maximum items to process (0 = all)")]
        public FsmInt maxItems = 0;

        [Tooltip("Reset loop on state enter")]
        public FsmBool resetOnEnter = true;

        [Title("Debug")]
        [Tooltip("Log loop progress")]
        public FsmBool logProgress = false;

        private string[] items;
        private int index;
        private bool initialized;

        public override void Reset()
        {
            jsonString = null;
            arrayKey = null;
            currentItem = null;
            currentIndex = null;
            totalCount = null;
            hasMoreItems = null;
            loopEvent = null;
            finishedEvent = null;
            emptyEvent = null;
            startIndex = 0;
            maxItems = 0;
            resetOnEnter = true;
            logProgress = false;

            items = null;
            index = 0;
            initialized = false;
        }

        public override void OnEnter()
        {
            if (resetOnEnter.Value || !initialized)
            {
                Initialize();
            }
            else
            {
                ProcessNextItem();
            }
        }

        private void Initialize()
        {
            if (string.IsNullOrEmpty(jsonString.Value))
            {
                if (logProgress.Value)
                {
                    Debug.LogWarning("[JSON Array Loop] JSON string is empty");
                }
                HandleEmpty();
                return;
            }

            try
            {
                string arrayString = "";

                // Extract array if key is provided
                if (!string.IsNullOrEmpty(arrayKey.Value))
                {
                    arrayString = ExtractArray(jsonString.Value, arrayKey.Value);

                    if (string.IsNullOrEmpty(arrayString))
                    {
                        if (logProgress.Value)
                        {
                            Debug.LogWarning($"[JSON Array Loop] Array key '{arrayKey.Value}' not found");
                        }
                        HandleEmpty();
                        return;
                    }
                }
                else
                {
                    arrayString = jsonString.Value.Trim();
                }

                // Parse array into items
                items = ParseArrayItems(arrayString);

                if (items == null || items.Length == 0)
                {
                    if (logProgress.Value)
                    {
                        Debug.LogWarning("[JSON Array Loop] Array is empty");
                    }
                    HandleEmpty();
                    return;
                }

                // Store total count
                if (!totalCount.IsNone)
                {
                    totalCount.Value = items.Length;
                }

                // Set starting index
                index = Mathf.Max(0, startIndex.Value);

                // Check if we should limit items
                if (maxItems.Value > 0 && items.Length > maxItems.Value)
                {
                    string[] limited = new string[maxItems.Value];
                    System.Array.Copy(items, limited, maxItems.Value);
                    items = limited;
                }

                initialized = true;

                if (logProgress.Value)
                {
                    Debug.Log($"[JSON Array Loop] Starting loop with {items.Length} items");
                }

                ProcessNextItem();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[JSON Array Loop] Error: {e.Message}");
                HandleEmpty();
            }
        }

        private void ProcessNextItem()
        {
            if (items == null || index >= items.Length)
            {
                HandleFinished();
                return;
            }

            // Store current item
            if (!currentItem.IsNone)
            {
                currentItem.Value = items[index];
            }

            // Store current index
            if (!currentIndex.IsNone)
            {
                currentIndex.Value = index;
            }

            // Store has more items
            if (!hasMoreItems.IsNone)
            {
                hasMoreItems.Value = (index + 1) < items.Length;
            }

            if (logProgress.Value)
            {
                string preview = items[index].Length > 50 ?
                    items[index].Substring(0, 50) + "..." : items[index];
                Debug.Log($"[JSON Array Loop] Item {index + 1}/{items.Length}: {preview}");
            }

            // Increment for next iteration
            index++;

            // Trigger loop event
            Fsm.Event(loopEvent);
            Finish();
        }

        private void HandleFinished()
        {
            if (logProgress.Value)
            {
                Debug.Log($"[JSON Array Loop] Loop complete! Processed {items?.Length ?? 0} items");
            }

            // Reset for next time
            if (resetOnEnter.Value)
            {
                initialized = false;
                items = null;
                index = 0;
            }

            Fsm.Event(finishedEvent);
            Finish();
        }

        private void HandleEmpty()
        {
            if (!totalCount.IsNone)
            {
                totalCount.Value = 0;
            }

            if (!currentItem.IsNone)
            {
                currentItem.Value = "";
            }

            if (!currentIndex.IsNone)
            {
                currentIndex.Value = -1;
            }

            if (!hasMoreItems.IsNone)
            {
                hasMoreItems.Value = false;
            }

            initialized = false;
            items = null;
            index = 0;

            Fsm.Event(emptyEvent);
            Finish();
        }

        private string ExtractArray(string json, string key)
        {
            string searchPattern = $"\"{key}\"";
            int keyIndex = json.IndexOf(searchPattern);

            if (keyIndex == -1)
            {
                return "";
            }

            int colonIndex = json.IndexOf(':', keyIndex);
            if (colonIndex == -1)
            {
                return "";
            }

            int valueStart = colonIndex + 1;
            while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart]))
            {
                valueStart++;
            }

            if (valueStart >= json.Length || json[valueStart] != '[')
            {
                return "";
            }

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

        private string[] ParseArrayItems(string arrayString)
        {
            arrayString = arrayString.Trim();

            if (string.IsNullOrEmpty(arrayString) || !arrayString.StartsWith("[") || !arrayString.EndsWith("]"))
            {
                return null;
            }

            string content = arrayString.Substring(1, arrayString.Length - 2).Trim();
            if (string.IsNullOrEmpty(content))
            {
                return new string[0];
            }

            var itemList = new System.Collections.Generic.List<string>();
            int depth = 0;
            bool inString = false;
            bool escaped = false;
            int itemStart = 1;

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
                    string item = arrayString.Substring(itemStart, i - itemStart).Trim();
                    itemList.Add(item);
                    itemStart = i + 1;
                }
            }

            // Add last item
            string lastItem = arrayString.Substring(itemStart, arrayString.Length - 1 - itemStart).Trim();
            itemList.Add(lastItem);

            return itemList.ToArray();
        }
    }
}