using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc;
using Azure.Communication.CallAutomation;
using Azure.Communication;
using Azure.Messaging.EventGrid;
using Azure.Messaging;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using Azure.Messaging.EventGrid.SystemEvents;
using System.Text.Json.Nodes;
using System.Net;
using System.Reflection.Metadata;
using System;
using Azure;
using static bugbash_test.StartDialogAndAddParticipantRequest;
using bugbash_test;
using Microsoft.Extensions.Logging;

//using System.Text.Json.Nodes;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});
var app = builder.Build();
app.UseCors();
var mriOfParticipantToAdd = "";
bool groupIdJoinFlow = false;

//const string hostingEndpoint = "https://9qddr7pn.usw2.devtunnels.ms:8080"; //This is an example, update it if you wish to use features that require it.
const string hostingEndpoint = "https://4201-4-154-115-109.ngrok-free.app";

// PMA DEV
const string acsConnectionString = "acsconnectionstring";
var client = new CallAutomationClient(connectionString: acsConnectionString);
var eventProcessor = client.GetEventProcessor(); //This will be used for the event processor later on
string callConnectionId = "";
string recordingId = "";
string contentLocation = "";

string deleteLocation = "";

DateTime ConnectRequestInitiatedAt = DateTime.Now;
DateTime ConnectRequestSucceededAt = DateTime.Now;
DateTime AddParticipantRequestInitiatedAt = DateTime.Now;
DateTime AddParticipantSucceededAt = DateTime.Now;
DateTime StartDialogRequestInititedAt = DateTime.Now;
DateTime StartDialogSucceededAT = DateTime.Now;


//app.UseCors(c => c.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.MapGet("/test", ()=>
    {
        Console.WriteLine("test endpoint");
    }
);


app.MapPost("/api/calls/{contextId}", async (
    [FromBody] CloudEvent[] cloudEvents,
    [FromRoute] string contextId,
    //[Required] string callerId,
    //[Required] string calleeId,
    IOptions<CallConfiguration> callConfiguration,
    ILogger<Program> logger) =>
{
foreach (var cloudEvent in cloudEvents)
{
    CallAutomationEventBase @event = CallAutomationEventParser.Parse(cloudEvent);
    logger.LogInformation($"Event received: {JsonConvert.SerializeObject(cloudEvent)}");
    logger.LogInformation($"Event received Type:" + cloudEvent.Type);

    var callConnection = client.GetCallConnection(@event.CallConnectionId);
    var callDialog = callConnection.GetCallDialog();

    if (callConnection == null || callDialog == null)
    {
        return Results.BadRequest($"Call objects failed to get for connection id {@event.CallConnectionId}.");
    }

    if (@event is AddParticipantSucceeded)
    {
            // Measure AddParticipant Latency
            AddParticipantSucceededAt = DateTime.Now;
            Console.WriteLine($"### AddParticipant succeeded at: {AddParticipantSucceededAt}");
            TimeSpan ElapsedTimeToAddParticipant = AddParticipantSucceededAt - AddParticipantRequestInitiatedAt;
            Console.WriteLine($"### Total time elapsed to AddParticipant: {ElapsedTimeToAddParticipant.TotalMilliseconds}");

            logger.LogInformation($"Add participant succeeded event received for call connection id: {@event}");
    }

    if (@event is AddParticipantFailed)
    {
        logger.LogInformation($"Add participant failed event received for call connection id: {@event}");
    }

    if (@event is CallConnected)
    {
            // Measure Connect Latency
            ConnectRequestSucceededAt = DateTime.Now;
            Console.WriteLine($"### Connect succeeded at: {ConnectRequestSucceededAt}");
            TimeSpan ElapsedTimeToConnect = ConnectRequestSucceededAt - ConnectRequestInitiatedAt;
            Console.WriteLine($"### Total time elapsed to Connect: {ElapsedTimeToConnect.TotalMilliseconds}");

            //Initiate start dialog as call connected event is received
            logger.LogInformation($"CallConnected event received for call connection id: {@event.CallConnectionId}");
            logger.LogInformation($"CorrelationId: {@event.CorrelationId}");

            // step1: AddParticipant
            if (!groupIdJoinFlow)
            {

                AddParticipantRequestInitiatedAt = DateTime.Now;
                Console.WriteLine($"### Addparticipant reqeust initiated at: {AddParticipantRequestInitiatedAt}");

                Console.WriteLine($"add given participant :{mriOfParticipantToAdd}");
                CommunicationUserIdentifier targetUser = new CommunicationUserIdentifier(mriOfParticipantToAdd);
                CallInvite invite = new CallInvite(targetUser);
                var callMedia = callConnection.AddParticipant(invite);
            }

            //Step2:  Start Dialog

            StartDialogRequestInititedAt = DateTime.Now;
            Console.WriteLine($"### StartDialog request initiated at: {StartDialogRequestInititedAt}");

            Dictionary<string, object> dialogContext = new Dictionary<string, object>();

            string botAppId;

            //if (calleeId.StartsWith("4:", StringComparison.OrdinalIgnoreCase))
            //{
            //    botAppId = callConfiguration.Value.BotRouting.ContainsKey(calleeId.Substring("4:".Length).Trim()) ? callConfiguration.Value.BotRouting[calleeId.Substring("4:".Length).Trim()] : callConfiguration.Value.DefaultBotId;
            //}
            //else
            //{
            //    botAppId = callConfiguration.Value.BotRouting.ContainsKey(calleeId) ? callConfiguration.Value.BotRouting[calleeId] : callConfiguration.Value.DefaultBotId;
            //}

            // botAppId = "4c105eb3-0317-484d-bbfb-88fa133552bc";
            botAppId = "337a9aee-9c55-49f6-996b-a558f2175ca1";

            var dialogOptions = new StartDialogOptions(Guid.NewGuid().ToString(), new PowerVirtualAgentsDialog(botAppId, dialogContext))
            {
                OperationContext = "DialogStart"
            };

            var response = await callDialog.StartDialogAsync(dialogOptions);

            logger.LogInformation($"Start dialog Response: {response.GetRawResponse().Content.ToString()}");

        }
        if (@event is DialogStarted { OperationContext: "DialogStart" })
        {
            // Measure StartDialog Latency
            DateTime StartDialogSucceededAt = DateTime.Now;
            Console.WriteLine($"### StartDialog succeeded at: {StartDialogSucceededAt}");
            TimeSpan ElapsedTimeToStartDialog = StartDialogSucceededAt - StartDialogRequestInititedAt;
            Console.WriteLine($"### Total time elapsed to StartDialog: {ElapsedTimeToStartDialog.TotalMilliseconds}");

            //Verify the start of dialog here 
            logger.LogInformation($"Start Dialog started");
        }
        if (@event is DialogFailed { OperationContext: "DialogStart" })
        {
            logger.LogInformation($"Start Dialog failed");
        }
        if (@event is DialogTransfer)
        {
            var transferEvent = (DialogTransfer)@event;
            await callDialog.StopDialogAsync(transferEvent.DialogId);
            await callConnection.TransferCallToParticipantAsync(new PhoneNumberIdentifier(transferEvent.TransferDestination));
        }
        if (@event is DialogHangup)
        {
            var hangupEvent = (DialogHangup)@event;

            //Stop the dialog
            await callDialog.StopDialogAsync(hangupEvent.DialogId);

            //Hang up the call for everyone
            await callConnection.HangUpAsync(true);
        }
        if (@event is DialogConsent { OperationContext: "DialogStart" })
        {

        }
        if (@event is DialogCompleted { OperationContext: "DialogStop" })
        {

        }

        if (@event is ParticipantsUpdated)
        {
            // Measure Participant Joined Latency
            DateTime participantJoinedWithGroupCallIdAt = DateTime.Now;
            Console.WriteLine($"### Participant join with group call id succeeded at: {participantJoinedWithGroupCallIdAt}");
            TimeSpan ElapsedTimeToJoinGroupCall = participantJoinedWithGroupCallIdAt - ConnectRequestSucceededAt;
            Console.WriteLine($"### Total time elapsed to Join Group call: {ElapsedTimeToJoinGroupCall.TotalMilliseconds}");
        }
    }
    return Results.Ok();
}).Produces(StatusCodes.Status200OK);

