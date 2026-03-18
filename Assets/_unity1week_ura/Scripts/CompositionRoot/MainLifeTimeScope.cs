using VContainer;
using VContainer.Unity;

using Unity1Week_Ura.Director;
using Unity1Week_Ura.Core;
using Unity1Week_Ura.Actor;
using System;
using UnityEngine;

namespace Unity1Week_Ura.CompositionRoot
{
    public class MainLifeTimeScope : LifetimeScope
    {
        [SerializeField] SmartPhoneView smartPhoneView;

        protected override void Configure(IContainerBuilder builder)
        {
            RegisterCore(builder);
            RegisterActor(builder);
            RegisterDirector(builder);
        }

        void RegisterCore(IContainerBuilder builder)
        {
            builder.Register<SceneModel>(Lifetime.Singleton);
        }
        void RegisterActor(IContainerBuilder builder)
        {
            builder.RegisterInstance(smartPhoneView);
        }
        void RegisterDirector(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<MainEntryPoint>();
            builder.Register<TitleSceneDirector>(Lifetime.Singleton);
            builder.Register<SelectSceneDirector>(Lifetime.Singleton);
            builder.Register<GameSceneDirector>(Lifetime.Singleton);
        }

    }
}