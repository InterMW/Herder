using System.Diagnostics;
using Infrastructure.Redis;

namespace DomainService;

public interface ICoordinatorService
{
    Task Coordinate(long time);
}

public class CoordinatorService(
        IPlaneRepository planeRepository,
        IWorkCommandPublisher publisher) : ICoordinatorService
{
    //When we get a message, mark it down
    //when we get the kick, scan the hash, make a list, inform the other processes to start
        //When they start, they will pop off the list
    public async Task Coordinate(long time)
    {
        // Find out which planes have been seen
        await foreach(var icao in planeRepository.GetIcaosForMoment(time))
        {
            //make a queue for handling them
            await planeRepository.PrepareIcao(time, icao);
        }
        //send out the message that these planes should be seen to
        //
        
        publisher.
        //when a plane is done processing, it is deleted from the hash
        //when the last plane is deleted, the hash wont exist
        Stopwatch timer = new();

        timer.Start();

        while(timer.ElapsedMilliseconds < 500 && await planeRepository.IcaoMomentSetExists(time))
        {
            await Task.Delay(10);
        }
    }
}
