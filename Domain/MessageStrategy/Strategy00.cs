namespace Domain.MessageStrategy;

public class Strategy00 : IMessageStrategy
{
    public int DownlinkFormat => 0;

    public void Interpret(Plane plane, int[] binary)
    {
        var ac = PlaneFrameDecoder.ExtractValue(binary,20,12);

        if (ac != 0)
        {
            plane.Altitude = PlaneFrameDecoder.decodeAC12Field(ac);
        }
    }
}
