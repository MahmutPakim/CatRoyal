using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatRoyal.Sunum
{
    /// <summary>
    /// İlk oynanabilir sürümün görselleri: şekiller çalışma anında, kodla çizilir. Hiçbir dış görsel kullanılmaz;
    /// gerçek çizimler geldiğinde yalnızca bu sınıf değişir.
    /// </summary>
    public static class SekilUretici
    {
        private const int Boyut = 64;
        private static readonly Dictionary<string, Sprite> Onbellek = new Dictionary<string, Sprite>();

        public static Sprite Daire => Al("daire", (x, y) => Yumusat(0.46f - Uzaklik(x, y)));

        public static Sprite Kare => Al("kare", (x, y) => Yumusat(0.46f - KoseliUzaklik(x, y, 0.12f)));

        public static Sprite Halka => Al("halka", (x, y) =>
            Mathf.Min(Yumusat(0.46f - Uzaklik(x, y)), Yumusat(Uzaklik(x, y) - 0.26f)));

        public static Sprite Elmas => Al("elmas", (x, y) => Yumusat(0.44f - (Mathf.Abs(x) + Mathf.Abs(y))));

        public static Sprite Serit => Al("serit", (x, y) =>
            Mathf.Min(Yumusat(0.1f - Mathf.Abs(y)), Yumusat(0.4f - Mathf.Abs(x))));

        public static Sprite Cerceve => Al("cerceve", (x, y) =>
            Mathf.Min(Yumusat(0.48f - KoseliUzaklik(x, y, 0.1f)), Yumusat(KoseliUzaklik(x, y, 0.1f) - 0.38f)));

        private static Sprite Al(string ad, Func<float, float, float> saydamlik)
        {
            if (Onbellek.TryGetValue(ad, out var sprite) && sprite != null)
            {
                return sprite;
            }

            var doku = new Texture2D(Boyut, Boyut, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "CatRoyal_" + ad,
            };

            var pikseller = new Color32[Boyut * Boyut];
            for (int py = 0; py < Boyut; py++)
            {
                for (int px = 0; px < Boyut; px++)
                {
                    float x = ((px + 0.5f) / Boyut) - 0.5f;
                    float y = ((py + 0.5f) / Boyut) - 0.5f;
                    byte a = (byte)(Mathf.Clamp01(saydamlik(x, y)) * 255f);
                    pikseller[(py * Boyut) + px] = new Color32(255, 255, 255, a);
                }
            }

            doku.SetPixels32(pikseller);
            doku.Apply();
            sprite = Sprite.Create(doku, new Rect(0, 0, Boyut, Boyut), new Vector2(0.5f, 0.5f), Boyut);
            sprite.name = doku.name;
            Onbellek[ad] = sprite;
            return sprite;
        }

        private static float Uzaklik(float x, float y) => Mathf.Sqrt((x * x) + (y * y));

        /// <summary>Köşeleri yuvarlatılmış kareye uzaklık ölçüsü.</summary>
        private static float KoseliUzaklik(float x, float y, float yuvarlama)
        {
            float dx = Mathf.Max(Mathf.Abs(x) - (0.5f - yuvarlama), 0f);
            float dy = Mathf.Max(Mathf.Abs(y) - (0.5f - yuvarlama), 0f);
            float ic = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
            return dx > 0f && dy > 0f ? 0.5f - yuvarlama + Mathf.Sqrt((dx * dx) + (dy * dy)) : ic;
        }

        /// <summary>Kenar yumuşatma: piksel genişliğinde geçiş.</summary>
        private static float Yumusat(float mesafe) => Mathf.Clamp01((mesafe * Boyut) + 0.5f);
    }
}
