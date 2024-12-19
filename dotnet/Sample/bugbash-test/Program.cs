using Microsoft.AspNetCore.Mvc;
using Azure.Communication.CallAutomation;
using Azure.Communication;
using Azure.Messaging.EventGrid;
using Azure.Messaging;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

//const string hostingEndpoint = "https://9qddr7pn.usw2.devtunnels.ms:8080"; //This is an example, update it if you wish to use features that require it.
const string hostingEndpoint = "https://1d22-4-154-115-106.ngrok-free.app";
const string acsConnectionString = "endpoint=https://corertc-test-apps.unitedstates.communication.azure.com/;accesskey=4e2Lng7UGYgQDKPHYvnlQSUhIZSJWrGr4lnCpcp2xm4rvF4a2g4fJQQJ99AFACULyCpAArohAAAAAZCSUlAo";
var client = new CallAutomationClient(connectionString: acsConnectionString);
var eventProcessor = client.GetEventProcessor(); //This will be used for the event processor later on
string callConnectionId = "";
string recordingId = "";
string contentLocation = "";
string deleteLocation = "";

app.MapGet("/test", ()=>
    {
        Console.WriteLine("test endpoint");
    }
);

app.MapPost("/callback", (
    [FromBody] CloudEvent[] cloudEvents) =>
{
    eventProcessor.ProcessEvents(cloudEvents);
    return Results.Ok();
});

app.MapGet("/startcall", (
    [FromQuery] string acstarget) =>
    {
        Console.WriteLine($"starting a new call to user:{acstarget}");
        CommunicationUserIdentifier targetUser = new CommunicationUserIdentifier(acstarget);
        var invite = new CallInvite(targetUser);
        var createCallOptions = new CreateCallOptions(invite, new Uri(hostingEndpoint+ "/callback"));
        var call = client.CreateCall(createCallOptions);
        callConnectionId = call.Value.CallConnection.CallConnectionId;
        return Results.Ok();
    }
);

app.MapGet("/senddtmftone", (
    [FromQuery] string acstarget) =>
    {
        Console.WriteLine($"send dtmf tone to acs user :{acstarget}");
        CommunicationUserIdentifier targetUser = new CommunicationUserIdentifier(acstarget);
        var callConnection = client.GetCallConnection(callConnectionId);
        var callMedia = callConnection.GetCallMedia();
        //var sendDtmfTonesOptions= new SendDtmfTonesOptions(new List<DtmfTone> {"zero","zero","zero","zero","zero"},targetUser); 
        callMedia.SendDtmfTones(new List<DtmfTone> {"zero","zero","zero","zero","zero"},targetUser);
        return Results.Ok();
    }
);

app.MapGet("/playmedia", (
    [FromQuery] string acstarget) =>
    {
        Console.WriteLine($"playing media to user:{acstarget}");
        var callConnection = client.GetCallConnection(callConnectionId);
        var callMedia = callConnection.GetCallMedia();
        FileSource fileSource = new FileSource(new Uri("https://callautomation.blob.core.windows.net/newcontainer/out.wav"));
        CommunicationUserIdentifier targetUser = new CommunicationUserIdentifier(acstarget);
        var playOptions = new PlayOptions(new List<PlaySource> { fileSource }, new List<CommunicationIdentifier> { targetUser })
        {
            Loop = true
        };
        callMedia.Play(playOptions);
        return Results.Ok();
    }
);

app.MapGet("/stopmedia", () =>
    {
        Console.WriteLine("stop media operations endpoint");
        var callConnection = client.GetCallConnection(callConnectionId);
        var callMedia = callConnection.GetCallMedia();
        callMedia.CancelAllMediaOperations();
        return Results.Ok();
    }
);

app.MapGet("/startgroupcall", (
    [FromQuery] string acstarget) =>
    {
        Console.WriteLine("start group call endpoint");
        List<string> targets = acstarget.Split(',').ToList();
        Console.WriteLine($"starting a new group call to user:{targets[0]} and user:{targets[1]}");
        CommunicationUserIdentifier targetUser = new CommunicationUserIdentifier(targets[0]);
        CommunicationUserIdentifier targetUser2 = new CommunicationUserIdentifier(targets[1]);
        var createGroupCallOptions = new CreateGroupCallOptions(new List<CommunicationIdentifier> {targetUser, targetUser2}, new Uri(hostingEndpoint+ "/callback"));
        var call = client.CreateGroupCall(createGroupCallOptions);
        callConnectionId = call.Value.CallConnection.CallConnectionId;
        return Results.Ok();
    }
);

app.MapGet("/playmediatoall", () =>
    {
        Console.WriteLine($"playing media to all users");
        var callConnection = client.GetCallConnection(callConnectionId);
        var callMedia = callConnection.GetCallMedia();
        FileSource fileSource = new FileSource(new System.Uri("https://callautomation.blob.core.windows.net/newcontainer/out.wav"));
        var playToAllOptions = new PlayToAllOptions(new List<PlaySource> {fileSource});
        callMedia.PlayToAll(playToAllOptions);
        return Results.Ok();
    }
);



