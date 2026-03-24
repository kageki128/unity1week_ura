using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    [CreateAssetMenu(fileName = "HowToPlayPages", menuName = "Unity1Week_Ura/HowToPlayPages")]
    public class HowToPlayPagesSO : ScriptableObject
    {
        [Serializable]
        public class PageData
        {
            public string Title => title;
            [SerializeField] string title = "タイトル";

            public string Text => text;
            [TextArea(3, 10)]
            [SerializeField] string text;

            public Sprite Image => image;
            [SerializeField] Sprite image;
        }

        public IReadOnlyList<PageData> Pages => pages;
        [SerializeField] List<PageData> pages = new();
    }
}
