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
        [Header("View")]
        [SerializeField] TitleViewHub titleViewHub;
        [SerializeField] SelectViewHub selectViewHub;
        [SerializeField] GameViewHub gameViewHub;
        [SerializeField] ResultViewHub resultViewHub;
        [Header("Factory")]
        [SerializeField] PostViewFactory postViewFactory;

        [Header("SO")]
        [SerializeField] GameConfigSO gameConfig;
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
            builder.Register<GameSession>(Lifetime.Singleton);
            builder.Register<AddressableSpriteLabelLoader>(Lifetime.Singleton);
            builder.Register<IAccountRepository, AccountRepository>(Lifetime.Singleton);
            builder.Register<IPostRepository, PostRepository>(Lifetime.Singleton);
            builder.Register<ISocialSharePort, XSharePort>(Lifetime.Singleton);
            builder.RegisterInstance(gameConfig);
            builder.RegisterInstance(addressableConfig);

        }
        void RegisterActor(IContainerBuilder builder)
        {
            builder.RegisterInstance(titleViewHub);
            builder.RegisterInstance(selectViewHub);
            builder.RegisterInstance(gameViewHub);
            builder.RegisterInstance(resultViewHub);
            builder.RegisterComponent(postViewFactory);
        }
        void RegisterDirector(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<MainEntryPoint>();
            builder.Register<TitleSceneDirector>(Lifetime.Singleton);
            builder.Register<SelectSceneDirector>(Lifetime.Singleton);
            builder.Register<GameSceneDirector>(Lifetime.Singleton);
            builder.Register<ResultSceneDirector>(Lifetime.Singleton);
        }

    }
}