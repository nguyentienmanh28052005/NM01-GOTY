using System.IO;
using System.Linq;
using HanaMart.Audio;
using UnityEditor;
using UnityEngine;

namespace HanaMart.EditorTools
{
    public static class AudioSetup
    {
        public const string BankPath="Assets/HanaMart/Data/AudioBank.asset";
        [MenuItem("HanaMart/Build Audio Bank")]
        public static void Run()
        {
            var bank=AssetDatabase.LoadAssetAtPath<AudioBank>(BankPath);
            if(!bank){bank=ScriptableObject.CreateInstance<AudioBank>();AssetDatabase.CreateAsset(bank,BankPath);}
            bank.entries=Directory.GetFiles("Assets/HanaMart/Audio/FromThree","*.wav").OrderBy(p=>p).Select(path=>new AudioBank.Entry
            {id=Path.GetFileNameWithoutExtension(path),clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path.Replace('\\','/'))}).ToArray();
            EditorUtility.SetDirty(bank);AssetDatabase.SaveAssets();
            Debug.Log($"Audio bank ready: {bank.entries.Length} exported clips");
        }
    }
}
