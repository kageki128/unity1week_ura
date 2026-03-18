using VContainer;
using VContainer.Unity;

using Unity1Week_Ura.Director;
using Unity1Week_Ura.Core;
using Unity1Week_Ura.Actor;
using Unity1Week_Ura.Infrastructure;
using System;
using UnityEngine;

namespace Unity1Week_Ura.CompositionRoot
{
    public class MainLifeTimeScope : LifetimeScope
    {
        [SerializeField] SmartPhoneView smartPhoneView;

        [SerializeField] GameRuleSO defaultGameRule;
        [SerializeField] AddressableConfigSO addressableConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            RegisterCore(builder);
            RegisterActor(builder);
            RegisterDirector(builder);
        }

        void RegisterCore(IContainerBuilder builder)
        {
            builder.Register<SceneModel>(Lifetime.Singleton);
            builder.Register<GameSessionModel>(Lifetime.Singleton);
            builder.Register<AddressableSpriteLabelLoader>(Lifetime.Singleton);
            builder.Register<IAccountRepository, AccountRepository>(Lifetime.Singleton);
            builder.Register<IPostRepository, PostRepository>(Lifetime.Singleton);
            builder.RegisterInstance(defaultGameRule);
            builder.RegisterInstance(addressableConfig);

        }
        void RegisterActor(IContainerBuilder builder)
        {
            builder.RegisterInstance(smartPhoneView);
        }
        void RegisterDirector(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<MainEntryPoint>();
            builder.Register<UIDirector>(Lifetime.Singleton);
            builder.Register<TitleSceneDirector>(Lifetime.Singleton);
            builder.Register<SelectSceneDirector>(Lifetime.Singleton);
            builder.Register<GameSceneDirector>(Lifetime.Singleton);
        }

    }
}