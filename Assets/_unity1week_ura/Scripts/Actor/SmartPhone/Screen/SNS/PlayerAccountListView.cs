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
        [SerializeField] float accountSpacing = 0.2f;

        readonly List<PlayerAccountView> playerAccountViews = new();

        public void SetPlayerAccounts(IReadOnlyList<Account> accounts)
        {
            ClearPlayerAccounts();
            foreach (var account in accounts)
            {
                var playerAccountView = CreatePlayerAccountView(account);
                playerAccountViews.Add(playerAccountView);
            }
            ArrangePlayerAccounts(null);

            OnClicked = Observable.Merge(playerAccountViews.Select(view => view.OnClicked).ToArray());
        }

        public void SetSelectedPlayerAccount(Account selectedAccount)
        {
            foreach (var playerAccountView in playerAccountViews)
            {
                bool isSelected = playerAccountView.Account == selectedAccount;
                playerAccountView.SetSelected(isSelected);
            }

            ArrangePlayerAccounts(selectedAccount);
        }
        
        void ClearPlayerAccounts()
        {
            foreach (var playerAccountView in playerAccountViews)
            {
                Destroy(playerAccountView.gameObject);
            }
            playerAccountViews.Clear();
        }
        
        void ArrangePlayerAccounts(Account selectedAccount)
        {
            float left = 0f;
            for (int i = 0; i < playerAccountViews.Count; i++)
            {
                var playerAccountView = playerAccountViews[i];
                bool isSelected = selectedAccount != null && playerAccountView.Account == selectedAccount;
                float width = playerAccountView.GetPredictedWidth(isSelected);

                // positionTarget は中心座標として扱うため、左端から半幅ぶん進めた地点に配置する
                float centerX = left + (width * 0.5f);
                playerAccountView.SetPosition(centerX, 0f);

                left += width + accountSpacing;
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