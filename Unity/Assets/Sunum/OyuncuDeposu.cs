using System;
using System.IO;
using CatRoyal.Cekirdek.Meta;
using CatRoyal.Cekirdek.Yapilandirma;
using UnityEngine;

namespace CatRoyal.Sunum
{
    /// <summary>
    /// Oyuncu kaydının diskteki yeri (GDD 9.1.5, 9.2.8, 9.3.10). Kayıt metnini çekirdek üretir
    /// (<see cref="OyuncuKaydi"/>); bu sınıf yalnızca dosyayı okur ve yazar.
    ///
    /// Yazma iki adımlıdır: önce geçici dosyaya yazılır, sonra asıl dosyanın yerine konur. Yazarken uygulama
    /// kapanırsa eski kayıt bozulmaz. Okunamayan kayıt silinmez, ".bozuk" adıyla kenara alınır.
    /// </summary>
    public sealed class OyuncuDeposu
    {
        private readonly string _yol;
        private string _sonYazilan;

        public OyuncuDeposu()
            : this(Path.Combine(Application.persistentDataPath, "oyuncu.json"))
        {
        }

        public OyuncuDeposu(string yol)
        {
            _yol = yol;
        }

        public string Yol => _yol;

        /// <summary>
        /// Kaydı yükler; kayıt yoksa yeni oyuncu kurar. Kayıt okunamıyorsa kenara alınır, yeni oyuncu kurulur ve
        /// <paramref name="uyari"/> doldurulur.
        /// </summary>
        public Oyuncu Yukle(ParametreTablosu parametreler, DateTime simdi, out string uyari)
        {
            uyari = null;
            if (File.Exists(_yol))
            {
                try
                {
                    string metin = File.ReadAllText(_yol);
                    var oyuncu = OyuncuKaydi.Yukle(metin, parametreler, simdi);
                    _sonYazilan = metin;
                    return oyuncu;
                }
                catch (Exception hata) when (hata is KayitHatasi || hata is IOException || hata is UnauthorizedAccessException)
                {
                    Debug.LogException(hata);
                    uyari = "Kayıt okunamadı; yeni oyuncuyla başlandı. " + KenaraAl();
                }
            }

            _sonYazilan = null;
            return new Oyuncu(parametreler, simdi);
        }

        /// <summary>Oyuncu son yazılandan farklıysa kaydeder.</summary>
        /// <returns>Dosyaya yazıldıysa true.</returns>
        public bool Kaydet(Oyuncu oyuncu)
        {
            string metin = OyuncuKaydi.Yaz(oyuncu);
            if (metin == _sonYazilan)
            {
                return false;
            }

            try
            {
                string gecici = _yol + ".gecici";
                File.WriteAllText(gecici, metin);
                if (File.Exists(_yol))
                {
                    File.Replace(gecici, _yol, null);
                }
                else
                {
                    File.Move(gecici, _yol);
                }

                _sonYazilan = metin;
                return true;
            }
            catch (Exception hata) when (hata is IOException || hata is UnauthorizedAccessException)
            {
                // Yazılamadı: bir sonraki denemede yeniden yazılır.
                Debug.LogException(hata);
                return false;
            }
        }

        /// <summary>Kaydı siler (geliştirici kısayolu).</summary>
        public void Sil()
        {
            if (File.Exists(_yol))
            {
                File.Delete(_yol);
            }

            _sonYazilan = null;
        }

        private string KenaraAl()
        {
            try
            {
                string hedef = _yol + ".bozuk";
                if (File.Exists(hedef))
                {
                    File.Delete(hedef);
                }

                File.Move(_yol, hedef);
                return "Eski dosya: " + hedef;
            }
            catch (Exception hata) when (hata is IOException || hata is UnauthorizedAccessException)
            {
                Debug.LogException(hata);
                return "Eski dosya kenara alınamadı.";
            }
        }
    }

    /// <summary>Analitik olaylarını editör günlüğüne yazar; gerçek analitik hizmeti bağlanana kadar kullanılır.</summary>
    public sealed class GunlukAnalitigi : IAnalitik
    {
        public void Bildir(string ad, System.Collections.Generic.IReadOnlyDictionary<string, object> alanlar)
        {
            var metin = new System.Text.StringBuilder("[analitik] ").Append(ad);
            foreach (var cift in alanlar)
            {
                metin.Append(' ').Append(cift.Key).Append('=').Append(cift.Value);
            }

            Debug.Log(metin.ToString());
        }
    }
}
