using DG.Tweening;
using R3;
using TMPro;
using Unity1Week_Ura.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Unity1Week_Ura.Actor
{
    public class PostView : MonoBehaviour
    {
        public Post post { get; private set; }
        public float Width => viewArranger.Width;
        public float Height => viewArranger.Height;
        public Observable<Post> OnPostClicked => onPostClicked;
        public Observable<Post> OnLikedByPlayer => onLikedByPlayer;
        public Observable<Post> OnRepostedByPlayer => onRepostedByPlayer;
        public Observable<PointerEventData> OnScrolled => onScrolled;

        [Header("Layout")]
        [SerializeField] ViewArranger viewArranger;
        [SerializeField] float defaultContentHeight = 0.6f;

        [Header("Main Visual")]
        [SerializeField] SpriteRenderer frameImage;
        [SerializeField] SpriteRenderer iconImage;

        [Header("Top Edge Object")]
        [SerializeField] Transform topEdgeObject;

        [Header("Repost Header")]
        [SerializeField] GameObject repostedHeaderIcon;

        [Header("Text")]
        [SerializeField] TMP_Text headerText;
        [SerializeField] TMP_Text repostedByText;
        [SerializeField] TMP_Text advertisementText;
        [SerializeField] TMP_Text contentText;
        [SerializeField] TMP_Text repostCountText;
        [SerializeField] TMP_Text likeCountText;

        [Header("Text Style")]
        [SerializeField] Color subTextColor;
        [SerializeField] int subTextFontSizeOffset = 3;
        [SerializeField] Color repltTextColor = new Color32(0xEE, 0x5A, 0x7F, 0xFF);

        [Header("Action Buttons")]
        [SerializeField] ButtonView postButtonView;
        [SerializeField] ButtonView repostButtonView;
        [SerializeField] ButtonView likeButtonView;
        [SerializeField] SpriteRenderer repostIconImage;
        [SerializeField] SpriteRenderer likeIconImage;

        [Header("Action Colors")]
        [SerializeField] Color actionIconDefaultColor = new Color32(0x53, 0x64, 0x71, 0xFF);
        [SerializeField] Color repostActiveColor = new Color32(0x00, 0xBA, 0x7C, 0xFF);
        [SerializeField] Color likeActiveColor = new Color32(0xF9, 0x18, 0x80, 0xFF);

        readonly CompositeDisposable disposables = new();
        readonly Subject<Post> onPostClicked = new();
        readonly Subject<Post> onLikedByPlayer = new();
        readonly Subject<Post> onRepostedByPlayer = new();
        readonly Subject<UnityEngine.EventSystems.PointerEventData> onScrolled = new();

        public void Initialize(Post post)
        {
            disposables.Clear();
            this.post = post;

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
                var parentAccountId = property.ParentPostAuthor?.Id;
                if (string.IsNullOrEmpty(parentAccountId))
                {
                    parentAccountId = property.ParentPostId;
                }

                var replyText = $"返信先: @{parentAccountId} さん";
                contentText.text = $"<color={repltTextColorHex}>{replyText}</color>\n{property.Text}";
            }
            RefreshRepostedByText();
            SubscribePostState();
            SubscribeActions();

            AdjustLayout();
        }

        public void SetPosition(float x, float y, bool useAnimation = true)
        {
            viewArranger.SetPosition(x, y, useAnimation);
        }

        public void SetInteractable(bool isInteractable)
        {
            postButtonView?.SetInteractable(isInteractable);
            repostButtonView?.SetInteractable(isInteractable);
            likeButtonView?.SetInteractable(isInteractable);
        }

        public void StopAnimations()
        {
            if (viewArranger != null)
            {
                viewArranger.StopAnimations();
            }

            var buttonAnimators = GetComponentsInChildren<ButtonAnimator>(true);
            for (var i = 0; i < buttonAnimators.Length; i++)
            {
                var buttonAnimator = buttonAnimators[i];
                if (buttonAnimator == null)
                {
                    continue;
                }

                buttonAnimator.StopAnimations();
            }

            var transforms = GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var target = transforms[i];
                if (target == null)
                {
                    continue;
                }

                DOTween.Kill(target);
            }

            var spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (var i = 0; i < spriteRenderers.Length; i++)
            {
                var target = spriteRenderers[i];
                if (target == null)
                {
                    continue;
                }

                DOTween.Kill(target);
            }

            var texts = GetComponentsInChildren<TMP_Text>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                var target = texts[i];
                if (target == null)
                {
                    continue;
                }

                DOTween.Kill(target);
            }
        }

        void SubscribePostState()
        {
            post.IsRepostedByPlayer.Subscribe(_ => RefreshActionViews()).AddTo(disposables);
            post.IsLikedByPlayer.Subscribe(_ => RefreshActionViews()).AddTo(disposables);
        }

        void AdjustLayout()
        {
            // テキストメッシュを強制更新して実際の描画サイズを取得
            contentText.ForceMeshUpdate();
            float renderedHeight = contentText.renderedHeight;

            // 1行時のデフォルト高さとの差分を計算
            float extraHeight = Mathf.Max(0f, renderedHeight - defaultContentHeight);
            
            float topExtraHeight = 0f;
            if (repostedByText.gameObject.activeSelf)
            {
                repostedByText.ForceMeshUpdate();
                float frameTop = frameImage.transform.localPosition.y + frameImage.transform.localScale.y * 0.5f;
                float textTop = repostedByText.transform.localPosition.y + repostedByText.rectTransform.rect.yMax;
                topExtraHeight = textTop - frameTop;
                if (topExtraHeight < 0f) topExtraHeight = 0f;
            }

            if (extraHeight <= 0f && topExtraHeight <= 0f) return;

            float halfExtra = extraHeight * 0.5f;
            float halfTopExtra = topExtraHeight * 0.5f;

            // 全ての子オブジェクトを自動的に判別してオフセット
            foreach (Transform child in transform)
            {
                if (child == frameImage.transform)
                {
                    continue; // Frameはスケール変更で対応するため除外
                }

                bool isTopEdge = (topEdgeObject != null && topEdgeObject == child);

                var pos = child.localPosition;

                if (isTopEdge)
                {
                    // 上端追従オブジェクトは、伸びたフレームの上端に合わせて上へ移動する
                    pos.y += halfExtra;
                    pos.y += halfTopExtra;
                }
                else
                {
                    // 中心(Y=0)以上のオブジェクトは上へ、未満のオブジェクトは下へ広げる
                    if (pos.y >= 0f)
                    {
                        pos.y += halfExtra;
                    }
                    else
                    {
                        pos.y -= halfExtra;
                    }
                    
                    pos.y -= halfTopExtra;
                }
                
                child.localPosition = pos;

                if (child.TryGetComponent<ButtonAnimator>(out var buttonAnimator))
                {
                    buttonAnimator.RefreshBaseTransformFromCurrent();
                }
            }

            // Frameは原点に固定したまま、スケールYのみ拡大（上下対称に広がる）
            var frameTransform = frameImage.transform;
            var frameScale = frameTransform.localScale;
            frameScale.y += (extraHeight + topExtraHeight);
            frameTransform.localScale = frameScale;
        }

        void SubscribeActions()
        {
            if (postButtonView != null)
            {
                postButtonView.OnClicked.Subscribe(_ => OnPostClickedEvent()).AddTo(disposables);
                postButtonView.OnScrolled.Subscribe(onScrolled.OnNext).AddTo(disposables);
            }

            if (repostButtonView != null)
            {
                repostButtonView.OnClicked.Subscribe(_ => OnRepostClicked()).AddTo(disposables);
                repostButtonView.OnScrolled.Subscribe(onScrolled.OnNext).AddTo(disposables);
            }

            if (likeButtonView != null)
            {
                likeButtonView.OnClicked.Subscribe(_ => OnLikeClicked()).AddTo(disposables);
                likeButtonView.OnScrolled.Subscribe(onScrolled.OnNext).AddTo(disposables);
            }
        }

        void OnPostClickedEvent()
        {
            onPostClicked.OnNext(post);
        }

        void OnRepostClicked()
        {
            onRepostedByPlayer.OnNext(post);
        }

        void OnLikeClicked()
        {
            onLikedByPlayer.OnNext(post);
        }

        void RefreshActionViews()
        {
            bool isRepostedByPlayer = post.IsRepostedByPlayer.CurrentValue;
            bool isLikedByPlayer = post.IsLikedByPlayer.CurrentValue;

            RefreshRepostedByText();
            UpdateCountText(repostCountText, post.RepostCount, isRepostedByPlayer, repostActiveColor);
            UpdateCountText(likeCountText, post.LikeCount, isLikedByPlayer, likeActiveColor);
            ApplyIconColor(repostIconImage, isRepostedByPlayer, repostActiveColor);
            ApplyIconColor(likeIconImage, isLikedByPlayer, likeActiveColor);
        }

        void RefreshRepostedByText()
        {
            var repostedByAccount = post.RepostedByAccount;
            bool hasRepostedByAccount = repostedByAccount != null && !string.IsNullOrEmpty(repostedByAccount.Name);

            repostedByText.gameObject.SetActive(hasRepostedByAccount);
            if (repostedHeaderIcon != null)
            {
                repostedHeaderIcon.SetActive(hasRepostedByAccount);
            }
            if (!hasRepostedByAccount)
            {
                return;
            }

            repostedByText.text = $"{repostedByAccount.Name} さんがリポストしました";
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
            onPostClicked.OnCompleted();
            onPostClicked.Dispose();
            onLikedByPlayer.OnCompleted();
            onLikedByPlayer.Dispose();
            onRepostedByPlayer.OnCompleted();
            onRepostedByPlayer.Dispose();
            onScrolled.OnCompleted();
            onScrolled.Dispose();
        }
    }
}
