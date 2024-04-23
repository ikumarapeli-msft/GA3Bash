import { CallAutomationClient, CallInvite, CallLocator, CallMediaRecognizeDtmfOptions, DtmfTone, FileSource, PlayOptions, StartRecordingOptions, RecordingStorageKind } from "@azure/communication-call-automation";
import { CommunicationUserIdentifier, PhoneNumberIdentifier } from "@azure/communication-common";
import express from "express";

process.on('uncaughtException', function (err) {
    console.error(err);
    console.log("Node NOT Exiting...");
});

const app = express();
const port = 8080; // default port to listen
app.use(express.json());
const hostingEndpoint = "https://clnsqrdr9k.usw2.devtunnels.ms:8080"; //This is an example, update to your endpoint
const acsConnectionString = "<INSERT ACS CONNECTION STRING HERE>";
const client = new CallAutomationClient(acsConnectionString);
let callConnectionId = "";
let recordingId = "";
let contentLocation = "";
let deleteLocation = "";

app.get( "/test", ( req, res ) => {
    console.log( "test endpoint" );
    res.sendStatus(200);
} );

//This will be used for callbacks, for example, here we are listening for a RecognizeCompleted event. We can add additional events here
app.post( "/callback", ( req, res ) => {
    const event = req.body[0];
    const eventData = event.data;

    if(event.type=="Microsoft.Communication.RecognizeCompleted")
    {
        let toneList:DtmfTone[] = eventData.dtmfResult.tones
        console.log(toneList)
    }
    res.sendStatus(200);
} );

app.get( "/startcall", async ( req, res ) => {
    console.log( "startcall endpoint" );
    const { acstarget } = req.query;
    let targetUser:CommunicationUserIdentifier = {communicationUserId:acstarget?.toString()||""};
    let callInvite:CallInvite = {targetParticipant:targetUser};
    let call = await client.createCall(callInvite, hostingEndpoint+"/callback")
    callConnectionId=call.callConnectionProperties.callConnectionId||""
    res.sendStatus(200);
} );

app.get( "/senddtmftone", ( req, res ) => {
    console.log( "send dtmf tone endpoint" );
    const { acstarget } = req.query;
    let targetUser:CommunicationUserIdentifier = {communicationUserId:acstarget?.toString()||""};
    const callConnection = client.getCallConnection(callConnectionId);
    const callMedia = callConnection.getCallMedia();
    callMedia.sendDtmfTones(["zero","zero","one"], targetUser);
    res.sendStatus(200);
} );

app.get( "/playmedia", ( req, res ) => {
    console.log( "playmedia endpoint" );
    const { acstarget } = req.query;
    let targetUser:CommunicationUserIdentifier = {communicationUserId:acstarget?.toString()||""};
    const callConnection = client.getCallConnection(callConnectionId);
    const callMedia = callConnection.getCallMedia();
    const filesource:FileSource = {url:"https://callautomation.blob.core.windows.net/newcontainer/out.wav", kind:"fileSource"}
    let playOptions:PlayOptions = {loop:true};
    callMedia.play([filesource],[targetUser],playOptions);
    res.sendStatus(200);
} );

app.get( "/stopmedia", ( req, res ) => {
    console.log( "stopmedia endpoint" );
    const callConnection = client.getCallConnection(callConnectionId);
    const callMedia = callConnection.getCallMedia();
    callMedia.cancelAllOperations();
    res.sendStatus(200);
} );

app.get( "/startgroupcall", async ( req, res ) => {
    console.log( "startgroupcall endpoint" );
    const { acstarget } = req.query;
    const targets = acstarget?.toString().split(',');
    
    let targetUser:CommunicationUserIdentifier = {communicationUserId:(targets?.at(0)||"")};
    let targetUser2:CommunicationUserIdentifier = {communicationUserId:(targets?.at(1)||"")};

    let call = await client.createGroupCall([targetUser,targetUser2], hostingEndpoint+"/callback")
    callConnectionId = call.callConnectionProperties.callConnectionId||""
    res.sendStatus(200);
} );

app.get( "/playmediatoall", ( req, res ) => {
    console.log( "playmediatoall endpoint" );
    const callConnection = client.getCallConnection(callConnectionId);
    const callMedia = callConnection.getCallMedia();
    const filesource:FileSource = {url:"https://callautomation.blob.core.windows.net/newcontainer/out.wav", kind:"fileSource"}
    let playOptions:PlayOptions = {loop:true};
    callMedia.playToAll([filesource],playOptions);
    res.sendStatus(200);
} );

