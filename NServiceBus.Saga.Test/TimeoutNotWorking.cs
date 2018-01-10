namespace SagaNotWorking
{
    using System;
    using System.Threading.Tasks;
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
            var executeOn = DateTime.Now.AddHours(1);
            Test.Saga(saga)
                .ExpectTimeoutToBeSetAt<TimeoutData>((state, time) => time == executeOn)
                .When((s, c) => s.Handle(new StartSagaCommand { Id = Guid.NewGuid(), ExecutionTime = executeOn }, c));
        }
    }

    public class TestSaga : NServiceBus.Saga<MySagaData>, IAmStartedByMessages<StartSagaCommand>, IHandleMessages<CompleteSagaCommand>, IHandleTimeouts<TimeoutData>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper)
        {
            mapper.ConfigureMapping<StartSagaCommand>(m => m.Id);
            mapper.ConfigureMapping<CompleteSagaCommand>(m => m.Id);
        }

        public Task Handle(StartSagaCommand message, IMessageHandlerContext context)
        {
            this.Data.Id = message.Id;
            return this.RequestTimeout<TimeoutData>(context, message.ExecutionTime);
        }

        public Task Handle(CompleteSagaCommand message, IMessageHandlerContext context)
        {
            return Task.CompletedTask;
        }

        public Task Timeout(TimeoutData state, IMessageHandlerContext context)
        {
            context.SendLocal(new CompleteSagaCommand { Id = state.SagaId });
            return Task.CompletedTask;
        }
    }

    public class MySagaData : IContainSagaData
    {
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        public Guid Id { get; set; }
    }
    public class CompleteSagaCommand
    {
        public Guid Id { get; set; }
    }

    public class StartSagaCommand
    {
        public Guid Id { get; set; }
        public DateTime ExecutionTime { get; set; }
    }
}