app.MapGet("/startrecording", () =>
    {
        Console.WriteLine("start recording endpoint");
        var callConnection = client.GetCallConnection(callConnectionId);
        var callLocator = new ServerCallLocator(callConnection.GetCallConnectionProperties().Value.ServerCallId);
        var callRecording = client.GetCallRecording();
        var recordingOptions = new StartRecordingOptions(callLocator);
        var recording = callRecording.Start(recordingOptions);
        recordingId=recording.Value.RecordingId;
        return Results.Ok();
    }
);

app.MapGet("/startrecordingbyos", (
    [FromQuery] string blob
    ) =>
    {
        Console.WriteLine("start recording byos endpoint");
        Console.WriteLine(blob);

        var callConnection = client.GetCallConnection(callConnectionId);
        var callLocator = new ServerCallLocator(callConnection.GetCallConnectionProperties().Value.ServerCallId);
        var callRecording = client.GetCallRecording();
        var recordingOptions = new StartRecordingOptions(callLocator)
        {
            RecordingStorage = RecordingStorage.CreateAzureBlobContainerRecordingStorage(new Uri(blob))
        };
        var recording = callRecording.Start(recordingOptions);
        recordingId=recording.Value.RecordingId;
        return Results.Ok(recordingId);
    }
);

app.MapGet("/startrecordingbyosgroup", (
    [FromQuery] string call,
    [FromQuery] string blob
    ) =>
    {
        Console.WriteLine("start recording byos endpoint");
        Console.WriteLine(call);
        Console.WriteLine(blob);

        var callLocator = new GroupCallLocator(call);
        var callRecording = client.GetCallRecording();
        var recordingOptions = new StartRecordingOptions(callLocator)
        {
            RecordingStorage = RecordingStorage.CreateAzureBlobContainerRecordingStorage(new Uri(blob))
        };
        var recording = callRecording.Start(recordingOptions);
        recordingId=recording.Value.RecordingId;
        return Results.Ok(recordingId);
    }
);

app.MapGet("/stoprecording", () =>
    {
        Console.WriteLine("stop recording endpoint");

        var callRecording = client.GetCallRecording();
        callRecording.Stop(recordingId);
        return Results.Ok();
    }
);

app.MapGet("/pauserecording", () =>
    {
        Console.WriteLine("pause recording endpoint");

        var callRecording = client.GetCallRecording();
        callRecording.Pause(recordingId);
        return Results.Ok();
    }
);

app.MapGet("/resumerecording", () =>
    {
        Console.WriteLine("resume recording endpoint");

        var callRecording = client.GetCallRecording();
        callRecording.Resume(recordingId);
        return Results.Ok();
    }
);

app.MapPost("/filestatus", ([FromBody] EventGridEvent[] eventGridEvents) =>
{
    Console.WriteLine("filestatus endpoint");
    foreach (var eventGridEvent in eventGridEvents)
    {
        if (eventGridEvent.TryGetSystemEventData(out object eventData))
        {
            // Handle the webhook subscription validation event.
            if (eventData is Azure.Messaging.EventGrid.SystemEvents.SubscriptionValidationEventData subscriptionValidationEventData)
            {
                var responseData = new Azure.Messaging.EventGrid.SystemEvents.SubscriptionValidationResponse
                {
                    ValidationResponse = subscriptionValidationEventData.ValidationCode
                };
                return Results.Ok(responseData);
            }

            if (eventData is Azure.Messaging.EventGrid.SystemEvents.AcsRecordingFileStatusUpdatedEventData statusUpdated)
            {
                contentLocation = statusUpdated.RecordingStorageInfo.RecordingChunks[0].ContentLocation;
                deleteLocation = statusUpdated.RecordingStorageInfo.RecordingChunks[0].DeleteLocation;
                Console.WriteLine(contentLocation);
                Console.WriteLine(deleteLocation);
            }
        }
    }
    return Results.Ok();
});

app.MapGet("/download", () =>
    {
        Console.WriteLine("download recording endpoint");
        var callRecording = client.GetCallRecording();
        callRecording.DownloadTo(new Uri(contentLocation),"testfile.wav");
        return Results.Ok();
    }
);

app.MapGet("/delete", () =>
    {
        Console.WriteLine("delete recording endpoint");
        var callRecording = client.GetCallRecording();
        callRecording.Delete(new Uri(deleteLocation));
        return Results.Ok();
    }
);

