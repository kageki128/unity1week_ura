using R3;
using TMPro;
using Unity1Week_Ura.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Unity1Week_Ura.Actor
{
    public class PlayerAccountView : MonoBehaviour
    {
        public float Width => iconImage.bounds.size.x;
        public float Height => iconImage.bounds.size.y;
        public Observable<Account> OnClicked => pointerEventObserver.OnClicked.Select(_ => account);
        public Account Account => account;

        [SerializeField] PointerEventObserver pointerEventObserver;
        [SerializeField] SpriteRenderer iconImage;
        [SerializeField] TMP_Text nameText;
        
        [SerializeField] Color selectedColor = new(1f, 0.85f, 0.35f, 1f);
        Color normalColor;

        Account account;

        public void Initialize(Account account)
        {
            this.account = account;

            normalColor = iconImage.color;

            if (account.Icon != null)
            {
                iconImage.sprite = account.Icon;
            }
            
            nameText.text = account.Name;
            SetSelected(false);
        }

        public void SetPosition(float x, float y)
        {
            transform.localPosition = new Vector3(x, y, 0f);
        }

        public void SetSelected(bool isSelected)
        {
            Color color = isSelected ? selectedColor : normalColor;
            iconImage.color = color;
        }
    }
}