app.get( "/startrecording", async ( req, res ) => {
    console.log( "startrecording endpoint" );
    const callRecording = client.getCallRecording();
    const callConnection = client.getCallConnection(callConnectionId);

    const callConnectionProperties = await callConnection.getCallConnectionProperties()
    const serverCallId = callConnectionProperties.serverCallId||""

    const callLocator:CallLocator = {id:serverCallId,kind:"serverCallLocator"}
    let recordingOptions:StartRecordingOptions = {callLocator};
    let recording = callRecording.start(recordingOptions);
    recordingId = (await recording).recordingId;
    res.sendStatus(200).send(recordingId);
} );

app.get( "/startrecordingbyos", async ( req, res ) => {
 
    console.log("start recording byos endpoint");
    console.log(req.query.blob);
 
    const callRecording = client.getCallRecording();
    const callConnection = client.getCallConnection(callConnectionId);

    const callConnectionProperties = await callConnection.getCallConnectionProperties()
    const serverCallId = callConnectionProperties.serverCallId||""

    const callLocator:CallLocator = {id:serverCallId,kind:"serverCallLocator"}
    const recordingStorageKind: RecordingStorageKind = "azureBlobStorage"
 
    let recordingOptions: StartRecordingOptions = {
        callLocator: callLocator,
        recordingStorage: {recordingStorageKind: recordingStorageKind, recordingDestinationContainerUrl: req.query.blob?.toString() || ""},
        pauseOnStart: false,
        recordingChannel: "mixed",
        recordingContent: "audioVideo",
        recordingFormat: "mp4"
    };
 
    let recording = callRecording.start(recordingOptions);
 
    recordingId = (await recording).recordingId;
 
    console.log(recordingId);
    res.status(200).send(recordingId);
   
} );


app.get( "/startrecordingbyosgroup", async ( req, res ) => {
    const { blob, call } = req.query;


    console.log( "startrecording byos group call endpoint" +" blob "+blob+" call "+call );    
    const callRecording = client.getCallRecording();
    const groupCallId = call+"";


    const callLocator:CallLocator = {id:groupCallId,kind:"groupCallLocator"}
    const recordingStorageKind: RecordingStorageKind = "azureBlobStorage"
 
    let recordingOptions: StartRecordingOptions = {
        callLocator: callLocator,
        recordingStorage: {recordingStorageKind: recordingStorageKind, recordingDestinationContainerUrl: req.query.blob?.toString() || ""},
        pauseOnStart: false,
        recordingChannel: "mixed",
        recordingContent: "audioVideo",
        recordingFormat: "mp4"
    };
 

    let recording = callRecording.start(recordingOptions);
    recordingId = (await recording).recordingId;
    res.sendStatus(200).send(recordingId);
} );


app.get( "/stoprecording", async ( req, res ) => {
    console.log( "stop recording endpoint" );
    const callRecording = client.getCallRecording();
    callRecording.stop(recordingId);
    res.sendStatus(200);
} );

app.get( "/pauserecording", async ( req, res ) => {
    console.log( "pause recording endpoint" );
    const callRecording = client.getCallRecording();
    callRecording.pause(recordingId);
    res.sendStatus(200);
} );
app.get( "/resumerecording", async ( req, res ) => {
    console.log( "resume recording endpoint" );
    const callRecording = client.getCallRecording();
    callRecording.resume(recordingId);
    res.sendStatus(200);
} );

app.post( "/filestatus", async ( req, res ) => {
    console.log( "filestatus endpoint" );
    const event = req.body[0];
    const eventData = event.data;
  
    if (event.eventType === "Microsoft.EventGrid.SubscriptionValidationEvent") {
      console.log("Received SubscriptionValidation event");
      res.status(200).send({ "ValidationResponse": eventData.validationCode });
    }
    
    if(eventData && event.eventType == "Microsoft.Communication.RecordingFileStatusUpdated") {
        deleteLocation = eventData.recordingStorageInfo.recordingChunks[0].deleteLocation
        contentLocation = eventData.recordingStorageInfo.recordingChunks[0].contentLocation
        console.log("Delete Location: " + deleteLocation);
        console.log("Content Location: " + contentLocation);
        res.sendStatus(200);
    }
});

