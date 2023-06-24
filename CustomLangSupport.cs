using System;
using TMPro;
using UnityEngine;

namespace MoreLanguages
{
    internal class CustomLangSupport : MonoBehaviour
    {
        public CustomLangSupport(IntPtr ptr) : base(ptr) {}

        private TMP_Text text;
        private void OnEnable()
        {
            if (text == null) SetText(GetComponent<TMP_Text>());
        }
        
        private void SetText(TMP_Text textComp)
        {
            //text
            text = textComp;
            text.enableWordWrapping = true;
            
            text.fontSizeMax = text.fontSize;
            text.enableAutoSizing = true;

            if (text.margin.x < 10 && text.margin.z < 10)
                text.margin = new Vector4(10, text.margin.y, 10, text.margin.w);
        }
    }
}