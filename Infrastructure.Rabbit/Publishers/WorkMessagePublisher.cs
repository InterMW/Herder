using MelbergFramework.Infrastructure.Rabbit.Messages;
using MelbergFramework.Infrastructure.Rabbit.Publishers;

namespace Infrastructure.Rabbit.Publishers;

public interface IWorkMessagePublisher
{
    void SendCommand();
}

public class WorkMessagePublisher(IStandardPublisher<WorkMessage> publisher): IWorkMessagePublisher 
{
    public void SendCommand() => publisher.Send(new());
}

public class WorkMessage : StandardMessage
{
    public override string GetRoutingKey() => "HerderWorkCommand";
}