app.MapPost("/incomingcall", async (
    [FromBody] Azure.Messaging.EventGrid.EventGridEvent[] eventGridEvents) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        if (eventGridEvent.TryGetSystemEventData(out object eventData))
        {
            // Handle the webhook subscription validation event.
            if (eventData is Azure.Messaging.EventGrid.SystemEvents.SubscriptionValidationEventData subscriptionValidationEventData)
            {
                var responseData = new Azure.Messaging.EventGrid.SystemEvents.SubscriptionValidationResponse
                {
                    ValidationResponse = subscriptionValidationEventData.ValidationCode
                };
                return Results.Ok(responseData);
            }
            else if (eventData is Azure.Messaging.EventGrid.SystemEvents.AcsIncomingCallEventData acsIncomingCallEventData)
            {
                var incomingCallContext = acsIncomingCallEventData.IncomingCallContext;
                Console.WriteLine(incomingCallContext);
                var callbackUri = new Uri(hostingEndpoint+ "/callback");
                AnswerCallResult answerCallResult = await client.AnswerCallAsync(incomingCallContext, callbackUri);
                callConnectionId = answerCallResult.CallConnectionProperties.CallConnectionId;
                
            }
        }
    }
    return Results.Ok();
});

app.MapPost("/incomingcallreject", async (
    [FromBody] Azure.Messaging.EventGrid.EventGridEvent[] eventGridEvents) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        if (eventGridEvent.TryGetSystemEventData(out object eventData))
        {
            // Handle the webhook subscription validation event.
            if (eventData is Azure.Messaging.EventGrid.SystemEvents.SubscriptionValidationEventData subscriptionValidationEventData)
            {
                var responseData = new Azure.Messaging.EventGrid.SystemEvents.SubscriptionValidationResponse
                {
                    ValidationResponse = subscriptionValidationEventData.ValidationCode
                };
                return Results.Ok(responseData);
            }
            else if (eventData is Azure.Messaging.EventGrid.SystemEvents.AcsIncomingCallEventData acsIncomingCallEventData)
            {
                var incomingCallContext = acsIncomingCallEventData.IncomingCallContext;
                var rejectCallResult = await client.RejectCallAsync(incomingCallContext);                
            }
        }
    }
    return Results.Ok();
});

app.MapPost("/incomingcallredirect", async (
    [FromBody] Azure.Messaging.EventGrid.EventGridEvent[] eventGridEvents) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        if (eventGridEvent.TryGetSystemEventData(out object eventData))
        {
            // Handle the webhook subscription validation event.
            if (eventData is Azure.Messaging.EventGrid.SystemEvents.SubscriptionValidationEventData subscriptionValidationEventData)
            {
                var responseData = new Azure.Messaging.EventGrid.SystemEvents.SubscriptionValidationResponse
                {
                    ValidationResponse = subscriptionValidationEventData.ValidationCode
                };
                return Results.Ok(responseData);
            }
            else if (eventData is Azure.Messaging.EventGrid.SystemEvents.AcsIncomingCallEventData acsIncomingCallEventData)
            {
                var acsRedirectTarget ="<ENTER ACS TEST USER HERE>";
                CommunicationUserIdentifier targetUser = new CommunicationUserIdentifier(acsRedirectTarget);
                var invite = new CallInvite(targetUser);
                var incomingCallContext = acsIncomingCallEventData.IncomingCallContext;
                var callbackUri = new Uri(hostingEndpoint+ "/callback");
                var rejectCallResult = await client.RedirectCallAsync(incomingCallContext, invite);                
            }
        }
    }
    return Results.Ok();
});

app.MapGet("/recognize", async ([FromQuery] string acstarget) =>
    {
        string pstnNumber = "+11231231234";
        Console.WriteLine("recognize endpoint");
        CommunicationUserIdentifier targetUser = new CommunicationUserIdentifier(acstarget);
        var callConnection = client.GetCallConnection(callConnectionId);
        var callMedia = callConnection.GetCallMedia();
        callConnection.GetParticipants();
        CallMediaRecognizeOptions dmtfRecognizeOptions = new CallMediaRecognizeDtmfOptions(targetUser, maxTonesToCollect: 3)
        {
            InterruptCallMediaOperation = true,
            InterToneTimeout = TimeSpan.FromSeconds(10),
            StopTones = new DtmfTone[] { DtmfTone.Pound },
            InitialSilenceTimeout = TimeSpan.FromSeconds(5),
            InterruptPrompt = true,
            Prompt = new FileSource(new Uri("https://callautomation.blob.core.windows.net/newcontainer/out.wav"))
        };

        var tone  = await callMedia.StartRecognizingAsync(dmtfRecognizeOptions);
        var results = await tone.Value.WaitForEventProcessorAsync();

        //here we write out what the user has entered into the phone. 
        if(results.IsSuccess)
        {
            Console.WriteLine(((DtmfResult)results.SuccessResult.RecognizeResult).ConvertToString());
        }

        return Results.Ok();
    }
);

app.Run();