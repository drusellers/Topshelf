// Copyright 2007-2010 The Apache Software Foundation.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace Topshelf.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using log4net;
    using Magnum.Channels;
    using Magnum.Concurrency;
    using Magnum.Extensions;
    using Magnum.Threading;
    using Messages;
    using Shelving;


    [DebuggerDisplay("Hosting {HostedServiceCount} Services")]
    public class OldServiceCoordinator :
        IOldServiceCoordinator
    {
        static readonly ILog _log = LogManager.GetLogger(typeof(OldServiceCoordinator));
        readonly Action<IOldServiceCoordinator> _beforeStartingServices;
        readonly Action<IOldServiceCoordinator> _afterStartingServices;
        readonly Action<IOldServiceCoordinator> _afterStoppingServices;

        readonly ReaderWriterLockedObject<Queue<Exception>> _exceptions =
            new ReaderWriterLockedObject<Queue<Exception>>(new Queue<Exception>());

        readonly InboundChannel _hostChannel;
        readonly List<Func<IServiceController>> _serviceConfigurators;

        readonly IList<IServiceController> _services = new List<IServiceController>();
        readonly TimeSpan _timeout;

    	public OldServiceCoordinator(Action<IOldServiceCoordinator> beforeStartingServices,
                                  Action<IOldServiceCoordinator> afterStartingServices, 
                                  Action<IOldServiceCoordinator> afterStoppingServices,
                                  TimeSpan waitTime)
        {
            ServiceStartedAction += msg => { };
            ServiceStoppedAction += msg => { };
            ServicePausedAction += msg => { };

            _beforeStartingServices = GetLogWrapper("BeforeStartingServices", beforeStartingServices);
            _afterStartingServices = GetLogWrapper("AfterStartingServices", afterStartingServices);
            _afterStoppingServices = GetLogWrapper("AfterStoppingServices", afterStoppingServices);

            _serviceConfigurators = new List<Func<IServiceController>>();

			_timeout = waitTime;
			_hostChannel = AddressRegistry.GetServiceCoordinatorHost(s =>
                {
                    s.AddConsumerOf<ShelfFault>().UsingConsumer(HandleServiceFault);
                    s.AddConsumerOf<ServiceRunning>().UsingConsumer(msg => ServiceStartedAction.Invoke(msg));
                    s.AddConsumerOf<ServiceStopped>().UsingConsumer(msg => ServiceStoppedAction.Invoke(msg));
                    s.AddConsumerOf<ServicePaused>().UsingConsumer(msg => ServicePausedAction.Invoke(msg));
                });
        }

        public IList<IServiceController> Services
        {
            get
            {
                LoadNewServiceConfigurations();

                return _services;
            }
        }

        public void Start()
        {
            _beforeStartingServices(this);

          //  ProcessEvent<StartService, ServiceRunning>("Start", "Starting", ref ServiceStartedAction, ServiceState.Started);

            _afterStartingServices(this);
        }

        public void Stop()
        {
          //  ProcessEvent<StopService, ServiceStopped>("Stop", "Stopping", ref ServiceStoppedAction, ServiceState.Stopped);

            _afterStoppingServices(this);

        }

        public void Pause()
        {
         //   ProcessEvent<PauseService, ServicePaused>("Pause", "Pausing", ref ServicePausedAction, ServiceState.Paused);
        }

        public void Continue()
        {
          //  ProcessEvent<ContinueService, ServiceRunning>("Continue", "Continuing", ref ServiceContinuedAction,ServiceState.Started);
        }

        public void StartService(string name)
        {
            if (Services.Count == 0)
                CreateServices();

         //   Services.Where(x => x.Name == name).First().Send(new StartService());
        }

        public void StopService(string name)
        {
            if (Services.Count == 0)
                CreateServices();

         //   Services.Where(x => x.Name == name).First().Send(new StopService());
        }

        public void PauseService(string name)
        {
            if (Services.Count == 0)
                CreateServices();

         //   Services.Where(x => x.Name == name).First().Send( new PauseService());
        }

        public void ContinueService(string name)
        {
            if (Services.Count == 0)
                CreateServices();

          //  Services.Where(x => x.Name == name).First().Send(new ContinueService());
        }

        public int HostedServiceCount
        {
            get { return Services.Count; }
        }

        public IList<ServiceInformation> GetServiceInfo()
        {
            LoadNewServiceConfigurations();

            return Services
                .ToList()
                .ConvertAll(serviceController => new ServiceInformation
                    {
                        Name = serviceController.Name,
            //            State = serviceController.State,
                     //   Type = serviceController.ServiceType.Name
                    });
        }

        public IServiceController GetService(string name)
        {
            return Services.Where(x => x.Name == name).FirstOrDefault();
        }

        #region Dispose

        bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            if (disposing)
            {
                Services.Each(s => s.Dispose());
                Services.Clear();

                if (_hostChannel != null)
                    _hostChannel.Dispose();
            }
            _disposed = true;
        }

        ~OldServiceCoordinator()
        {
            Dispose(false);
        }

        #endregion

