﻿using System;
using System.Linq;
using System.Collections.Generic;
using FubuCore.Conversion;

namespace StoryTeller.Engine
{
    public class SystemLifecycle : IDisposable
    {
        private readonly ISystem _system;
        private bool _environmentIsInitialized;
        private readonly object _locker = new object();

        public SystemLifecycle(ISystem system)
        {
            _system = system;
        }

        protected void ensureEnvironmentInitialized()
        {
            if (!_environmentIsInitialized)
            {
                lock (_locker)
                {
                    if (!_environmentIsInitialized)
                    {
                        _system.SetupEnvironment();
                        _environmentIsInitialized = true;
                    }
                }
            }
        }

        public void StartApplication()
        {
            ensureEnvironmentInitialized();
        }

        protected void tearDownEnvironment()
        {
            lock (_locker)
            {
                _system.TeardownEnvironment();
                _environmentIsInitialized = false;
            }
        }

        public void RecycleEnvironment()
        {
            tearDownEnvironment();
            ensureEnvironmentInitialized();
        }

        public void ExecuteContext(ITestContext context, Action action)
        {
            ensureEnvironmentInitialized();
            _system.RegisterServices(context);

            try
            {
                _system.Setup();

                var startups = context.StartupActionTypes.Select(x => _system.GetAction(x));

                startups.Each(x => x.Startup(context));

                action();

                // TODO -- this might need to go to system teardown
                startups.Each(x => x.Teardown(context));
            }
            finally
            {
                _system.Teardown();
            }
        }

        public void Dispose()
        {
            tearDownEnvironment();
        }

        public Func<Type, object> Resolver
        {
            get
            {
                return t => _system.Get(t);
            }
        }

        public void SetupEnvironment()
        {
            _system.SetupEnvironment();
        }

        public void RegisterServices(ITestContext context)
        {
            _system.RegisterServices(context);
        }

        public IObjectConverter BuildConverter()
        {
            return _system.BuildConverter();
        }
    }
}