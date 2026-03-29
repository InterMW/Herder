namespace Domain.MessageStrategy;

public interface IMessageDecoder
{
    void DecodeMessage(Plane plane, string frame);
}

public class MessageDecoder(IEnumerable<IMessageStrategy> strategies) : IMessageDecoder
{
    private Dictionary<int, IMessageStrategy> Strategies = strategies.ToDictionary(_ => _.DownlinkFormat);

    public void DecodeMessage(Plane plane, string frame)
    {
        var bin = PlaneFrameDecoder.GetBin(frame);
        var downlink = PlaneFrameDecoder.ExtractValue(bin, 0, 5);

        if(!Strategies.TryGetValue(downlink, out var strat))
        {
            return;
        }

        strat.Interpret(plane, bin);
    }
}