//        void ProcessEvent<TSent, TRecieved>(string printableMethod, string printableAction,
//                                            ref Action<TRecieved> stateEvent, ServiceState targetState)
//            where TSent : ServiceCommand
//			where TRecieved : ServiceEvent
//		{
//            int servicesNotInTargetState = Services.Count(x => x.State != targetState);
//            bool completed;
//            long serviceReachedTargetState = 0;
//
//            using (var latch = new ManualResetEvent(false))
//            {
//                var countDown = new CountdownLatch(servicesNotInTargetState, () => latch.Set());
//
//                Action<TRecieved> action = msg =>
//                    {
//                        countDown.CountDown();
//                        Interlocked.Increment(ref serviceReachedTargetState);
//                    };
//
//                stateEvent += action;
//
//                _log.Debug("{0} is now {1} all '{2}' subordinate services".FormatWith(printableMethod, printableAction.ToLower(), Services.Count));
//                foreach (IService serviceController in Services)
//                {
//                    _log.InfoFormat("{1} subordinate service '{0}'", serviceController.Name, printableAction);
//                    serviceController.Send(default(TSent));
//                }
//
//                completed = latch.WaitOne(_timeout);
//                stateEvent -= action;
//            }
//
//            if (!completed && (HostedServiceCount == 1 || serviceReachedTargetState == 0))
//            {
//                int qCount = _exceptions.ReadLock(q => q.Count);
//                Exception ex = null;
//
//                if (qCount > 0)
//                    ex = _exceptions.WriteLock(s => s.Dequeue());
//
//                throw new Exception(
//                    "One or more services failed to {0} in a timely manner.".FormatWith(printableMethod), ex);
//            }
//
//            if (!Services.Any(x => x.State == targetState))
//                throw new Exception("All services have errored out.", _exceptions.ReadLock(q => q.Dequeue()));
//        }

        event Action<ServiceRunning> ServiceStartedAction;
        event Action<ServiceStopped> ServiceStoppedAction;
        event Action<ServicePaused> ServicePausedAction;
        event Action<ServiceRunning> ServiceContinuedAction;
        public event Action<Exception> ShelfFaulted;

        void LoadNewServiceConfigurations()
        {
            if (_serviceConfigurators.Any())
            {
                foreach (var serviceConfigurator in _serviceConfigurators)
                {
                    IServiceController serviceController = serviceConfigurator();
                    _services.Add(serviceController);
                }

                _serviceConfigurators.Clear();
            }
        }

        public void AddNewService(IServiceController controller)
        {
            _services.Add(controller);
            //TODO: How to best call start here?
        }

        public void RegisterServices(IList<Func<IServiceController>> services)
        {
            _serviceConfigurators.AddRange(services);
        }

        void CreateServices()
        {
            foreach (var serviceConfigurator in _serviceConfigurators)
            {
                IServiceController serviceController = serviceConfigurator();
                Services.Add(serviceController);
            }
        }

        void HandleServiceFault(ShelfFault faultMessage)
        {
            _exceptions.WriteLock(s => s.Enqueue(faultMessage.Exception));

            Action<Exception> handle = ShelfFaulted;
            if (handle != null)
                handle.Invoke(faultMessage.Exception);
        }

        Action<IOldServiceCoordinator> GetLogWrapper(string name, Action<IOldServiceCoordinator> action)
        {
            return sc =>
            {
                _log.DebugFormat("Calling {0}", name);
                action(sc);
                _log.InfoFormat("{0} complete", name);
            };
        }
    }
}