app.MapPost("/callback", (
    [FromBody] CloudEvent[] cloudEvents) =>
{
    eventProcessor.ProcessEvents(cloudEvents);
    return Results.Ok();
});

app.MapPost("/api/startDialogAndAddParticipant", (
    [FromBody] StartDialogAndAddParticipantRequest request) =>
    {
        mriOfParticipantToAdd = request.usermri;
        groupIdJoinFlow = request.isgroupcall;

        var requestReceivedFromFE_StartTime = DateTime.Now;
        Console.WriteLine($"### Request Receivd From Front End at: {requestReceivedFromFE_StartTime}");

        if (!groupIdJoinFlow)
        {
            try
            {

                ConnectRequestInitiatedAt = DateTime.Now;
                Console.WriteLine($"### Connect Request initiated at: {ConnectRequestInitiatedAt}");

                Console.WriteLine($"Scenario1: connect to a group call -> start dialog and add participant");
                // Create a group call with connect -> start Dialog -> addparticipant.
                var groupId = Guid.NewGuid().ToString();
                var callLocator = new GroupCallLocator(groupId);
                var callbackUri = new Uri(hostingEndpoint + $"/api/calls/{Guid.NewGuid()}");
                var connectCallOptions = new ConnectCallOptions(callLocator, callbackUri);
                var connectCallResult = client.ConnectCall(connectCallOptions);
                ConnectCallResult result = (ConnectCallResult)connectCallResult;
                callConnectionId = connectCallResult.Value.CallConnection.CallConnectionId;
                Console.WriteLine($"connect to a call result :{callConnectionId}");
                var resultData = new
                {
                    callConnectionId = callConnectionId
                };
                return Results.Ok(resultData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in creating group call: {ex.Message}");
                return Results.StatusCode(500);
            }
        } else {
            Console.WriteLine($"Scenario2: connect to a call result -> dialog and return group call id");
            // Create a group call with connect -> start Dialog -> return the created group id [add participant is not needed and the participant can join the call using the group id]
            try
            {

                ConnectRequestInitiatedAt = DateTime.Now;
                Console.WriteLine($"### Connect Request received at: {ConnectRequestInitiatedAt}");

                var groupId = Guid.NewGuid().ToString();
                var callLocator = new GroupCallLocator(groupId);
                var callbackUri = new Uri(hostingEndpoint + $"/api/calls/{Guid.NewGuid()}");
                var connectCallOptions = new ConnectCallOptions(callLocator, callbackUri);
                var connectCallResult = client.ConnectCall(connectCallOptions);
                ConnectCallResult result = (ConnectCallResult)connectCallResult;
                callConnectionId = connectCallResult.Value.CallConnection.CallConnectionId;
                Console.WriteLine($"connect to a call result :{callConnectionId}");
                var resultData = new
                {
                    groupId = groupId,
                    callConnectionId = callConnectionId
                };
                return Results.Ok(resultData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in creating group call: {ex.Message}");
                return Results.StatusCode(500);
            }

        }
    }
);


app.MapPost("/startcall", (
    [FromQuery] string acstarget) =>
{
    Console.WriteLine($"starting a new call to user:{acstarget}");
    CommunicationUserIdentifier targetUser = new CommunicationUserIdentifier(acstarget);
    var invite = new CallInvite(targetUser);
    var createCallOptions = new CreateCallOptions(invite, new Uri(hostingEndpoint + "/callback"));
    var call = client.CreateCall(createCallOptions);
    callConnectionId = call.Value.CallConnection.CallConnectionId;
    return Results.Ok();
}
);

app.MapPost("/connectcall", (
    [FromQuery] string locatorType,
    [FromQuery] string locatorId) =>
{
    //Set the call locator based on the locator type
    CallLocator callLocator = null;
    if (locatorType == "GroupCall")
    {
        callLocator  = new GroupCallLocator(locatorId);
    } else if(locatorType == "RoomCall")
    {
         callLocator = new RoomCallLocator(locatorId);
    } else
    {
        callLocator = new ServerCallLocator(locatorId);
    }

    var callbackUri = new Uri(hostingEndpoint + $"/api/calls/{Guid.NewGuid()}");
    var connectCallOptions = new ConnectCallOptions(callLocator, callbackUri);
    var connectCallResult = client.ConnectCall(connectCallOptions);
    ConnectCallResult result = (ConnectCallResult)connectCallResult;
    Console.WriteLine($"connect to a call result :{result}");
    callConnectionId = connectCallResult.Value.CallConnection.CallConnectionId;
    return Results.Ok();
}
);

//app.MapPost("/addparticipant", (
//    [FromBody] string usermri,
//    [FromBody] string isgroupcall
//    ) =>
//{
//    Console.WriteLine($"add given participant :{usermri}");
//    Console.WriteLine($"is group call: {isgroupcall}");
//    CommunicationUserIdentifier targetUser = new CommunicationUserIdentifier(usermri);

//    var callConnection = client.GetCallConnection(callConnectionId);
//    CallInvite invite = new CallInvite(targetUser);
//    var callMedia = callConnection.AddParticipant(invite);
//    return Results.Ok();
//}
//);

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
                var callbackUri = new Uri(hostingEndpoint + "/callback");
                AnswerCallResult answerCallResult = await client.AnswerCallAsync(incomingCallContext, callbackUri);
                callConnectionId = answerCallResult.CallConnectionProperties.CallConnectionId;

            }
        }
    }
    return Results.Ok();
});

