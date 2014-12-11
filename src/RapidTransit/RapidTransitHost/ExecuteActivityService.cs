// Copyright 2007-2014 Chris Patterson, Dru Sellers, Travis Smith, et. al.
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
namespace RapidTransit
{
    using System;
    using Configuration;
    using MassTransit;
    using MassTransit.Courier;
    using MassTransit.Logging;
    using Topshelf;


    /// <summary>
    /// For an activity that has no compensation, only create the execute portion of the activity
    /// </summary>
    /// <typeparam name="TActivity"></typeparam>
    /// <typeparam name="TArguments"></typeparam>
    public class ExecuteActivityService<TActivity, TArguments> :
        ServiceControl,
        IDisposable
        where TActivity : ExecuteActivity<TArguments>
        where TArguments : class
    {
        readonly string _activityName;
        readonly IActivityQueueNameProvider _activityUriProvider;
        readonly ExecuteActivityFactory<TArguments> _executeActivityFactory;
        readonly int _executeConsumerLimit;
        readonly string _executeQueueName;
        readonly ILog _log;
        readonly ITransportConfigurator _transportFactory;
        bool _disposed;
        IServiceBus _executeBus;

        public ExecuteActivityService(IConfigurationProvider configuration, ITransportConfigurator transportFactory,
            IActivityQueueNameProvider activityUriProvider, ExecuteActivityFactory<TArguments> executeActivityFactory)
        {
            _log = Logger.Get(GetType());

            _transportFactory = transportFactory;
            _activityUriProvider = activityUriProvider;
            _executeActivityFactory = executeActivityFactory;

            _activityName = GetActivityName();

            _executeQueueName = _activityUriProvider.GetExecuteActivityQueueName(_activityName);
            _executeConsumerLimit = GetExecuteConsumerLimit(configuration);
        }

        public virtual void Dispose()
        {
            if (_disposed)
                return;

            if (_executeBus != null)
                _executeBus.Dispose();

            _disposed = true;
        }

        public virtual bool Start(HostControl hostControl)
        {
//            _executeBus = CreateExecuteServiceBus();

            return true;
        }

        public virtual bool Stop(HostControl hostControl)
        {
            if (_executeBus != null)
            {
                _log.InfoFormat("Stopping Execute {0} Service Bus", _activityName);
                _executeBus.Dispose();
                _executeBus = null;
            }

            return true;
        }

        string GetActivityName()
        {
            string activityName = typeof(TActivity).Name;
            if (activityName.EndsWith("Service", StringComparison.OrdinalIgnoreCase))
                activityName = activityName.Substring(0, activityName.Length - "Service".Length);
            return activityName;
        }

        int GetExecuteConsumerLimit(IConfigurationProvider configurationProvider)
        {
            string settingName = string.Format("{0}ConsumerLimit", _activityName);

            return configurationProvider.GetSetting(settingName, Environment.ProcessorCount);
        }

        protected virtual void CreateExecuteServiceBus()
        {
            _log.InfoFormat("Creating Execute {0} Receive Endpoint", _activityName);

            Uri compensateAddress = null; // compensateServiceBus.Endpoint.Address;

            _transportFactory.Configure(_executeQueueName, _executeConsumerLimit, x =>
            {
                x.ExecuteActivityHost<TActivity, TArguments>(compensateAddress, _executeActivityFactory);
            });
        }
    }
}