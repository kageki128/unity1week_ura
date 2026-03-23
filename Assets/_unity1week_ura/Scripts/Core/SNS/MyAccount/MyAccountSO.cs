using UnityEngine;

namespace Unity1Week_Ura.Core
{
    [CreateAssetMenu(fileName = "MyAccount", menuName = "Unity1Week_Ura/MyAccount")]
    public class MyAccountSO : ScriptableObject
    {
        public string Id => id;
        [SerializeField] string id = "default_account_id";
    }
}