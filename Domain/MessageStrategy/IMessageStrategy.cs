namespace Domain.MessageStrategy;

public interface IMessageStrategy
{
    int DownlinkFormat { get; }
    void Interpret(Plane plane, int[] binary);
}
