namespace ActivityMonitoring.Assertions
{
    public class Class1
    {
        //new Monitor
        //Monitor.AddConstraintOptions(new{
        //  
        //  MainQueueSize = MaxEventsHeld (int)
        //  NQSContextSize = MAxEventHeld  (int)
        //  AcceptEventsWithoutActiveNQSContext = boolean,
        //  RaiseExceptionWhenEventsAddedWithoutActiveNQSContext = boolean,
        //  IsManualStartApplicable = //By default initializes MOnitor with Start() called
        //  CaptureSystemDate = //Monitor.Add will internally capture a systemDateTimeOffset for user to access via CaptureTime property,
        //  MaxNQSInstances = default 1// if more than 1, then Simultaneous captures are involved //Dictates whether there can be only one active NQS.
        //                      If no, then any events added will be duplicated across
        //  
        //})
        //Monitor.Start()
        //Monitor.Stop() //Raise exception if anyone tries to add new event post stop. It needs to be restarted
        //Monitor.Clear() clears all data in the main queue<T>
        //Monitor.Free(string guid) checks if an NQS with that Guid is registered, and disposes it
        //Monitor.Format() clears everything
        //Monitor.Add(T) //Raise exception if not started
        //Monitor.NewCaptureContext().WithCondition(canCapture = null,out Guid guid).Build(out var ) creates new Context with random guid and sends it back
        //Monitor.EndCaptureContext()(out NQS)

        //When new context gets registered Monitor.Clear().NewContext() Kinda like create NQS, internally, all additions will go towards the new queue structure for assertions
        //the NQS may be used later for assertions such that
        //If NQS.Discard().UntilContains(T) so that 
        //if it finds, then we discard all entries in NQS till the find
        //if it doesn't find, we throw assertion exception
        //IF NQS.Search().UntilContains(T)
        //If NQS.<>().UntilSatisfies(Predicate<T>)
        //If NQS.ForAll().Invoke(object state, Action<T, object> action)
        //
        //When a context gets completed, we return an NQS and all new concurrent additions get routed back to the main queue
    }
}