using Domain;
using MelbergFramework.Infrastructure.Rabbit.Messages;
using MelbergFramework.Infrastructure.Rabbit.Publishers;

namespace Infrastructure.Rabbit.Publishers;

public interface ICompletePlaneFramePublisher
{
    void SendFrame(SkyFrame frame);
}

public class CompletePlaneFramePublisher(IStandardPublisher<CompletedPlaneFrameMessage> publisher) :
    ICompletePlaneFramePublisher
{
    public void SendFrame(SkyFrame frame) => publisher.Send(new() { Planes = frame.Planes, Now = frame.Timestamp });
}

public class CompletedPlaneFrameMessage : StandardMessage
{
    public IEnumerable<Plane> Planes = Array.Empty<Plane>();
    public long Now;
    public override string GetRoutingKey() => "planeframe.complete";
}
