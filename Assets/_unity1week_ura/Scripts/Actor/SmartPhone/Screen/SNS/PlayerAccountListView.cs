using System.Collections.Generic;
using System.Linq;
using R3;
using Unity1Week_Ura.Core;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class PlayerAccountListView : MonoBehaviour
    {
        public Observable<Account> OnClicked;

        [SerializeField] PlayerAccountView playerAccountViewPrefab;
        [SerializeField] Transform playerAccountViewParent;

        readonly List<PlayerAccountView> playerAccountViews = new();

        public void SetPlayerAccounts(IReadOnlyList<Account> accounts)
        {
            ClearPlayerAccounts();
            foreach (var account in accounts)
            {
                var playerAccountView = CreatePlayerAccountView(account);
                playerAccountViews.Add(playerAccountView);
            }
            ArrangePlayerAccounts();

            OnClicked = Observable.Merge(playerAccountViews.Select(view => view.OnClicked).ToArray());
        }

        public void SetSelectedPlayerAccount(Account selectedAccount)
        {
            foreach (var playerAccountView in playerAccountViews)
            {
                bool isSelected = playerAccountView.Account == selectedAccount;
                playerAccountView.SetSelected(isSelected);
            }
        }
        
        void ClearPlayerAccounts()
        {
            foreach (var playerAccountView in playerAccountViews)
            {
                Destroy(playerAccountView.gameObject);
            }
            playerAccountViews.Clear();
        }
        
        void ArrangePlayerAccounts()
        {
            // リストを左から隙間無く配置する
            for (int i = 0; i < playerAccountViews.Count; i++)
            {
                var playerAccountView = playerAccountViews[i];
                float x = i * playerAccountView.Width;
                playerAccountView.SetPosition(x, 0f);
            }
        }
        PlayerAccountView CreatePlayerAccountView(Account account)
        {
            PlayerAccountView playerAccountView = Instantiate(playerAccountViewPrefab, playerAccountViewParent);
            playerAccountView.Initialize(account);
            return playerAccountView;
        }
    }
}