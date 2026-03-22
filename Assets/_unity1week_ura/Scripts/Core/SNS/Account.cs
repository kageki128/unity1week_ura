using UnityEngine;

namespace Unity1Week_Ura.Core
{
    public class Account
    {
        public string Id { get; }
        public string Name { get; }
        public Sprite Icon { get; }
        public AccountType Type { get; }
        public string RelatedPlayerAccountId { get; }

        public Account(string id, string name, Sprite icon, AccountType type, string relatedPlayerAccountId)
        {
            Id = id;
            Name = name;
            Icon = icon;
            Type = type;
            RelatedPlayerAccountId = relatedPlayerAccountId;
        }
    }
}
