using UnityEngine;
using UnityEngine.UI;

namespace Unity1Week_Ura.Core
{
    public class Account
    {
        public string Id { get; }
        public string Name { get; }
        public Sprite Icon { get; }
        public AccountType Type { get; }

        public Account(string id, string name, Sprite icon, AccountType type)
        {
            Id = id;
            Name = name;
            Icon = icon;
            Type = type;
        }
    }
}
