using System;
using System.Collections.Generic;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    [CreateAssetMenu(fileName = "PlayerAccountCharacterSprites", menuName = "Unity1Week_Ura/PlayerAccountCharacterSprites")]
    public class PlayerAccountCharacterSpritesSO : ScriptableObject
    {
        [Serializable]
        public class AccountSpriteEntry
        {
            public string AccountId => account != null ? account.Id : string.Empty;
            [SerializeField] MyAccountSO account;

            public IReadOnlyList<Sprite> Sprites => sprites;
            [SerializeField] List<Sprite> sprites = new();
        }

        [SerializeField] List<AccountSpriteEntry> accountEntries = new();

        public bool TryGetEntry(string accountId, out AccountSpriteEntry entry)
        {
            entry = null;
            if (string.IsNullOrEmpty(accountId))
            {
                return false;
            }

            for (var i = 0; i < accountEntries.Count; i++)
            {
                var candidate = accountEntries[i];
                if (candidate == null)
                {
                    continue;
                }

                if (string.Equals(candidate.AccountId, accountId, StringComparison.Ordinal))
                {
                    entry = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
