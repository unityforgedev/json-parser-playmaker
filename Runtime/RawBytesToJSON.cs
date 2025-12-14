/*
 * ═══════════════════════════════════════════════════════════════
 *                          UNITY FORGE
 *                    Raw Bytes To JSON Action
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
    [Tooltip("Convert raw bytes (byte array) back to JSON string. Useful for decoding binary data.")]
    public class RawBytesToJSON : FsmStateAction
    {
        [Title("Input Method")]
        [Tooltip("Choose input source")]
        public InputMethod inputMethod = InputMethod.ByteArray;

        [Title("Raw Bytes Input")]
        [Tooltip("The raw bytes as int array")]
        [ArrayEditor(VariableType.Int)]
        [UIHint(UIHint.Variable)]
        public FsmArray rawBytes;

        [Title("Base64 Input")]
        [Tooltip("Base64 encoded string")]
        [UIHint(UIHint.Variable)]
        public FsmString base64String;

        [Title("Encoding")]
        [Tooltip("Text encoding to use")]
        public EncodingType encoding = EncodingType.UTF8;

        [Title("Output")]
        [RequiredField]
        [Tooltip("Store the decoded JSON string")]
        [UIHint(UIHint.Variable)]
        public FsmString jsonString;

        [Tooltip("Store the character count")]
        [UIHint(UIHint.Variable)]
        public FsmInt charCount;

        [Title("Events")]
        [Tooltip("Event sent on success")]
        public FsmEvent successEvent;

        [Tooltip("Event sent on error")]
        public FsmEvent errorEvent;

        [Title("Debug")]
        [Tooltip("Log decoding details")]
        public FsmBool logDetails = false;

        public enum InputMethod
        {
            ByteArray,
            Base64String
        }

        public enum EncodingType
        {
            UTF8,
            ASCII,
            Unicode,
            UTF32
        }

        public override void Reset()
        {
            inputMethod = InputMethod.ByteArray;
            rawBytes = null;
            base64String = null;
            encoding = EncodingType.UTF8;
            jsonString = null;
            charCount = null;
            successEvent = null;
            errorEvent = null;
            logDetails = false;
        }

        public override void OnEnter()
        {
            DecodeBytes();
            Finish();
        }

        private void DecodeBytes()
        {
            try
            {
                byte[] bytes = null;

                if (inputMethod == InputMethod.ByteArray)
                {
                    // Convert from int array to byte array
                    if (rawBytes == null || rawBytes.Length == 0)
                    {
                        if (logDetails.Value)
                        {
                            Debug.LogWarning("[Raw Bytes To JSON] Byte array is empty");
                        }
                        SetError();
                        return;
                    }

                    bytes = new byte[rawBytes.Length];
                    for (int i = 0; i < rawBytes.Length; i++)
                    {
                        object val = rawBytes.Get(i);
                        bytes[i] = (byte)((int)val);
                    }

                    if (logDetails.Value)
                    {
                        Debug.Log($"[Raw Bytes To JSON] Decoding {bytes.Length} bytes from byte array");
                    }
                }
                else // Base64String
                {
                    if (string.IsNullOrEmpty(base64String.Value))
                    {
                        if (logDetails.Value)
                        {
                            Debug.LogWarning("[Raw Bytes To JSON] Base64 string is empty");
                        }
                        SetError();
                        return;
                    }

                    bytes = System.Convert.FromBase64String(base64String.Value);

                    if (logDetails.Value)
                    {
                        Debug.Log($"[Raw Bytes To JSON] Decoded {bytes.Length} bytes from Base64");
                    }
                }

                // Get encoding
                Encoding enc = GetEncoding(encoding);

                // Convert to string
                string result = enc.GetString(bytes);

                // Store result
                if (!jsonString.IsNone)
                {
                    jsonString.Value = result;
                }

                if (!charCount.IsNone)
                {
                    charCount.Value = result.Length;
                }

                if (logDetails.Value)
                {
                    string preview = result.Length > 100 ? result.Substring(0, 100) + "..." : result;
                    Debug.Log($"[Raw Bytes To JSON] Decoded to {result.Length} characters using {encoding}");
                    Debug.Log($"[Raw Bytes To JSON] Result: {preview}");
                }

                Fsm.Event(successEvent);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Raw Bytes To JSON] Error: {e.Message}");
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
            if (!jsonString.IsNone)
            {
                jsonString.Value = "";
            }

            if (!charCount.IsNone)
            {
                charCount.Value = 0;
            }

            Fsm.Event(errorEvent);
        }
    }
}