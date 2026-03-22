using System.Collections.Generic;
using UnityEngine;

namespace Unity1Week_Ura.Core
{
    [CreateAssetMenu(fileName = "GameRule", menuName = "Unity1Week_Ura/GameRule")]
    public class GameRuleSO : ScriptableObject
    {
        public string DifficultyName => difficultyName;
        [SerializeField] string difficultyName = "Unknown";

        public float TimeLimitSeconds => timeLimitSeconds;
        [SerializeField] float timeLimitSeconds = 120.0f;

        public float PostPerSecond => postPerSecond;
        [SerializeField] float postPerSecond = 0.5f;

        public IReadOnlyList<MyAccountSO> UsedAccounts => usedAccounts;
        [SerializeField] List<MyAccountSO> usedAccounts;
    }
}
