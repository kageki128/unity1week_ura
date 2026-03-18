using UnityEngine;

namespace Unity1Week_Ura.Core
{
    [CreateAssetMenu(fileName = "GameRule", menuName = "Unity1Week_Ura/GameRule")]
    public class GameRuleSO : ScriptableObject
    {
        public float TimeLimitSeconds => timeLimitSeconds;
        [SerializeField] float timeLimitSeconds = 120.0f;

        public int SecretAccountCount => secretAccountCount;
        [SerializeField] int secretAccountCount = 1;
    }
}