namespace SagaNotWorking
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using NServiceBus;
    using NServiceBus.Testing;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    public class SagaTest
    {
        [Test]
        public void ExpectTimeoutFails()
        {
            var saga = new TestSaga();
            var executeOn = 31.January(2019).At(10, 35).AsLocal();
            Test.Saga(saga)
                .ExpectTimeoutToBeSetAt<TimeoutData>((state, time) => time == executeOn)
                .When((s, c) => s.Handle(new StartSagaCommand { Id = new Guid("22222222-2222-2222-2222-222222222222"), ExecutionTime = executeOn }, c));
        }

        [Test]
        public async Task ExpectTimeoutFails_WithNewTestingFramework()
        {
            var saga = new TestSaga { Data = new MySagaData { Id = new Guid("11111111-1111-1111-1111-111111111111") } };
            var executeOn = 31.January(2019).At(10, 35).AsLocal();

            var context = new TestableMessageHandlerContext();
            await saga.Handle(new StartSagaCommand { Id = new Guid("22222222-2222-2222-2222-222222222222"), ExecutionTime = executeOn }, context).ConfigureAwait(false);

            var sentMessage = context.SentMessages.SingleOrDefault();
            sentMessage.Message<TimeoutData>().Time.Should().Be(executeOn);
        }
    }

    public class TestSaga : NServiceBus.Saga<MySagaData>, IAmStartedByMessages<StartSagaCommand>, IHandleMessages<CompleteSagaCommand>, IHandleTimeouts<TimeoutData>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper)
        {
            mapper.ConfigureMapping<StartSagaCommand>(m => m.Id)
                .ToSaga(_ => _.TheId);
            mapper.ConfigureMapping<CompleteSagaCommand>(m => m.SagaId)
                .ToSaga(_ => _.TheId);
        }

        public Task Handle(StartSagaCommand message, IMessageHandlerContext context)
        {
            return this.RequestTimeout<TimeoutData>(context, message.ExecutionTime);
        }

        public Task Handle(CompleteSagaCommand message, IMessageHandlerContext context)
        {
            return Task.CompletedTask;
        }

        public Task Timeout(TimeoutData state, IMessageHandlerContext context)
        {
            return context.SendLocal(new CompleteSagaCommand { SagaId = state.SagaId });
        }
    }

    public class MySagaData : ContainSagaData
    {
        public Guid TheId { get; set; }
    }

    public class CompleteSagaCommand
    {
        public Guid SagaId { get; set; }
    }

    public class StartSagaCommand
    {
        public Guid Id { get; set; }
        public DateTime ExecutionTime { get; set; }
    }
}