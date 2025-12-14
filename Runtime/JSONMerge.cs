/*
 * ═══════════════════════════════════════════════════════════════
 *                          UNITY FORGE
 *                    JSON Merge Action
 * ═══════════════════════════════════════════════════════════════
 * 
 * Author: Unity Forge
 * Github: https://github.com/unityforgedev
 * 
 */

using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("JSON")]
    [Tooltip("Merge two or more JSON objects together. Later objects override earlier ones.")]
    public class JSONMerge : FsmStateAction
    {
        [Title("JSON Objects to Merge")]
        [RequiredField]
        [Tooltip("First JSON object (base)")]
        [UIHint(UIHint.Variable)]
        public FsmString jsonObject1;

        [RequiredField]
        [Tooltip("Second JSON object (overrides first)")]
        [UIHint(UIHint.Variable)]
        public FsmString jsonObject2;

        [Tooltip("Third JSON object (optional)")]
        [UIHint(UIHint.Variable)]
        public FsmString jsonObject3;

        [Tooltip("Fourth JSON object (optional)")]
        [UIHint(UIHint.Variable)]
        public FsmString jsonObject4;

        [Tooltip("Fifth JSON object (optional)")]
        [UIHint(UIHint.Variable)]
        public FsmString jsonObject5;

        [Title("Output")]
        [RequiredField]
        [Tooltip("Store the merged JSON")]
        [UIHint(UIHint.Variable)]
        public FsmString mergedJSON;

        [Title("Options")]
        [Tooltip("Merge mode")]
        public MergeMode mergeMode = MergeMode.Override;

        [Tooltip("Pretty print output")]
        public FsmBool prettyPrint = false;

        [Tooltip("Deep merge nested objects")]
        public FsmBool deepMerge = true;

        [Title("Status")]
        [Tooltip("Store number of objects merged")]
        [UIHint(UIHint.Variable)]
        public FsmInt objectsMerged;

        [Tooltip("Store total keys in result")]
        [UIHint(UIHint.Variable)]
        public FsmInt totalKeys;

        [Title("Events")]
        [Tooltip("Event sent when merge is complete")]
        public FsmEvent successEvent;

        [Tooltip("Event sent if merge fails")]
        public FsmEvent errorEvent;

        [Title("Debug")]
        [Tooltip("Log merge process")]
        public FsmBool logProcess = false;

        public enum MergeMode
        {
            Override,      // Later values replace earlier ones
            KeepFirst,     // Keep first value, ignore duplicates
            Append         // For arrays, append instead of replace
        }

        public override void Reset()
        {
            jsonObject1 = null;
            jsonObject2 = null;
            jsonObject3 = null;
            jsonObject4 = null;
            jsonObject5 = null;
            mergedJSON = null;
            mergeMode = MergeMode.Override;
            prettyPrint = false;
            deepMerge = true;
            objectsMerged = null;
            totalKeys = null;
            successEvent = null;
            errorEvent = null;
            logProcess = false;
        }

        public override void OnEnter()
        {
            MergeJSONObjects();
            Finish();
        }

        private void MergeJSONObjects()
        {
            try
            {
                var jsonObjects = new List<string>();

                // Collect all non-empty JSON objects
                if (!string.IsNullOrEmpty(jsonObject1.Value))
                    jsonObjects.Add(jsonObject1.Value);
                if (!string.IsNullOrEmpty(jsonObject2.Value))
                    jsonObjects.Add(jsonObject2.Value);
                if (!string.IsNullOrEmpty(jsonObject3.Value))
                    jsonObjects.Add(jsonObject3.Value);
                if (!string.IsNullOrEmpty(jsonObject4.Value))
                    jsonObjects.Add(jsonObject4.Value);
                if (!string.IsNullOrEmpty(jsonObject5.Value))
                    jsonObjects.Add(jsonObject5.Value);

                if (jsonObjects.Count == 0)
                {
                    if (logProcess.Value)
                    {
                        Debug.LogWarning("[JSON Merge] No valid JSON objects to merge");
                    }

                    if (!mergedJSON.IsNone)
                    {
                        mergedJSON.Value = "{}";
                    }

                    Fsm.Event(errorEvent);
                    return;
                }

                if (logProcess.Value)
                {
                    Debug.Log($"[JSON Merge] Merging {jsonObjects.Count} JSON objects...");
                }

                // Parse all objects
                var parsedObjects = new List<Dictionary<string, object>>();
                foreach (var json in jsonObjects)
                {
                    var parsed = ParseJSON(json);
                    if (parsed != null)
                    {
                        parsedObjects.Add(parsed);
                    }
                }

                if (parsedObjects.Count == 0)
                {
                    if (logProcess.Value)
                    {
                        Debug.LogWarning("[JSON Merge] Failed to parse any JSON objects");
                    }

                    if (!mergedJSON.IsNone)
                    {
                        mergedJSON.Value = "{}";
                    }

                    Fsm.Event(errorEvent);
                    return;
                }

                // Merge all objects
                var result = new Dictionary<string, object>();
                foreach (var obj in parsedObjects)
                {
                    MergeDictionaries(result, obj);
                }

                // Store stats
                if (!objectsMerged.IsNone)
                {
                    objectsMerged.Value = parsedObjects.Count;
                }

                if (!totalKeys.IsNone)
                {
                    totalKeys.Value = result.Count;
                }

                // Build output JSON
                string outputJSON = BuildJSON(result);

                if (!mergedJSON.IsNone)
                {
                    mergedJSON.Value = outputJSON;
                }

                if (logProcess.Value)
                {
                    Debug.Log($"[JSON Merge] Success! Merged {parsedObjects.Count} objects with {result.Count} total keys");
                    if (prettyPrint.Value)
                    {
                        Debug.Log($"[JSON Merge] Result:\n{outputJSON}");
                    }
                }

                Fsm.Event(successEvent);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[JSON Merge] Error: {e.Message}");

                if (!mergedJSON.IsNone)
                {
                    mergedJSON.Value = "{}";
                }

                Fsm.Event(errorEvent);
            }
        }

        private Dictionary<string, object> ParseJSON(string json)
        {
            json = json.Trim();

            if (!json.StartsWith("{") || !json.EndsWith("}"))
            {
                if (logProcess.Value)
                {
                    Debug.LogWarning("[JSON Merge] Invalid JSON object (must start with { and end with })");
                }
                return null;
            }

            var result = new Dictionary<string, object>();
            string content = json.Substring(1, json.Length - 2).Trim();

            if (string.IsNullOrEmpty(content))
            {
                return result; // Empty object
            }

            // Parse key-value pairs
            int depth = 0;
            bool inString = false;
            bool escaped = false;
            int pairStart = 0;

            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];

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

                if (c == '{' || c == '[')
                {
                    depth++;
                }
                else if (c == '}' || c == ']')
                {
                    depth--;
                }
                else if (c == ',' && depth == 0)
                {
                    ParseKeyValuePair(content.Substring(pairStart, i - pairStart), result);
                    pairStart = i + 1;
                }
            }

            // Parse last pair
            ParseKeyValuePair(content.Substring(pairStart), result);

            return result;
        }

        private void ParseKeyValuePair(string pair, Dictionary<string, object> dict)
        {
            pair = pair.Trim();
            if (string.IsNullOrEmpty(pair))
                return;

            int colonIndex = pair.IndexOf(':');
            if (colonIndex == -1)
                return;

            // Extract key
            string keyPart = pair.Substring(0, colonIndex).Trim();
            if (keyPart.StartsWith("\"") && keyPart.EndsWith("\""))
            {
                keyPart = keyPart.Substring(1, keyPart.Length - 2);
            }

            // Extract value
            string valuePart = pair.Substring(colonIndex + 1).Trim();

            dict[keyPart] = valuePart;
        }

        private void MergeDictionaries(Dictionary<string, object> target, Dictionary<string, object> source)
        {
            foreach (var kvp in source)
            {
                string key = kvp.Key;
                object value = kvp.Value;

                if (mergeMode == MergeMode.KeepFirst && target.ContainsKey(key))
                {
                    // Keep first value, skip this one
                    if (logProcess.Value)
                    {
                        Debug.Log($"[JSON Merge] Keeping first value for key: {key}");
                    }
                    continue;
                }

                if (deepMerge.Value && target.ContainsKey(key))
                {
                    // Check if both are objects that can be merged
                    string existingValue = target[key].ToString();
                    string newValue = value.ToString();

                    if (existingValue.Trim().StartsWith("{") && newValue.Trim().StartsWith("{"))
                    {
                        // Both are objects, merge them
                        var existingObj = ParseJSON(existingValue);
                        var newObj = ParseJSON(newValue);

                        if (existingObj != null && newObj != null)
                        {
                            MergeDictionaries(existingObj, newObj);
                            target[key] = BuildJSON(existingObj);

                            if (logProcess.Value)
                            {
                                Debug.Log($"[JSON Merge] Deep merged nested object: {key}");
                            }
                            continue;
                        }
                    }
                }

                // Override or add new key
                target[key] = value;

                if (logProcess.Value)
                {
                    Debug.Log($"[JSON Merge] {(target.ContainsKey(key) ? "Overriding" : "Adding")} key: {key}");
                }
            }
        }

        private string BuildJSON(Dictionary<string, object> dict)
        {
            var sb = new StringBuilder();
            string indent = prettyPrint.Value ? "  " : "";
            string newline = prettyPrint.Value ? "\n" : "";

            sb.Append("{");
            sb.Append(newline);

            int count = 0;
            foreach (var kvp in dict)
            {
                if (prettyPrint.Value)
                {
                    sb.Append(indent);
                }

                sb.Append("\"");
                sb.Append(kvp.Key);
                sb.Append("\":");

                if (prettyPrint.Value)
                {
                    sb.Append(" ");
                }

                sb.Append(kvp.Value.ToString());

                if (count < dict.Count - 1)
                {
                    sb.Append(",");
                }

                sb.Append(newline);
                count++;
            }

            sb.Append("}");

            return sb.ToString();
        }
    }
}