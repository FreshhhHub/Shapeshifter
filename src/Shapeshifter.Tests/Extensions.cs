﻿namespace Shapeshifter.WindowsDesktop
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Autofac;

    using Controls.Designer.Services;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;
    using Infrastructure.Dependencies;

    static class Extensions
    {
        static readonly IDictionary<Type, object> fakeCache;

        static Extensions()
        {
            fakeCache = new Dictionary<Type, object>();
        }

        internal static void ClearCache()
        {
            fakeCache.Clear();
        }

        public static string GetSolutionRoot()
        {
            var currentPath = Environment.CurrentDirectory;

            var found = false;
            while (!found)
            {
                var files = Directory.GetFiles(currentPath);
                if (files
                    .Select(Path.GetFileName)
                    .Contains("README.md"))
                {
                    found = true;
                }
                else
                {
                    currentPath = Path.GetDirectoryName(currentPath);
                }
            }

            return Path.Combine(currentPath, "src");
        }

        public static void AssertWait(Action expression)
        {
            AssertWait(10000, expression);
        }

        static IReadOnlyCollection<Type> GetImplementingTypes(Type type)
        {
            var assemblies = AppDomain
                .CurrentDomain.GetAssemblies()
                .Where(x => x.FullName.StartsWith(nameof(Shapeshifter)));
            var types = assemblies
                .SelectMany(x => x.GetTypes())
                .Where(x => type.IsAssignableFrom(x) && x.IsClass)
                .Where(x => !x.GetInterfaces().Contains(typeof(IDesignerService)));
            return types.ToArray();
        }

        public static void RegisterFakesForDependencies<TClass>(
            this ContainerBuilder builder,
            params Type[] exceptTypes) where TClass : class
        {
            var type = typeof(TClass);

            Type classType;
            if (type.IsClass)
            {
                classType = type;
            }
            else
            {
                var implementingTypes = GetImplementingTypes(type);
                if (!implementingTypes.Any())
                {
                    return;
                }

                if (implementingTypes.Count > 1)
                {
                    throw new InvalidOperationException(
                        $"The type {typeof(TClass).Name} has several implementing types.");
                }

                classType = implementingTypes.Single();
            }

            var constructors = classType
                .GetConstructors()
                .Where(x => x.IsPublic && !x.IsStatic);
            var targetConstructor = constructors
                .OrderByDescending(x => x.GetParameters().Length)
                .First();
            var parameters = targetConstructor.GetParameters();
            foreach (var parameter in parameters)
            {
                RegisterFakeForType(builder, exceptTypes, parameter.ParameterType);
            }

            var properties = classType
                .GetProperties()
                .Where(x => x
                    .GetCustomAttributes(true)
                    .Any(a => a is InjectAttribute));
            foreach(var property in properties)
            {
                RegisterFakeForType(builder, exceptTypes, property.PropertyType);
            }
        }

        private static void RegisterFakeForType(ContainerBuilder builder, Type[] exceptTypes, Type type)
        {
            if (!exceptTypes.Contains(type))
            {
                var genericMethod = typeof(Extensions)
                    .GetMethods()
                    .Single(
                        x =>
                        (x.Name == nameof(RegisterFake)) &&
                        (x.GetParameters()
                          .Length == 1))
                    .MakeGenericMethod(type);
                genericMethod.Invoke(
                    null,
                    new object[]
                    {
                            builder
                    });
            }
            else
            {
                var genericMethod = typeof(Extensions)
                    .GetMethods()
                    .Single(x =>
                        x.Name == nameof(RegisterFakesForDependencies))
                    .MakeGenericMethod(type);
                genericMethod.Invoke(
                    null,
                    new object[]
                    {
                            builder,
                            exceptTypes
                    });
            }
        }

        public static void AssertWait(int timeout, Action expression)
        {
            var time = DateTime.Now;

            var exceptions = new List<AssertFailedException>();
            while (
                ((DateTime.Now - time).TotalMilliseconds < timeout) ||
                (exceptions.Count == 0))
            {
                try
                {
                    expression();
                    return;
                }
                catch (AssertFailedException ex)
                {
                    if (exceptions.All(x => x.Message != ex.Message))
                    {
                        exceptions.Add(ex);
                    }
                }

                Thread.Sleep(1);
            }

            if (exceptions.Count > 1)
            {
                throw new AggregateException(
                    "The assertion timeout of " + timeout + " milliseconds was exceeded, and multiple exceptions were caught.",
                    exceptions);
            }

            throw exceptions.First();
        }

        public static TInterface With<TInterface>(this TInterface item, Action<TInterface> method)
        {
            method(item);
            return item;
        }

        public static TInterface RegisterFake<TInterface>(this ContainerBuilder builder)
            where TInterface : class
        {
            if (fakeCache.ContainsKey(typeof (TInterface)))
            {
                return (TInterface) fakeCache[typeof (TInterface)];
            }

            if (!typeof (TInterface).IsSealed)
            {
                var fake = Substitute.For<TInterface>();
                var collection = new ReadOnlyCollection<TInterface>(
                    new List<TInterface>(
                        new[]
                        {
                            fake
                        }));

                Register(builder, fake);
                Register<IReadOnlyCollection<TInterface>>(builder, collection);
                Register<IEnumerable<TInterface>>(builder, collection);

                return fake;
            }
            else
            {
                return null;
            }
        }

        static void Register<TInterface>(ContainerBuilder builder, TInterface fake) where TInterface : class
        {
            if (fakeCache.ContainsKey(typeof (TInterface)))
            {
                fakeCache.Add(typeof(TInterface), fake);
                return;
            }

            builder.Register(c => fake)
                   .As<TInterface>()
                   .SingleInstance();
        }

        public static void IgnoreAwait(this Task task) { }
    }
}