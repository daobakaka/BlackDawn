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
        // 显式定义映射表，顺序固定，index即映射索引
        public static readonly char[] CharTable = new char[]
        {
            '_', '0','1','2','3','4','5','6','7','8','9',
            '.', 'K','M','G','T','P','I','S'
        };

        public static readonly Vector4[] UVTable = new Vector4[CharTable.Length];

        public static bool IsInitialized = false;

        // 旧的备用字典（可保留也可不保留）
        public static readonly Dictionary<char, Vector2Int> CharToGrid = new();

        // Atlas 配置
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

        // 初始化方法
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

            // 手动添加占位符
            CharToGrid['_'] = new Vector2Int(0, 0);

            uvScale = new Vector2(1f / atlasCols, 1f / atlasRows);

            // 填充 UVTable（顺序对齐 CharTable）
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
                // 没找到就返回 Blank（即 index 0）
                return UVTable[0];
            }
        }

        // 高性能接口：查 index
        public static int GetCharIndex(char c)
        {
            for (int i = 0; i < CharTable.Length; i++)
            {
                if (CharTable[i] == c)
                    return i;
            }
            return 0; // fallback '_'
        }

        // 高性能接口：直接查 UV
        public static Vector4 GetUVByIndex(int index)
        {
            if (index >= 0 && index < UVTable.Length)
                return UVTable[index];
            return UVTable[0];
        }
    }
}
