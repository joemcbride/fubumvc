using System;
using System.Collections.Generic;
using FubuMVC.Core.Packaging;
using FubuMVC.Core.Packaging.Environment;

namespace FubuTestApplication
{
    public class EnvironmentThatBlowsUpInStartUp : IEnvironment
    {
        public void Dispose()
        {
            
        }

        public IEnumerable<IInstaller> StartUp(IPackageLog log)
        {
            throw new ApplicationException("I blew up!");
        }
    }

    public class EnvironmentThatLogsAProblem : IEnvironment
    {
        public void Dispose()
        {

        }

        public IEnumerable<IInstaller> StartUp(IPackageLog log)
        {
            log.MarkFailure("I found a problem in StartUp");
            return new IInstaller[0];
        }
    }

    public class EnvironmentThatBlowsUpInCtor : IEnvironment
    {
        public EnvironmentThatBlowsUpInCtor()
        {
            throw new NotImplementedException("I blew up in the ctor");
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IInstaller> StartUp(IPackageLog log)
        {
            throw new NotImplementedException();
        }
    }

    public class EnvironmentWithAllGoodInstallers : IEnvironment
    {
        public void Dispose()
        {
        }

        public IEnumerable<IInstaller> StartUp(IPackageLog log)
        {
            log.Trace("I started up with all good installers");

            yield return new GoodInstaller1();
            yield return new GoodInstaller2();
            yield return new GoodInstaller3();
        }
    }

    public class StubEnvironment : IEnvironment
    {
        private readonly IInstaller[] _installers;

        public StubEnvironment(params IInstaller[] installers)
        {
            _installers = installers;
        }

        public void Dispose()
        {
            
        }

        public IEnumerable<IInstaller> StartUp(IPackageLog log)
        {
            return _installers;
        }
    }

    public class InstallerThatMarksFailureInLogDuringInstall : IInstaller
    {
        public void Install(IPackageLog log)
        {
            log.MarkFailure("I detected a problem during Install");
        }

        public void CheckEnvironment(IPackageLog log)
        {
        }
    }

    public class InstallerThatMarksFailureInLogDuringCheckEnvironment : IInstaller
    {
        public void Install(IPackageLog log)
        {
            
        }

        public void CheckEnvironment(IPackageLog log)
        {
            log.MarkFailure("I detected a problem during CheckEnvironment");
        }
    }

    public class InstallerThatBlowsUpInCheckEnvironment : IInstaller
    {
        public void Install(IPackageLog log)
        {
            
        }

        public void CheckEnvironment(IPackageLog log)
        {
            throw new NotImplementedException("The environment is borked!");
        }
    }

    public class InstallerThatBlowsUpInInstall : IInstaller
    {
        public void Install(IPackageLog log)
        {
            throw new NotImplementedException("You shall not pass");
        }

        public void CheckEnvironment(IPackageLog log)
        {
        }
    }

    public class GoodInstaller1 : IInstaller
    {
        public void Install(IPackageLog log)
        {
            log.Trace("All Good 1");
        }

        public void CheckEnvironment(IPackageLog log)
        {
            log.Trace("All Good 1 -- Env");
        }
    }

    public class GoodInstaller2 : IInstaller
    {
        public void Install(IPackageLog log)
        {
            log.Trace("All Good 2");
        }

        public void CheckEnvironment(IPackageLog log)
        {
            log.Trace("All Good 2 -- Env");
        }
    }

    public class GoodInstaller3 : IInstaller
    {
        public void Install(IPackageLog log)
        {
            log.Trace("All Good 3");
        }

        public void CheckEnvironment(IPackageLog log)
        {
            log.Trace("All Good 3 -- Env");
        }
    }
}