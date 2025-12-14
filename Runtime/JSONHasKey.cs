/*
 * ═══════════════════════════════════════════════════════════════
 *                          UNITY FORGE
 *                    JSON Has Key Action
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
    [Tooltip("Check if a key exists in JSON without extracting the value.")]
    public class JSONHasKey : FsmStateAction
    {
        [RequiredField]
        [Tooltip("The JSON string to check")]
        [UIHint(UIHint.Variable)]
        public FsmString jsonString;

        [RequiredField]
        [Tooltip("The key name to check for")]
        public FsmString keyName;

        [RequiredField]
        [Tooltip("Store whether the key exists")]
        [UIHint(UIHint.Variable)]
        public FsmBool keyExists;

        [Tooltip("Store the value type if key exists (string, number, boolean, object, array, null)")]
        [UIHint(UIHint.Variable)]
        public FsmString valueType;

        [Tooltip("Event to send if key exists")]
        public FsmEvent keyExistsEvent;

        [Tooltip("Event to send if key does not exist")]
        public FsmEvent keyNotExistsEvent;

        [Tooltip("Log check results to console")]
        public FsmBool logResults = false;

        public override void Reset()
        {
            jsonString = null;
            keyName = null;
            keyExists = null;
            valueType = null;
            keyExistsEvent = null;
            keyNotExistsEvent = null;
            logResults = false;
        }

        public override void OnEnter()
        {
            CheckKey();
            Finish();
        }

        private void CheckKey()
        {
            if (string.IsNullOrEmpty(jsonString.Value))
            {
                if (logResults.Value)
                {
                    Debug.LogWarning("[JSON Has Key] JSON string is empty or null");
                }
                SetKeyNotExists();
                return;
            }

            if (string.IsNullOrEmpty(keyName.Value))
            {
                if (logResults.Value)
                {
                    Debug.LogWarning("[JSON Has Key] Key name is empty or null");
                }
                SetKeyNotExists();
                return;
            }

            try
            {
                bool exists = KeyExists(jsonString.Value, keyName.Value, out string type);

                if (!keyExists.IsNone)
                {
                    keyExists.Value = exists;
                }

                if (!valueType.IsNone)
                {
                    valueType.Value = type;
                }

                if (logResults.Value)
                {
                    if (exists)
                    {
                        Debug.Log($"[JSON Has Key] Key '{keyName.Value}' EXISTS (Type: {type})");
                    }
                    else
                    {
                        Debug.Log($"[JSON Has Key] Key '{keyName.Value}' NOT FOUND");
                    }
                }

                if (exists)
                {
                    Fsm.Event(keyExistsEvent);
                }
                else
                {
                    Fsm.Event(keyNotExistsEvent);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[JSON Has Key] Error: {e.Message}");
                SetKeyNotExists();
            }
        }

        private bool KeyExists(string json, string key, out string type)
        {
            type = "unknown";

            // Find the key in JSON
            string searchPattern = $"\"{key}\"";
            int keyIndex = json.IndexOf(searchPattern);

            if (keyIndex == -1)
            {
                return false;
            }

            // Make sure this is actually a key (followed by a colon)
            // Not just a string value that contains the key name
            int colonIndex = json.IndexOf(':', keyIndex);
            if (colonIndex == -1)
            {
                return false;
            }

            // Verify no other characters between key and colon (except whitespace)
            for (int i = keyIndex + searchPattern.Length; i < colonIndex; i++)
            {
                if (!char.IsWhiteSpace(json[i]))
                {
                    // This might be a false match, keep searching
                    int nextIndex = json.IndexOf(searchPattern, keyIndex + 1);
                    if (nextIndex != -1)
                    {
                        return KeyExists(json.Substring(nextIndex), key, out type);
                    }
                    return false;
                }
            }

            // Skip whitespace after colon
            int valueStart = colonIndex + 1;
            while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart]))
            {
                valueStart++;
            }

            if (valueStart >= json.Length)
            {
                return false;
            }

            // Determine value type
            char firstChar = json[valueStart];

            if (firstChar == '"')
            {
                type = "string";
            }
            else if (firstChar == '{')
            {
                type = "object";
            }
            else if (firstChar == '[')
            {
                type = "array";
            }
            else if (json.Substring(valueStart).StartsWith("null"))
            {
                type = "null";
            }
            else if (json.Substring(valueStart).StartsWith("true") ||
                     json.Substring(valueStart).StartsWith("false"))
            {
                type = "boolean";
            }
            else if (char.IsDigit(firstChar) || firstChar == '-')
            {
                type = "number";
            }

            return true;
        }

        private void SetKeyNotExists()
        {
            if (!keyExists.IsNone)
            {
                keyExists.Value = false;
            }

            if (!valueType.IsNone)
            {
                valueType.Value = "none";
            }

            Fsm.Event(keyNotExistsEvent);
        }
    }
}