//app.MapPost("/api/incomingCall", async (
//    [FromBody] EventGridEvent[] eventGridEvents,
//    IOptions<CallConfiguration> callConfiguration,
//    ILogger<Program> logger) =>
//{
//    foreach (var eventGridEvent in eventGridEvents)
//    {
//        logger.LogInformation($"Incoming Call event received : {JsonConvert.SerializeObject(eventGridEvent)}");
//        // Handle system events
//        if (eventGridEvent.TryGetSystemEventData(out object eventData))
//        {
//            // Handle the subscription validation event.
//            if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
//            {
//                var responseData = new SubscriptionValidationResponse
//                {
//                    ValidationResponse = subscriptionValidationEventData.ValidationCode
//                };
//                return Results.Ok(responseData);
//            }
//        }
//        var jsonObject = JsonNode.Parse(eventGridEvent.Data).AsObject();
//        var callerId = (string)(jsonObject["from"]["rawId"]);
//        var calleeId = (string)(jsonObject["to"]["rawId"]);
//        var incomingCallContext = (string)jsonObject["incomingCallContext"];
//        var callbackUri = new Uri(hostingEndpoint + $"/api/calls/{Guid.NewGuid()}?callerId={callerId}&calleeId={calleeId}");

//        AnswerCallOptions options = new AnswerCallOptions(incomingCallContext, callbackUri)
//        {

//        };
//        AnswerCallResult answerCallResult = await client.AnswerCallAsync(options);
//    }
//    return Results.Ok();
//});

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