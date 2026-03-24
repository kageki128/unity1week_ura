using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Unity1Week_Ura.Infrastructure
{
    [CreateAssetMenu(fileName = "AddressableConfig", menuName = "Unity1Week_Ura/AddressableConfig")]
    public class AddressableConfigSO : ScriptableObject
    {
        public AssetReference AccountDatas => accountDatas;
        [SerializeField] AssetReference accountDatas;

        public AssetReference PostDatas => postDatas;
        [SerializeField] AssetReference postDatas;

        public AssetLabelReference IconLabel => iconLabel;
        [SerializeField] AssetLabelReference iconLabel;

        public AssetLabelReference AttachedImageLabel => attachedImageLabel;
        [SerializeField] AssetLabelReference attachedImageLabel;
    }
}
