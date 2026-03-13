using MelbergFramework.Infrastructure.Rabbit.Messages;
using MelbergFramework.Infrastructure.Rabbit.Publishers;

namespace Infrastructure.Rabbit.Publishers;

public interface IWorkMessagePublisher
{
    void SendCommand(long time);
}

public class WorkMessagePublisher(IStandardPublisher<WorkMessage> publisher): IWorkMessagePublisher 
{
    public void SendCommand(long time) => publisher.Send(new(){ Time = time});
}

public class WorkMessage : StandardMessage
{
    public override string GetRoutingKey() => "HerderWorkCommand";

    public long Time { get; set; }
}

