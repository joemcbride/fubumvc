using System;
using System.Collections.Generic;
using System.Linq;
using FubuCore;

namespace FubuMVC.Core.Services
{
    public static class ApplicationLoaderFinder
    {
        public static IApplicationLoader FindLoader(string bootstrapperType)
        {
            if (bootstrapperType.IsNotEmpty())
            {
                var type = Type.GetType(bootstrapperType);
                return BuildApplicationLoader(type);
            }

            var candidates = FindLoaderTypes();
            if (candidates.Count() == 1)
            {
                return BuildApplicationLoader(candidates.Single());
            }
            if (candidates.Any())
            {
                throw new Exception(
                    "Found multiple candidates, you may need to specify an explicit selection in the bottle-service.config file.  \nCandidates found are " +
                    candidates.Select(x => x.AssemblyQualifiedName).Join(",\n"));
            }
            
            Console.WriteLine("Found no loaders or application sources");

            throw new Exception("Could not find any service loader candidates");

        }

        public static IEnumerable<Type> FindLoaderTypes()
        {
            var list = new List<Type>();

            AssemblyFinder.FindAssemblies(a => !a.IsDynamic && a.GetName().Name != "Bottles").Each(assem => {
                try
                {
                    list.AddRange(assem.GetExportedTypes().Where(IsLoaderTypeCandidate));
                }
                catch (Exception)
                {
                    
                    Console.WriteLine("Unable to find exported types for assembly " + assem.FullName);
                }
            });

            return list;
        } 

        public static bool IsLoaderTypeCandidate(Type type)
        {
            if (!type.IsConcreteWithDefaultCtor()) return false;

            if (type.Closes(typeof (IApplicationSource<,>))) return true;

            return type.CanBeCastTo<IApplicationLoader>();
        }

        public static IApplicationLoader BuildApplicationLoader(Type type)
        {
            var loaderType = DetermineLoaderType(type);

            return Activator.CreateInstance(loaderType).As<IApplicationLoader>();
        }

        public static Type DetermineLoaderType(Type type)
        {
            if (type.CanBeCastTo<IApplicationLoader>()) return type;


            var @interface = type.FindInterfaceThatCloses(typeof (IApplicationSource<,>));
            if (@interface == null) throw new ArgumentOutOfRangeException("type must implement either IApplicationLoader, or IApplicationSource<TApplication,TRuntime> (FubuMVC's IApplicationSource)");

            var genericArguments = @interface.GetGenericArguments();
            return typeof (ApplicationLoader<,,>)
                .MakeGenericType(type, genericArguments.First(), genericArguments.Last());
        }
    }
}