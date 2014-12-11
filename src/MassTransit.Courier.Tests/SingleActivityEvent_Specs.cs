﻿// Copyright 2007-2014 Chris Patterson, Dru Sellers, Travis Smith, et. al.
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
namespace MassTransit.Courier.Tests
{
    using System;
    using System.Threading.Tasks;
    using Contracts;
    using NUnit.Framework;
    using Testing;


    [TestFixture]
    public class Executing_a_routing_slip_with_a_single_activity :
        ActivityTestFixture
    {
        [Test]
        public async void Should_receive_the_routing_slip_activity_completed_event()
        {
            ConsumeContext<RoutingSlipActivityCompleted> context = await _activityCompleted;

            Assert.AreEqual(_trackingNumber, context.Message.TrackingNumber);
        }

        [Test]
        public async void Should_receive_the_routing_slip_activity_log()
        {
            ConsumeContext<RoutingSlipActivityCompleted> context = await _activityCompleted;

            Assert.AreEqual("Hello", context.Message.GetResult<string>("OriginalValue"));
        }

        [Test]
        public async void Should_receive_the_routing_slip_activity_variable()
        {
            ConsumeContext<RoutingSlipActivityCompleted> context = await _activityCompleted;

            Assert.AreEqual("Knife", context.Message.GetVariable<string>("Variable"));
        }

        [Test]
        public async void Should_receive_the_routing_slip_completed_event()
        {
            ConsumeContext<RoutingSlipCompleted> context = await _completed;

            Assert.AreEqual(_trackingNumber, context.Message.TrackingNumber);
        }

        [Test]
        public async void Should_receive_the_routing_slip_timestamps()
        {
            ConsumeContext<RoutingSlipActivityCompleted> context = await _activityCompleted;
            ConsumeContext<RoutingSlipCompleted> completeContext = await _completed;

            Assert.AreEqual(completeContext.Message.Timestamp, context.Message.Timestamp + context.Message.Duration);
        }

        [Test]
        public async void Should_receive_the_routing_slip_variable()
        {
            ConsumeContext<RoutingSlipCompleted> context = await _completed;

            Assert.AreEqual("Knife", context.Message.GetVariable<string>("Variable"));
        }

        Task<ConsumeContext<RoutingSlipCompleted>> _completed;
        Task<ConsumeContext<RoutingSlipActivityCompleted>> _activityCompleted;
        Guid _trackingNumber;

        [TestFixtureSetUp]
        public async void Should_publish_the_completed_event()
        {
            _completed = SubscribeHandler<RoutingSlipCompleted>();
            _activityCompleted = SubscribeHandler<RoutingSlipActivityCompleted>();

            _trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(_trackingNumber);
            builder.AddSubscription(Bus.Address, RoutingSlipEvents.All);

            ActivityTestContext testActivity = GetActivityContext<TestActivity>();
            builder.AddActivity(testActivity.Name, testActivity.ExecuteUri, new
            {
                Value = "Hello",
            });

            builder.AddVariable("Variable", "Knife");

            await Bus.Execute(builder.Build());

            await _completed;
        }

        protected override void SetupActivities()
        {
            AddActivityContext<TestActivity, TestArguments, TestLog>(() => new TestActivity());
        }
    }
}