# PiwikClientTest
Testing app of my Piwik dedicated server.  
The project Piwik.Tracker is copied from <https://github.com/piwik/piwik-dotnet-tracker>  
The class	PPPiwikClient is my usage interface of the tracking api.  
The project PiwikClientTest is a testing client with a simple UI.

### Usage
Firstly new an instance of class `PPPiwikClient`, then assign the required site ID, application name, application version, and user ID. This class do HTTP works in a background thread, so in order to get the response, I need to register to the `SendRecordCompleted` event.

    PPPiwikClient pc = new PPPiwikClient(1, "my awesome app", "v1.0.0", "Alice");
    pc.SendRecordCompleted += MySendCompletedEvent;

Then I can use this instance repeatly to send usage record or send event to my Piwik dedicated server.

    // send a record
    pc.SendRecord(RecordType.Use);
    // send an event
    pc.SendEvent("my event category", "my action");

Eventually, a response will trigger the `SendRecordCompleted` event. I can get the result from the `SendRecordCompleteEventArgs` argument. Inside this argument, `HttpStatusCode` will always have a 3-digit integer value. If a exception is thrown during background worker, `ExceptionMessage` will have a value string; otherwise, `SendResult` will keep the HTTP sending result string.

### Behind the Scene
The `Piwik.Tracker` class will create a Http request with a url like:

    string url = "http://<my piwik server address>/piwik.php?idsite=<1>&rec=1&apiv=1&r=414460&uid=<Alice>&_idts=1508812138&_idvc=0&gt_ms=1000&_id=522b276a356bdf39&url=<http://my awesome app/v1.1.0/my sub function>";
    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
    request.Method = "GET;
    request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64)";
    HttpWebResponse result = (HttpWebResponse)request.GetResponse();
    
Basically, the piwik server url, tracked site id, and the recorded url are the required fields.
