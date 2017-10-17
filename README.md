# PiwikClientTest
Testing app of my Piwik dedicated server.  
The project Piwik.Tracker is copied from <https://github.com/piwik/piwik-dotnet-tracker>  
The class	PPPiwikClient is my usage interface of the tracking api.  
The project PiwikClientTest is a testing client with a simple UI.

### Usage
Firstly new an instance of class `PPPiwikClient`, then assign the required site ID, application name, application version, and user ID. This class do HTTP works in a background thread, so in order to get the response, I need to register to the `SendRecordCompleted` event.

    PPPiwikClient pc = new PPPiwikClient(1, "my awsome app", "v1.0.0", "Alice");
    pc.SendRecordCompleted += MySendCompletedEvent;

Then I can use this instance repeatly to send usage record or send event to my Piwik dedicated server.

    // send a record
    pc.SendRecord(RecordType.Use);
    // send an event
    pc.SendEvent("my event category", "my action");
