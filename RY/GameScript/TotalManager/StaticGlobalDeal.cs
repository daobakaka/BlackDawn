using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlackDawn
{
    public static class StaticGlobalDeal
    {

    }

    public static class DamageTextUVLookup
    {
        // ��ʽ����ӳ���˳��̶���index��ӳ������
        public static readonly char[] CharTable = new char[]
        {
            '_', '0','1','2','3','4','5','6','7','8','9',
            '.', 'K','M','G','T','P','I','S'
        };

        public static readonly Vector4[] UVTable = new Vector4[CharTable.Length];

        public static bool IsInitialized = false;

        // �ɵı����ֵ䣨�ɱ���Ҳ�ɲ�������
        public static readonly Dictionary<char, Vector2Int> CharToGrid = new();

        // Atlas ����
        private static readonly string[] atlasRowsChars = new string[]
        {
            "_!\"#$%&'()*+,-./",
            "0123456789:;<=>?",
            "@ABCDEFGHIJKLMNO",
            "PQRSTUVWXYZ[\\]^-",
            "`abcdefghijklmno",
            "pqrstuvwxyz{|}~ ",
            "                ",
            "                ",
            "                ",
            "                ",
            "                ",
            "                ",
            "                ",
            "                ",
            "                ",
            "                "
        };

        private static int atlasCols = 16;
        private static int atlasRows = 16;
        private static Vector2 uvScale;

        // ��ʼ������
        public static void InitializeAtlas()
        {
            if (IsInitialized) return;

            CharToGrid.Clear();
            int rows = atlasRowsChars.Length;
            int cols = 16;

            for (int row = 0; row < rows; ++row)
            {
                string line = atlasRowsChars[row].PadRight(cols, ' ');
                for (int col = 0; col < cols; ++col)
                {
                    char c = line[col];
                    if ((row == 0 && col == 0) || (row == 5 && col == 15)) continue;
                    if (c == ' ') continue;

                    CharToGrid[c] = new Vector2Int(col, row);
                }
            }

            // �ֶ����ռλ��
            CharToGrid['_'] = new Vector2Int(0, 0);

            uvScale = new Vector2(1f / atlasCols, 1f / atlasRows);

            // ��� UVTable��˳����� CharTable��
            for (int i = 0; i < CharTable.Length; i++)
            {
                UVTable[i] = GetUVRect(CharTable[i]);
            }

            IsInitialized = true;
        }

        public static Vector4 GetUVRect(char c)
        {
            if (CharToGrid.TryGetValue(c, out Vector2Int grid))
            {
                float cellSizeX = 1.0f / atlasCols;
                float cellSizeY = 1.0f / atlasRows;

                float uMin = grid.x * cellSizeX;
                float uMax = (grid.x + 1) * cellSizeX;
                float vMin = 1.0f - (grid.y + 1) * cellSizeY;
                float vMax = 1.0f - grid.y * cellSizeY;

                return new Vector4(uMin, vMin, uMax, vMax);
            }
            else
            {
                // û�ҵ��ͷ��� Blank���� index 0��
                return UVTable[0];
            }
        }

        // �����ܽӿڣ��� index
        public static int GetCharIndex(char c)
        {
            for (int i = 0; i < CharTable.Length; i++)
            {
                if (CharTable[i] == c)
                    return i;
            }
            return 0; // fallback '_'
        }

        // �����ܽӿڣ�ֱ�Ӳ� UV
        public static Vector4 GetUVByIndex(int index)
        {
            if (index >= 0 && index < UVTable.Length)
                return UVTable[index];
            return UVTable[0];
        }
    }
}
