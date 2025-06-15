using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace BlackDawn
{
    public class DamageTextMono : MonoBehaviour
    {
        [Header("材质设置")]
        public Material atlasMaterial;

        [Header("字符渲染参数")]
        public int charCount = 6; // 固定6位
        public string text6 = "000000";

        // 动画状态 struct
        private struct AttackAnimState
        {
            public bool isActive;
            public float timer;
            public float activeDuration;
            public float totalDuration;
            public float scaleMultiplier;
            public float offsetSpeed;
            public float shakeAmountX;
            public Color textColor;
        }

        private AttackAnimState[] attackStates = new AttackAnimState[8];

        void Awake()
        {

        }

        public void SetText(string str)
        {
            string paddedText = str.PadLeft(charCount, '_');
            text6 = paddedText;

            for (int i = 0; i < charCount; i++)
            {
                char c = text6[i];
                Vector4 uvRect = DamageTextUVLookup.GetUVRect(c);

                string propName = $"_Char{i + 1}UVRect";
                atlasMaterial.SetVector(propName, uvRect);
            }
        }

        private void SetAttackMode(int modeIndex, string text, Color color, float scaleMul, float offsetSpd, float shakeX, float activeDur, float totalDur)
        {
            SetText(text);

            attackStates[modeIndex] = new AttackAnimState
            {
                isActive = true,
                timer = 0f,
                activeDuration = activeDur,
                totalDuration = totalDur,
                scaleMultiplier = scaleMul,
                offsetSpeed = offsetSpd * 5,
                shakeAmountX = shakeX,
                textColor = color
            };

            atlasMaterial.SetColor("_TextColor", color);
            atlasMaterial.SetVector("_Offset", new Vector2(shakeX, offsetSpd));
            atlasMaterial.SetFloat("_StartTime", Time.time);
            atlasMaterial.SetFloat("_Scale", scaleMul * 3);
        }

        private void Update()
        {
            // 测试按键
            if (Input.GetKeyDown(KeyCode.Q)) SetAttackMode(0, "1", Color.white, 0.1f, 1f, 0, 0.2f, 0.6f);
            if (Input.GetKeyDown(KeyCode.W)) SetAttackMode(1, "12", new Color(0, 0.7f, 1), 0.25f, 1f, 0, 0.2f, 0.6f);
            if (Input.GetKeyDown(KeyCode.E)) SetAttackMode(2, "123", Color.white, 0.8f, 0.5f, 0f, 0.2f, 0.6f);
            if (Input.GetKeyDown(KeyCode.R)) SetAttackMode(3, "123.4K", new Color(0, 0.7f, 1), 1f, 0.5f, 0.02f, 0.2f, 0.6f);
            if (Input.GetKeyDown(KeyCode.T)) SetAttackMode(4, "311.4K", new Color(1, 0.85f, 0), 0.5f, 1.3f, 0f, 0.2f, 0.6f);
            if (Input.GetKeyDown(KeyCode.Y)) SetAttackMode(5, "823.5K", new Color(0, 1f, 1), 0.5f, 1.3f, 0, 0.2f, 0.6f);
            if (Input.GetKeyDown(KeyCode.U)) SetAttackMode(6, "103.4M", new Color(1, 0.85f, 0), 1f, 0.7f, 0.02f, 0.2f, 0.6f);
            if (Input.GetKeyDown(KeyCode.I)) SetAttackMode(7, "123.4G", new Color(1, 0.5f, 0), 2f, 0.4f, 0.1f, 0.2f, 0.6f);
        }
    }
}