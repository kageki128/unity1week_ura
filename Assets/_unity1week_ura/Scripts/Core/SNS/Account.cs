using UnityEngine;
using UnityEngine.UI;

namespace Unity1Week_Ura.Core
{
    public class Account
    {
        public string Id { get; }
        public string Name { get; }
        public Sprite Icon { get; }

        public Account(string id, string name, Sprite icon)
        {
            Id = id;
            Name = name;
            Icon = icon;
        }
    }
}