using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Unity1Week_Ura.Actor
{
    public class TimedViewAnimationPlayer : MonoBehaviour
    {
        [Serializable]
        public class TimedViewAnimation
        {
            [Tooltip("Animation target (AnimationViewBase).")]
            [SerializeField] AnimationViewBase view;

            [Tooltip("Start timing in seconds from sequence start. 0 means immediate start.")]
            [SerializeField, Min(0f)] float startAtSeconds;

            public AnimationViewBase View => view;
            public float StartAtSeconds => startAtSeconds;
        }

        [Header("Show Sequence")]
        [Tooltip("Animations played by PlayShowAsync. Start timing allows simultaneous starts.")]
        [SerializeField] List<TimedViewAnimation> showAnimations = new();

        [Header("Hide Sequence")]
        [Tooltip("Animations played by PlayHideAsync. Start timing allows simultaneous starts.")]
        [SerializeField] List<TimedViewAnimation> hideAnimations = new();

        public bool HasShowAnimations => showAnimations.Count > 0;
        public bool HasHideAnimations => hideAnimations.Count > 0;

        public delegate UniTask AnimationPlayHandler(AnimationViewBase view, bool isShow, CancellationToken ct);

        public void InitializeRegisteredViews()
        {
            var initializedViews = new HashSet<AnimationViewBase>();
            InitializeRegisteredViews(initializedViews);
        }

        public void InitializeRegisteredViews(ISet<AnimationViewBase> initializedViews)
        {
            InitializeSequence(showAnimations, initializedViews);
            InitializeSequence(hideAnimations, initializedViews);
        }

        public UniTask PlayShowAsync(CancellationToken ct)
        {
            return PlayShowAsync(DefaultPlayHandler, ct);
        }

        public UniTask PlayHideAsync(CancellationToken ct)
        {
            return PlayHideAsync(DefaultPlayHandler, ct);
        }

        public UniTask PlayShowAsync(AnimationPlayHandler playHandler, CancellationToken ct)
        {
            return PlaySequenceAsync(showAnimations, isShow: true, playHandler, ct);
        }

        public UniTask PlayHideAsync(AnimationPlayHandler playHandler, CancellationToken ct)
        {
            return PlaySequenceAsync(hideAnimations, isShow: false, playHandler, ct);
        }

        static void InitializeSequence(IReadOnlyList<TimedViewAnimation> animations, ISet<AnimationViewBase> initializedViews)
        {
            for (var i = 0; i < animations.Count; i++)
            {
                var view = animations[i]?.View;
                if (view == null || initializedViews.Contains(view))
                {
                    continue;
                }

                view.Initialize();
                initializedViews.Add(view);
            }
        }

        async UniTask PlaySequenceAsync(
            IReadOnlyList<TimedViewAnimation> animations,
            bool isShow,
            AnimationPlayHandler playHandler,
            CancellationToken ct)
        {
            if (playHandler == null)
            {
                playHandler = DefaultPlayHandler;
            }

            var tasks = new List<UniTask>(animations.Count);
            var sequenceStartTime = Time.realtimeSinceStartupAsDouble;

            for (var i = 0; i < animations.Count; i++)
            {
                var animation = animations[i];
                if (animation == null || animation.View == null)
                {
                    continue;
                }

                tasks.Add(PlaySingleAsync(animation, isShow, sequenceStartTime, playHandler, ct));
            }

            if (tasks.Count == 0)
            {
                return;
            }

            await UniTask.WhenAll(tasks);
        }

        async UniTask PlaySingleAsync(
            TimedViewAnimation animation,
            bool isShow,
            double sequenceStartTime,
            AnimationPlayHandler playHandler,
            CancellationToken ct)
        {
            var elapsedSeconds = Time.realtimeSinceStartupAsDouble - sequenceStartTime;
            var remainingSeconds = animation.StartAtSeconds - (float)elapsedSeconds;
            if (remainingSeconds > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(remainingSeconds), DelayType.DeltaTime, PlayerLoopTiming.Update, ct);
            }

            await playHandler(animation.View, isShow, ct);
        }

        static UniTask DefaultPlayHandler(AnimationViewBase view, bool isShow, CancellationToken ct)
        {
            return isShow ? view.ShowAsync(ct) : view.HideAsync(ct);
        }
    }
}
