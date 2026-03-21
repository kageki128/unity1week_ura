using R3;
using TMPro;
using Unity1Week_Ura.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity1Week_Ura.Actor
{
    public class PostView : MonoBehaviour
    {
        public Post post { get; private set; }
        public float Width => viewArranger.Width;
        public float Height => viewArranger.Height;
        public Observable<Post> OnLikedByPlayer => onLikedByPlayer;
        public Observable<Post> OnRepostedByPlayer => onRepostedByPlayer;

        [Header("Layout")]
        [SerializeField] ViewArranger viewArranger;
        [SerializeField] float defaultContentHeight = 0.6f;

        [Header("Main Visual")]
        [SerializeField] SpriteRenderer frameImage;
        [SerializeField] SpriteRenderer iconImage;

        [Header("Text")]
        [SerializeField] TMP_Text headerText;
        [SerializeField] TMP_Text advertisementText;
        [SerializeField] TMP_Text contentText;
        [SerializeField] TMP_Text repostCountText;
        [SerializeField] TMP_Text likeCountText;

        [Header("Text Style")]
        [SerializeField] Color subTextColor;
        [SerializeField] int subTextFontSizeOffset = 3;
        [SerializeField] Color repltTextColor = new Color32(0xEE, 0x5A, 0x7F, 0xFF);

        [Header("Action Buttons")]
        [SerializeField] ButtonView repostButtonView;
        [SerializeField] ButtonView likeButtonView;
        [SerializeField] SpriteRenderer repostIconImage;
        [SerializeField] SpriteRenderer likeIconImage;

        [Header("Action Colors")]
        [SerializeField] Color actionIconDefaultColor = new Color32(0x53, 0x64, 0x71, 0xFF);
        [SerializeField] Color repostActiveColor = new Color32(0x00, 0xBA, 0x7C, 0xFF);
        [SerializeField] Color likeActiveColor = new Color32(0xF9, 0x18, 0x80, 0xFF);

        readonly CompositeDisposable disposables = new();
        readonly Subject<Post> onLikedByPlayer = new();
        readonly Subject<Post> onRepostedByPlayer = new();

        public void Initialize(Post post)
        {
            disposables.Clear();

            var property = post.Property;
            var subTextColorHex = $"#{ColorUtility.ToHtmlStringRGBA(subTextColor)}";
            var repltTextColorHex = $"#{ColorUtility.ToHtmlStringRGBA(repltTextColor)}";

            iconImage.sprite = property.Author.Icon;
            headerText.text = $"{property.Author.Name}<size={subTextFontSizeOffset}><color={subTextColorHex}>　@{property.Author.Id}　{post.PublishDateTime:yyyy/MM/dd HH:mm}</color></size>";

            advertisementText.gameObject.SetActive(property.Type == PostType.Advertisement);

            if (string.IsNullOrEmpty(property.ParentPostId))
            {
                contentText.text = property.Text;
            }
            else
            {
                var replyText = $"返信先: @{property.ParentPostId} さん";
                contentText.text = $"<color={repltTextColorHex}>{replyText}</color>\n{property.Text}";
            }
            this.post = post;
            RefreshActionViews();
            SubscribeActions();

            AdjustLayout();
        }

        public void SetPosition(float x, float y)
        {
            viewArranger.SetPosition(x, y);
        }

        void AdjustLayout()
        {
            // テキストメッシュを強制更新して実際の描画サイズを取得
            contentText.ForceMeshUpdate();
            float renderedHeight = contentText.renderedHeight;

            // 1行時のデフォルト高さとの差分を計算
            float extraHeight = Mathf.Max(0f, renderedHeight - defaultContentHeight);
            if (extraHeight <= 0f) return;

            float halfExtra = extraHeight * 0.5f;

            // 全ての子オブジェクトを自動的に判別してオフセット
            foreach (Transform child in transform)
            {
                if (child == frameImage.transform)
                {
                    continue; // Frameはスケール変更で対応するため除外
                }

                var pos = child.localPosition;
                // 中心(Y=0)以上のオブジェクトは上へ、未満のオブジェクトは下へ広げる
                if (pos.y >= 0f)
                {
                    pos.y += halfExtra;
                }
                else
                {
                    pos.y -= halfExtra;
                }
                child.localPosition = pos;
            }

            // Frameは原点に固定したまま、スケールYのみ拡大（上下対称に広がる）
            var frameTransform = frameImage.transform;
            var frameScale = frameTransform.localScale;
            frameScale.y += extraHeight;
            frameTransform.localScale = frameScale;
        }

        void SubscribeActions()
        {
            if (repostButtonView != null)
            {
                repostButtonView.OnClicked.Subscribe(_ => OnRepostClicked()).AddTo(disposables);
            }

            if (likeButtonView != null)
            {
                likeButtonView.OnClicked.Subscribe(_ => OnLikeClicked()).AddTo(disposables);
            }
        }

        void OnRepostClicked()
        {
            bool isActive = post.ToggleRepostByPlayer();
            if (isActive)
            {
                onRepostedByPlayer.OnNext(post);
            }
            RefreshActionViews();
        }

        void OnLikeClicked()
        {
            bool isActive = post.ToggleLikeByPlayer();
            if (isActive)
            {
                onLikedByPlayer.OnNext(post);
            }
            RefreshActionViews();
        }

        void RefreshActionViews()
        {
            UpdateCountText(repostCountText, post.RepostCount, post.IsRepostedByPlayer, repostActiveColor);
            UpdateCountText(likeCountText, post.LikeCount, post.IsLikedByPlayer, likeActiveColor);
            ApplyIconColor(repostIconImage, post.IsRepostedByPlayer, repostActiveColor);
            ApplyIconColor(likeIconImage, post.IsLikedByPlayer, likeActiveColor);
        }

        void UpdateCountText(TMP_Text countText, int count, bool isActive, Color activeColor)
        {
            if (countText == null)
            {
                return;
            }

            countText.text = count.ToString();
            countText.color = isActive ? activeColor : actionIconDefaultColor;
        }

        void ApplyIconColor(SpriteRenderer icon, bool isActive, Color activeColor)
        {
            if (icon == null)
            {
                return;
            }

            icon.color = isActive ? activeColor : actionIconDefaultColor;

            if (icon.TryGetComponent<ButtonAnimator>(out var buttonAnimator))
            {
                buttonAnimator.RefreshBaseColorsFromCurrent();
            }
        }

        void OnDestroy()
        {
            disposables.Dispose();
            onLikedByPlayer.OnCompleted();
            onLikedByPlayer.Dispose();
            onRepostedByPlayer.OnCompleted();
            onRepostedByPlayer.Dispose();
        }
    }
}