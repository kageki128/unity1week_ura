using VContainer;
using VContainer.Unity;

using Unity1Week_Ura.Director;
using Unity1Week_Ura.Core;

namespace Unity1Week_Ura.CompositionRoot
{
    public class MainLifeTimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            RegisterCore(builder);
            RegisterDirector(builder);
        }

        void RegisterCore(IContainerBuilder builder)
        {
            builder.Register<SceneModel>(Lifetime.Singleton);
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