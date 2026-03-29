namespace Domain.MessageStrategy;

public class Strategy05 : IMessageStrategy
{
    public int DownlinkFormat => 5;

    public void Interpret(Plane plane, int[] binary)
    {
        var id = PlaneFrameDecoder.ExtractValue(binary,19,13);

        if (id != 0)
        {
            plane.Squawk = string.Format("{0:x4}", PlaneFrameDecoder.decodeID13Field(id));
        }
    }
}
