/*
 * ═══════════════════════════════════════════════════════════════
 *                          UNITY FORGE
 *                    JSON To Raw Bytes Action
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
    [Tooltip("Convert JSON string to raw bytes (byte array). Useful for binary data transmission.")]
    public class JSONToRawBytes : FsmStateAction
    {
        [RequiredField]
        [Tooltip("The JSON string to convert")]
        [UIHint(UIHint.Variable)]
        public FsmString jsonString;

        [Title("Encoding")]
        [Tooltip("Text encoding to use")]
        public EncodingType encoding = EncodingType.UTF8;

        [Title("Output")]
        [RequiredField]
        [Tooltip("Store the raw bytes as byte array")]
        [ArrayEditor(VariableType.Int)]
        [UIHint(UIHint.Variable)]
        public FsmArray rawBytes;

        [Tooltip("Store the byte count")]
        [UIHint(UIHint.Variable)]
        public FsmInt byteCount;

        [Tooltip("Store as Base64 string (alternative output)")]
        [UIHint(UIHint.Variable)]
        public FsmString base64String;

        [Title("Events")]
        [Tooltip("Event sent on success")]
        public FsmEvent successEvent;

        [Tooltip("Event sent on error")]
        public FsmEvent errorEvent;

        [Title("Debug")]
        [Tooltip("Log encoding details")]
        public FsmBool logDetails = false;

        public enum EncodingType
        {
            UTF8,
            ASCII,
            Unicode,
            UTF32
        }

        public override void Reset()
        {
            jsonString = null;
            encoding = EncodingType.UTF8;
            rawBytes = null;
            byteCount = null;
            base64String = null;
            successEvent = null;
            errorEvent = null;
            logDetails = false;
        }

        public override void OnEnter()
        {
            EncodeJSON();
            Finish();
        }

        private void EncodeJSON()
        {
            if (string.IsNullOrEmpty(jsonString.Value))
            {
                if (logDetails.Value)
                {
                    Debug.LogWarning("[JSON To Raw Bytes] JSON string is empty");
                }
                SetError();
                return;
            }

            try
            {
                // Get encoding
                Encoding enc = GetEncoding(encoding);

                // Convert to bytes
                byte[] bytes = enc.GetBytes(jsonString.Value);

                // Store byte count
                if (!byteCount.IsNone)
                {
                    byteCount.Value = bytes.Length;
                }

                // Store as byte array (convert to int array for FSM)
                if (!rawBytes.IsNone)
                {
                    rawBytes.Resize(bytes.Length);
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        rawBytes.Set(i, (int)bytes[i]);
                    }
                }

                // Store as Base64 string
                if (!base64String.IsNone)
                {
                    base64String.Value = System.Convert.ToBase64String(bytes);
                }

                if (logDetails.Value)
                {
                    Debug.Log($"[JSON To Raw Bytes] Encoded {jsonString.Value.Length} chars to {bytes.Length} bytes using {encoding}");
                    if (!base64String.IsNone)
                    {
                        Debug.Log($"[JSON To Raw Bytes] Base64: {base64String.Value.Substring(0, Mathf.Min(50, base64String.Value.Length))}...");
                    }
                }

                Fsm.Event(successEvent);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[JSON To Raw Bytes] Error: {e.Message}");
                SetError();
            }
        }

        private Encoding GetEncoding(EncodingType type)
        {
            switch (type)
            {
                case EncodingType.UTF8:
                    return Encoding.UTF8;
                case EncodingType.ASCII:
                    return Encoding.ASCII;
                case EncodingType.Unicode:
                    return Encoding.Unicode;
                case EncodingType.UTF32:
                    return Encoding.UTF32;
                default:
                    return Encoding.UTF8;
            }
        }

        private void SetError()
        {
            if (!byteCount.IsNone)
            {
                byteCount.Value = 0;
            }

            if (!rawBytes.IsNone)
            {
                rawBytes.Resize(0);
            }

            if (!base64String.IsNone)
            {
                base64String.Value = "";
            }

            Fsm.Event(errorEvent);
        }
    }
}