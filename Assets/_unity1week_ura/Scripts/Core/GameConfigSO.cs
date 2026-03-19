using UnityEngine;

namespace Unity1Week_Ura.Core
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Unity1Week_Ura/GameConfig")]
    public class GameConfigSO : ScriptableObject
    {
        public SceneType InitialSceneType => initialSceneType;
        [SerializeField] SceneType initialSceneType;
        public GameRuleSO InitialGameRule => initialGameRule;
        [SerializeField] GameRuleSO initialGameRule;
    }
}