using Autofac;
using LuisBot.Abstract;
using LuisBot.Entities;
using LuisBot.Entities.User.Entities;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Builder.Internals.Fibers;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Sample.LuisBot;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace LuisBot.Infrastructure
{
    internal sealed class MainModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder.Register(c => new LuisModelAttribute(ConfigurationManager.AppSettings["luis:SubscriptionId"],
            ConfigurationManager.AppSettings["luis:ModelId"])).AsSelf().AsImplementedInterfaces().SingleInstance();

            // Top Level Dialog
            builder.RegisterType<BasicLuisDialog>().As<IDialog<object>>().InstancePerDependency();


            //Other Dialogs
            //builder.Register((c, d) => new RandomLuisDialog(d.Resolve<ILuisService>, c.Resolve<IBotRepository>())).AsSelf().InstancePerDependency();
            

            // Singlton services
            builder.RegisterType<LuisService>().Keyed<ILuisService>(FiberModule.Key_DoNotSerialize).AsImplementedInterfaces().SingleInstance();

            // register some objects dependent on the incoming message

            //builder.RegisterType<IFactRepository>().Keyed<IFactRepository>(FiberModule.Key_DoNotSerialize).AsImplementedInterfaces().InstancePerMatchingLifetimeScope(DialogModule.LifetimeScopeTag);



        }
    }
}