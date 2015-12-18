![alt tag](https://raw.github.com/odinhaus/Suffuz/master/Media/Suffuze_Logo.png)
#### Pronounced: */səˈfyo͞oz/*
#### Meaning: *gradually spread through or over*

#####*A simple and fast API for distributed computing with .Net.*

##NuGet
####PM> Install-Package Altus.Suffuz
[Altus.Suffuz on NuGet.org](https://www.nuget.org/packages/Altus.Suffuz/)

##Description
Suffūz is an API allowing .Net applications to easily request services from and distribute units of work to a group of disparate processes or machines.

Out of the box, it utilizes Multicast UDP messaging groups, allowing clients to establish arbitrary "channels" for communication endpoints, over which POCO .Net types can be exchanged.

In addition to the basic request/response scenarios supported, the system also allows for general broadcast messaging and direct messaging as well as load distribution through the application of arbitrary cost functions and availability delays that can be configured on a route-specific basis, allowing worker agents with more capacity to handle units of work to respond more quickly to requests for service, and then be allocated the work requested.  

The API uses simple fluent-styled syntax to create both calling and routing patterns, and it is built to easily adapt to your choice of Dependency Injection platform, supporting rich extensibility and testability.




##Sample Use Cases
###SCADA
![alt tag](https://raw.github.com/odinhaus/Suffuz/master/Media/SCADA.png)

In a typical SCADA application, you have a variety of computing nodes either observing or directly participating in an industrial control process.  Many times, nodes can join and leave throughout the lifecycle of the process.  A messaging infrastructure that allows for seamless group membership, with both broadcast messaging and directed messaging is paramount for development of such systems.  

Suffūz is a great fit for these types of processes by leveraging fast, compiled binary serialization and UDP messaging, Suffūz can achieve sub-millisecond message latencies, which is critical for high-speed industrial processes.  It is also sufficient for most high-fidelity industrial data logging applications as well.


###Shared Memory
![alt tag](https://raw.github.com/odinhaus/Suffuz/master/Media/SharedMemory.png)

Often times when scaling web applications across multiple web and service nodes, there is a need for relatively fast access to shared state across those devices.  Suffūz would provide the correct pieces for those situations as well, where a common stateful node would maintain a dictionary of shared items in memory, which could be accessed and updated by N dependant nodes, that might come and go randomly over the lifecycle of the shared state.

In these scenarios, fast access, and zero-touch group membership are of the utmost importance.  Suffūz provides the platform to make this simple.


###Worker Pool (With or Without Nomination Constraints)
![alt tag](https://raw.github.com/odinhaus/Suffuz/master/Media/WorkerPool.png)

In some designs, you need a designated node to act as a dispatcher of queued work requests where you also have N worker nodes, all capable of processing the request.

In some situations you might want the work to be dispatched to a single node, and other cases you might want the work dispatched to arbitrary number of nodes, based on some logical condition as determined by the dispatcher.

In the former case, worker selection can be achieved by a simple matter of asking for workers capable of processing a given request to identify themselves, and then dispatching the request to the first respondant, and ignoring the others.  All things being equal, the fastest worker to respond, should also be the fastest worker to process the request.

But things aren't always equal, so sometimes you need a way to balance the field. Suffūz accomplishes this by allowing the dispatcher to determine a nomination selection threshold which is evaluated on each worker, for each request route.  Each worker can be configured with a simple delegate to compute a score, between 0.0 and 1.0 representing the worker's current ability to handle the request, with 1.0 being the most capable, and 0.0 meaning least.  That score is compared to the threshold function provided by the dispatcher, and if it is sufficient, the worker indicates its ability to process the request with a nomination response.  

Additionally, the route can also be configured with a nomination response delay based on the nomination score which server to directly support worker candidate selection by the dispatcher.  By making low scores respond more slowly than high scores, it increases the chance that high scorers nomination responses will be received by the dispatcher first, allowing them to be chosen for the work, rather than more burdened workers.

Obviously, imposing response delays won't be appropriate for all systems, as it could have the side-effect of reducing overall system throughput, so additional mechanisms might be required to determine how to compute the delay factor, based on the system-wide average capacity, for example, which could be broadcast regularly over a designated channel, and incorporated into the delay computation function.  That's all up to you, and your use cases.  Either way, Suffūz supports it in any form that you need.

In the latter case, where the same work request is distributed across N concurrent nodes, delaying responses probably makes less sense, so you'd likely only want to optionally configure the nomination threshold for those routes, and allow all workers that pass the nomination test to complete and return the results of their work as soon as possible.

###Whatever Else You Can Dream Up....
Any other simple distributed computing scenarios you might imagine, Suffūz can probably give you a good place to start.


##Sample Usage
####Remote Execution
```C#
var testRequest = new TestRequest();
var commandRequest = new CommandRequest();

//// executes the default call on the CHANNEL, with no arguments or response
Put.Via(Channels.CHANNEL).Execute();

//// executes with no result on the CHANNEL
Put.Via(Channels.CHANNEL, commandRequest).Execute();

// executes a TestRequest call on the CHANNEL, and returns the first result returned from any respondant
// blocks for the default timeout (Get.DefaultTimeout)
var result1 = Post<TestRequest, TestResponse>.Via(Channels.CHANNEL, testRequest).Execute();


// executes the request on respondants whose capacity exceeds an arbitrary threshold
// the first respondant passing the nomination predicate test and returning a signaling message to the caller
// is then sent the actual request to be processed, ensuring the actual request is only processed on a single remote agent
// nomination scoring is configured as part of the route definition on the recipient
// if no nomination scoring is defined on the recipient, a maximum value of 1.0 is returned for the score
// the Delegate expression is evaluated within the respondants' process, such that failing 
// test prevent the request from beng dispatched to their corresponding handlers, 
// thus preventing both evaluation and responses
TestResponse result2 = Post<TestRequest, TestResponse>
                                .Via(Channels.CHANNEL, testRequest)
                                .Nominate(response => response.Score > TestResponse.SomeNumber())
                                .Execute();

// executes a TestRequest call on the CHANNEL, and returns the first result returned from any respondant
// blocks for the default timeout (Get.DefaultTimeout)
// because this combination of request/response type is not mapped, no response will return within the timeout period specified (500ms)
// a timeout exception is throw in this case, or if none of the respondant results are received in time
try
{
    var result3 = Post<CommandRequest, TestResponse>.Via(Channels.CHANNEL, commandRequest).Execute(500);
}
catch (TimeoutException)
{
    Logger.LogInfo("Handled Timeout");
}

// executes a directed TestRequest call on the CHANNEL, for a specific recipient (App.InstanceName), 
// and returns the first result returned from any respondant
// blocks for up to 500ms
var result4 = Post<TestRequest, TestResponse>.Via(Channels.CHANNEL, testRequest).Execute(500, App.InstanceName);

// executes a TestRequest call on the CHANNEL, and returns all responses received within one second
var enResult1 = Post<TestRequest, TestResponse>.Via(Channels.CHANNEL, testRequest)
                                .All()
                                .Execute(1000)
                                .ToArray();
Debug.Assert(enResult1.Length > 0);

// executes a TestRequest call on the CHANNEL, and returns the first two responses received within the Get.DefaultTimeout time period
// if the terminator condition is not met within the timeout period, the result will contain the responses received up to that time
var enResult2 = Post<TestRequest, TestResponse>
                                .Via(Channels.CHANNEL, testRequest)
                                .Take(2)
                                .Execute()
                                .ToArray();

// executes the request on respondants whose capacity exceeds and arbitrary threshold
// returns enumerable results from all respondants where responses meet an arbitrary predicate (Size > 2) which is evaluated locally
// and blocks for 2000 milliseconds while waiting for responses
// if no responses are received within the timeout, and empty set is returned
// any responses received after the timeout are ignored
var enResult3 = Post<TestRequest, TestResponse>
                                .Via(Channels.CHANNEL, testRequest)
                                .Nominate(cr => cr.Score > 0.9)
                                .Take(r => r.Size > 2)
                                .Execute(2000)
                                .ToArray();

// executes the request on respondants whose capacity exceeds and arbitrary threshold
// returns enumerable results from all respondants where responses meet an arbitrary predicate (Size > 2) which is evaluated locally
// and blocks until the terminal condition is met (ChannelContext.Current.Count > 1)
var enResult4 = Post<TestRequest, TestResponse>
                                .Via(Channels.CHANNEL, testRequest)
                                .Nominate(cr => cr.Score > 0.9)
                                .Take(r => r.Size > 2)
                                .Until(r => ChannelContext.Current.Count > 1)
                                .Execute()
                                .ToArray();

// in this case, because we're executing an enumeration for an unmapped request/response pair, the call will simply block for the 
// timeout period, and return no results.  Enumerations DO NOT throw timeout exceptions in the absence of any responses, only scalar
// execution calls can produce timeout exceptions.
var enResult5 = Post<CommandRequest, TestResponse>
                                .Via(Channels.CHANNEL, commandRequest)
                                .All()
                                .Execute(500)
                                .ToArray();

// an alternative calling pattern, allowing the response type to be deferred until after the channel designation.
// for factory calling patterns, where the construction of the call in terms of what types to return may differ 
// from one channel to the next for the same request type, but your pipeline determines the channel to call 
// before it determines the response.
var ebResult6 = Post.Via(Channels.CHANNEL, testRequest)
                    .Return<TestResponse>()
                    .Nominate(cr => cr.Score > 0.5)
                    .Take(r => r.Size > 2)
                    .Until(r => ChannelContext.Current.Count > 1)
                    .Execute()
                    .ToArray(); 
```

####Message Routing
```C#
var router = App.Resolve<IServiceRouter>();
// set a default handler for CHANNEL for requests with no arguments and no responses
router.Route<Handler>(Channels.CHANNEL, (handler) => handler.HandleNoArgs());

// route incoming requests on CHANNEL of type CommandRequest to handler with no result
router.Route<Handler, CommandRequest>(Channels.CHANNEL, (handler, request) => handler.Handle(request));

// route incoming requests on CHANNEL of type TestRequest to an instance of type Handler, returning a TestResponse result
// additionally, set a capacity limit on this request
// and delay responses for up to 5 seconds for this request proportional to its current capacity score
router.Route<Handler, TestRequest, TestResponse>(Channels.CHANNEL, (handler, request) => handler.Handle(request))
      .Nominate(() => new NominateResponse()
      {
          Score = CostFunctions.CapacityCost(25d, 0d, 100d)
      })
      .Delay((capacity) => TimeSpan.FromMilliseconds(5000d * (1d - capacity.Score)));

// route incoming requests on CHANNEL with no arguments to an instance of Handler, returning a TestResponse result
// additionally, set a nominatoion score on this request to a double
// and delay responses for up to 5 seconds for this request proportional to its current capacity score
router.Route<Handler, TestResponse>(Channels.CHANNEL, (handler) => handler.Handle())
      .Nominate(() => CostFunctions.CapacityCost(25d, 0d, 100d))
      .Delay((capacity) => TimeSpan.FromMilliseconds(5000d * (1d - capacity.Score)));

// route BestEffort messages
router.Route<Handler, TestRequest, TestResponse>(Channels.BESTEFFORT_CHANNEL, (handler, request) => handler.HandleBE(request));
```

####Setting Up DI and Creating Channels
```C#
// sets the DI container adapter to TypeRegistry
App<TypeRegistry>.Initialize();
// get the channel service from the DU container
var channelService = App.Resolve<IChannelService>();
// creates the local channel instance for the CHANNEL service and starts listening
channelService.Create(CHANNEL);
```

####Bootstrapping and Dependency Injection
```C#
/// <summary>
/// Sample Bootstrapper reading from configuration, and providing a dependency resolver, with basic DI Mappings for StructureMap
/// </summary>
public class TypeRegistry : IBootstrapper
{
    /// <summary>
    /// Any byte[] that can be used in the creation of message hashes when communicating with other nodes
    /// </summary>
    public byte[] InstanceCryptoKey
    {
        get
        {
            return Convert.FromBase64String(ConfigurationManager.AppSettings["instanceCryptoKey"]);
        }
    }
    /// <summary>
    /// Globally unique Id for this node
    /// </summary>
    public ulong InstanceId
    {
        get
        {
            return ulong.Parse(ConfigurationManager.AppSettings["instanceId"]);
        }
    }
    /// <summary>
    /// Globally unique Name for this node
    /// </summary>
    public string InstanceName
    {
        get
        {
            return ConfigurationManager.AppSettings["instanceName"];
        }
    }
    /// <summary>
    /// Returns the DI type resolver adapter
    /// </summary>
    /// <returns></returns>
    public IResolveTypes Initialize()
    {
        var channelService = App.ResolveAll<IChannelService>().Single(cs => cs.AvailableServiceLevels == ServiceLevels.Default);
            // create our channel mappings
            channelService.Register(Channels.CHANNEL, Channels.CHANNEL_EP);

            var beChannelService = App.ResolveAll<IChannelService>().Single(cs => cs.AvailableServiceLevels == ServiceLevels.BestEffort);
            // create our best-effort mcast channel mappings
            beChannelService.Register(
                Channels.BESTEFFORT_CHANNEL,
                Channels.BESTEFFORT_CHANNEL_EP,
                Channels.BESTEFFORT_CHANNEL_TTL);

            return new TypeResolver(
                new Container(c =>
            {
                // define additional mappings here
            }));
    }
}

/// <summary>
/// Simple StructureMap DI adapter for Suffūz 
/// </summary>
public class TypeResolver : IResolveTypes
{
    private IContainer _container;

    public TypeResolver(IContainer container)
    {
        _container = container;
    }
    public T Resolve<T>()
    {
        return _container.GetInstance<T>();
    }

    public IEnumerable<T> ResolveAll<T>()
    {
        return _container.GetAllInstances<T>();
    }
}

/// <summary>
/// Simple Channel mapping constants
/// </summary>
public class Channels
{
    /// <summary>
    /// channel1
    /// </summary>
    public static readonly string CHANNEL = "channel1";
    /// <summary>
    /// 224.0.0.0
    /// </summary>
    public static readonly IPEndPoint CHANNEL_EP = new IPEndPoint(IPAddress.Parse("224.0.0.0"), 5000);
    /// <summary>
    /// channel2
    /// </summary>
    public static readonly string BESTEFFORT_CHANNEL = "channel2";
    /// <summary>
    /// 224.0.0.1
    /// </summary>
    public static readonly IPEndPoint BESTEFFORT_CHANNEL_EP = new IPEndPoint(IPAddress.Parse("224.0.0.1"), 5000);
    /// <summary>
    /// 30 seconds
    /// </summary>
    public static readonly TimeSpan BESTEFFORT_CHANNEL_TTL = TimeSpan.FromSeconds(30);
}
```
##Performance
####Serialization Benchmarks
######Suffūz Binary Protocol Serialization
```
Suffūz Bandwidth [Kb]: 72265.625
Serialization:
Suffuz Throughput [Mb/s]: 96.4828104138852
Suffūz Rate [Hz]: 1335113.48464619
Deserialization:
Suffūz Throughput [Mb/s]: 85.3195100354191
Suffūz Rate [Hz]: 1180637.54427391
```
######NewtonSoft.Json v7
```
Json Bandwidth [Kb]: 121093.75
Serialization:
Json Throughput [Mb/s]: 15.6290332989158
Json Rate [Hz]: 129065.565307176
Deserialization:
Json Throughput [Mb/s]: 12.1506873369456
Json Rate [Hz]: 100341.15994380
```
