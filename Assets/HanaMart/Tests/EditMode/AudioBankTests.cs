using System.IO;
using System.Linq;
using HanaMart.Audio;
using NUnit.Framework;
using UnityEditor;

namespace HanaMart.Tests
{
    [Timeout(10000)]
    public class AudioBankTests
    {
        [Test] public void EveryExportedClipIsPlayableByOriginalId()
        {
            var bank=AssetDatabase.LoadAssetAtPath<AudioBank>("Assets/HanaMart/Data/AudioBank.asset");
            Assert.IsNotNull(bank);
            var files=Directory.GetFiles("Assets/HanaMart/Audio/FromThree","*.wav");
            Assert.AreEqual(42,files.Length);Assert.AreEqual(42,bank.entries.Length);
            Assert.AreEqual(42,bank.entries.Select(e=>e.id).Distinct().Count());
            foreach(var path in files)
            {
                var clip=bank.Find(Path.GetFileNameWithoutExtension(path));
                Assert.IsNotNull(clip,path);Assert.Greater(clip.length,.05f,path);Assert.AreEqual(44100,clip.frequency,path);
            }
        }
    }
}