app.get( "/download", async ( req, res ) => {
    console.log( "download endpoint" );
    const callRecording = client.getCallRecording();
    callRecording.downloadToPath(contentLocation,"testfile.wav")
    res.sendStatus(200);
} );

app.get( "/delete", async ( req, res ) => {
    console.log( "delete endpoint" );
    const callRecording = client.getCallRecording();
    callRecording.delete(deleteLocation)
    res.sendStatus(200);
} );

//****only for those wih a pstn number
app.post( "/incomingcall", async ( req, res ) => {
    console.log( "incomingcall endpoint" );
    const event = req.body[0];
    const eventData = event.data;
  
    if (event.eventType === "Microsoft.EventGrid.SubscriptionValidationEvent") {
      console.log("Received SubscriptionValidation event");
      res.status(200).send({ "ValidationResponse": eventData.validationCode });
    }
    
    if(eventData && event.eventType == "Microsoft.Communication.IncomingCall") {
        var incomingCallContext = eventData.incomingCallContext;
        var callbackUri = hostingEndpoint + "/callback";
        let call = await client.answerCall(incomingCallContext,callbackUri);
        callConnectionId = call.callConnectionProperties.callConnectionId||""
        res.sendStatus(200);
    }
});

//****only for those wih a pstn number
app.post( "/incomingcallreject", async ( req, res ) => {
    console.log( "incomingcallreject endpoint" );
    const event = req.body[0];
    const eventData = event.data;
  
    if (event.eventType === "Microsoft.EventGrid.SubscriptionValidationEvent") {
      console.log("Received SubscriptionValidation event");
      res.status(200).send({ "ValidationResponse": eventData.validationCode });
    }
    
    if(eventData && event.eventType == "Microsoft.Communication.IncomingCall") {
        var incomingCallContext = eventData.incomingCallContext;
        let call = await client.rejectCall(incomingCallContext);
        res.sendStatus(200);
    }
});


//****only for those wih a pstn number
app.post( "/incomingcallredirect", async ( req, res ) => {
    console.log( "incomingcallredirect endpoint" );
    const event = req.body[0];
    const eventData = event.data;
  
    if (event.eventType === "Microsoft.EventGrid.SubscriptionValidationEvent") {
      console.log("Received SubscriptionValidation event");
      res.status(200).send({ "ValidationResponse": eventData.validationCode });
    }
    
    if(eventData && event.eventType == "Microsoft.Communication.IncomingCall") {
        var incomingCallContext = eventData.incomingCallContext;
        let acstarget  = "<ENETER ACS TEST USER HERE>";
        let targetUser:CommunicationUserIdentifier = {communicationUserId:acstarget?.toString()||""};
        let callInvite:CallInvite = {targetParticipant:targetUser};
        let call = await client.redirectCall(incomingCallContext, callInvite);
        res.sendStatus(200);
    }
});

app.get( "/recognize", async ( req, res ) => {
    //here you can use the web app or a pstn call. update the startRrecognizing method with either numn or target user.
    let num:PhoneNumberIdentifier = {phoneNumber:"+1564564654"} //"+11231231234"
    const { acstarget } = req.query;
    let targetUser:CommunicationUserIdentifier = {communicationUserId:acstarget?.toString()||""};


    console.log( "recognize endpoint" );
    const callConnection = client.getCallConnection(callConnectionId);
    const callMedia = callConnection.getCallMedia();
    const filesource:FileSource = {url:"https://callautomation.blob.core.windows.net/newcontainer/out.wav", kind:"fileSource"}


    let recognizeOptions:CallMediaRecognizeDtmfOptions =  {kind:"callMediaRecognizeDtmfOptions",
    interruptCallMediaOperation: true,
    interToneTimeoutInSeconds:10,
    stopDtmfTones: [DtmfTone.Pound],
    initialSilenceTimeoutInSeconds:5,
    interruptPrompt:true,
    playPrompt:filesource,
    maxTonesToCollect:3
};

    callMedia.startRecognizing(targetUser,recognizeOptions);

    res.sendStatus(200);
} );

// start the Express server
app.listen( port, () => {
    console.log( `server started at http://localhost:${ port }` );
} );