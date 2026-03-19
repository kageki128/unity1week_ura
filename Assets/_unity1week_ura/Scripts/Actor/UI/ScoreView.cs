using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class ScoreView : ViewBase
    {
        [SerializeField] TMP_Text scoreText;

        public override void Initialize()
        {
            SetScore(0);
            gameObject.SetActive(false);
        }

        public override UniTask ShowAsync(CancellationToken ct)
        {
            gameObject.SetActive(true);
            return UniTask.CompletedTask;
        }

        public override UniTask HideAsync(CancellationToken ct)
        {
            gameObject.SetActive(false);
            return UniTask.CompletedTask;
        }

        public void SetScore(int score)
        {
            scoreText.text = score.ToString();
        }
    }
}