using UnityEngine;

namespace Unity1Week_Ura.Core
{
    [CreateAssetMenu(fileName = "MyAccount", menuName = "Unity1Week_Ura/MyAccount")]
    public class MyAccountSO : ScriptableObject
    {
        public string Id => id;
        [SerializeField] string id = "default_account_id";

        public string DisplayName => displayName;
        [SerializeField] string displayName = "Default User";

        public string Description => description;
        [TextArea]
        [SerializeField] string description = "This is a default user account.";
    }
}