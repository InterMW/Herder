namespace Domain.MessageStrategy;

public class Strategy04 : IMessageStrategy
{
    public int DownlinkFormat => 4;

    public void Interpret(Plane plane, int[] binary)
    {
        var ac = PlaneFrameDecoder.ExtractValue(binary,19,13);

        if (ac != 0)
        {
            plane.Altitude = PlaneFrameDecoder.decodeAC13Field(ac);
        }
    }
}
