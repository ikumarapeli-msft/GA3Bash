package com.communication.callautomation;

import com.azure.communication.callautomation.CallAutomationClient;
import com.azure.communication.callautomation.CallAutomationClientBuilder;
import com.azure.communication.callautomation.CallAutomationEventParser;
import com.azure.communication.callautomation.CallMedia;
import com.azure.communication.callautomation.models.*;
import com.azure.communication.callautomation.models.events.*;
import com.azure.communication.common.CommunicationUserIdentifier;
import com.azure.communication.common.PhoneNumberIdentifier;
import com.azure.core.http.rest.Response;
import com.azure.core.util.Context;
import com.azure.messaging.eventgrid.EventGridEvent;
import com.azure.messaging.eventgrid.SystemEventNames;
import com.azure.messaging.eventgrid.systemevents.AcsIncomingCallEventData;
import com.azure.messaging.eventgrid.systemevents.AcsRecordingFileStatusUpdatedEventData;
import com.azure.messaging.eventgrid.systemevents.SubscriptionValidationEventData;
import com.azure.messaging.eventgrid.systemevents.SubscriptionValidationResponse;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;
import java.nio.file.Paths;
import java.time.Duration;
import java.util.Arrays;
import java.util.List;


@RestController
@Slf4j
public class ProgramSample {

    String hostingEndpoint = "https://slnqrd9k.usw2.devtunnels.ms:8080"; // This is a sample update with your endpoint
    String acsConnectionString = "<INSERT ACS CONNECTION STRING HERE>";
    
    private CallAutomationClient client;

    String callConnectionId = "";
    String recordingId = "";
    String contentLocation = "";
    String deleteLocation = ""; 

    public ProgramSample() {
        client = initClient();
    }

    @GetMapping(path = "/startcall")
    public ResponseEntity<String> startCallEndpoint(@RequestParam String acsTarget) {
        System.out.println("Start call endpoint hit");
        System.out.println("Starting a new call to user: " + acsTarget);
        CommunicationUserIdentifier targetUser = new CommunicationUserIdentifier(acsTarget);
        CallInvite invite = new CallInvite(targetUser);
        CreateCallOptions createCallOptions = new CreateCallOptions(invite, hostingEndpoint + "/callback");
        Response<CreateCallResult> result = client.createCallWithResponse(createCallOptions, Context.NONE);
        callConnectionId = result.getValue().getCallConnectionProperties().getCallConnectionId();
        return ResponseEntity.ok("Call initiated successfully");
    }

    @GetMapping(path = "/senddtmftone")
    public ResponseEntity<String> sendDTMFToneEndpoint(@RequestParam String acsTarget) {
        System.out.println("Send dtmf tone to acs user: " + acsTarget);
        CommunicationUserIdentifier targetUser = new CommunicationUserIdentifier(acsTarget);
        client.getCallConnection(callConnectionId).getCallMedia().sendDtmfTones(Arrays.asList(DtmfTone.A, DtmfTone.FOUR, DtmfTone.ZERO), targetUser);
        return ResponseEntity.ok("DTMF tone sent successfully");
    }

    @GetMapping(path = "/playmedia")
    public ResponseEntity<String> playMediaEndpoint(@RequestParam String acsTarget) {
        System.out.println("Playing media to acs user: " + acsTarget);
        CommunicationUserIdentifier targetUser = new CommunicationUserIdentifier(acsTarget);
        FileSource fileSource = new FileSource().setUrl("https://callautomation.blob.core.windows.net/newcontainer/out.wav");
        client.getCallConnection(callConnectionId).getCallMedia().play(fileSource, Arrays.asList(targetUser));
        return ResponseEntity.ok("Media Played successfully");
    }

    @GetMapping(path = "/stopmedia")
    public ResponseEntity<String> stopMediaEndpoint() {
        System.out.println("stop media operations endpoint");
        client.getCallConnection(callConnectionId).getCallMedia().cancelAllMediaOperations();
        return ResponseEntity.ok("Media Stopped successfully");
    }

    @GetMapping(path = "/startgroupcall")
    public ResponseEntity<String> startgroupCallEndpoint(@RequestParam String acsTargets) {
        System.out.println("Start group call endpoint");
        List<String> targets = Arrays.asList(acsTargets.split(","));
        System.out.println("Starting a new group call to user: " + targets.get(0) + " and user: " + targets.get(1));
        CommunicationUserIdentifier targetUser = new CommunicationUserIdentifier(targets.get(0));
        CommunicationUserIdentifier targetUser2 = new CommunicationUserIdentifier(targets.get(1));
        CreateGroupCallOptions groupCallOptions = new CreateGroupCallOptions(Arrays.asList(targetUser, targetUser2), hostingEndpoint + "/callback");
        Response<CreateCallResult> result = client.createGroupCallWithResponse(groupCallOptions, Context.NONE);
        callConnectionId = result.getValue().getCallConnectionProperties().getCallConnectionId();
        return ResponseEntity.ok("group call initiated successfully");
    }

    @GetMapping(path = "/playmediatoall")
    public ResponseEntity<String> playMediaToAllEndpoint() {
        System.out.println("play media to all endpoint");
        FileSource fileSource = new FileSource().setUrl("https://callautomation.blob.core.windows.net/newcontainer/out.wav");
        client.getCallConnection(callConnectionId).getCallMedia().playToAll(fileSource);
        return ResponseEntity.ok("Media Played to all successfully");
    }

    @GetMapping(path = "/startrecording")
    public ResponseEntity<String> startRecordingEndpoint() {
        System.out.println("start recording endpoint");
        ServerCallLocator serverCallLocator = new ServerCallLocator(client.getCallConnection(callConnectionId).getCallProperties().getServerCallId());
        StartRecordingOptions recordingOptions = new StartRecordingOptions(serverCallLocator);
        var start = client.getCallRecording().start(recordingOptions);
        return ResponseEntity.ok("Recording strated successfully with ID:"+start.getRecordingId());
    }

    @GetMapping(path = "/startrecordingbyos")
    public ResponseEntity<String> startRecordingBYOSEndpoint(@RequestParam String blob) {
        System.out.println("start recording BYOS endpoint");
        ServerCallLocator serverCallLocator = new ServerCallLocator(client.getCallConnection(callConnectionId).getCallProperties().getServerCallId());
        StartRecordingOptions recordingOptions = new StartRecordingOptions(serverCallLocator);

        //add start recording options here for byos

        var start = client.getCallRecording().start(recordingOptions);
        return ResponseEntity.ok("Recording strated successfully with ID:"+start.getRecordingId());
    }

    @GetMapping(path = "/startrecordingbyosGroup")
    public ResponseEntity<String> startRecordingBYOSGroupEndpoint(@RequestParam String call, @RequestParam String blob) {
        System.out.println("start recording BYOS endpoint");
        GroupCallLocator groupCallLocator = new GroupCallLocator(call);
        StartRecordingOptions recordingOptions = new StartRecordingOptions(groupCallLocator);

        //add start recording options here for byos

        var start = client.getCallRecording().start(recordingOptions);
        return ResponseEntity.ok("Recording started successfully with ID:"+start.getRecordingId());
    }

    @GetMapping(path = "/stoprecording")
    public ResponseEntity<String> stopRecordingEndpoint() {
        System.out.println("stop recording endpoint");
        client.getCallRecording().stop(recordingId);
        return ResponseEntity.ok("Recording started successfully");
    }

    @GetMapping(path = "/pauserecording")
    public ResponseEntity<String> pauseRecordingEndpoint() {
        System.out.println("pause recording endpoint");
        client.getCallRecording().pause(recordingId);
        return ResponseEntity.ok("Recording paused successfully");
    }

    @GetMapping(path = "/resumerecording")
    public ResponseEntity<String> resumeRecordingEndpoint() {
        System.out.println("resume recording endpoint");
        client.getCallRecording().resume(recordingId);
        return ResponseEntity.ok("Recording resumed successfully");
    }

    @PostMapping(path = "/filestatus")
    public ResponseEntity<SubscriptionValidationResponse> fileStatusEndpoint(@RequestBody final String reqBody) {
        List<EventGridEvent> events = EventGridEvent.fromString(reqBody);
        for (EventGridEvent eventGridEvent : events) {
            if (eventGridEvent.getEventType().equals(SystemEventNames.EVENT_GRID_SUBSCRIPTION_VALIDATION)) {
                
                SubscriptionValidationEventData subscriptioneventData = eventGridEvent.getData().toObject(SubscriptionValidationEventData.class);
                SubscriptionValidationResponse response = new SubscriptionValidationResponse().setValidationResponse(subscriptioneventData.getValidationCode());
                
                return ResponseEntity.ok(response);
            }
            else if (eventGridEvent.getEventType().equals(SystemEventNames.COMMUNICATION_RECORDING_FILE_STATUS_UPDATED)) {

                AcsRecordingFileStatusUpdatedEventData statusUpdated = eventGridEvent.getData().toObject(AcsRecordingFileStatusUpdatedEventData.class);
                contentLocation = statusUpdated.getRecordingStorageInfo().getRecordingChunks().get(0).getContentLocation();
                deleteLocation = statusUpdated.getRecordingStorageInfo().getRecordingChunks().get(0).getDeleteLocation();
                System.out.println("content Location :"+contentLocation);
                System.out.println("delete Location :"+deleteLocation);
            }
        }
        return ResponseEntity.ok().body(null);
    }

    @GetMapping(path = "/download")
    public ResponseEntity<String> downloadRecordingEndpoint() {
        System.out.println("download recording endpoint");
        client.getCallRecording().downloadTo(contentLocation, Paths.get("testfile.wav"));
        return ResponseEntity.ok("Recording downloaded successfully");
    }

    @GetMapping(path = "/delete")
    public ResponseEntity<String> deleteRecordingEndpoint() {
        System.out.println("delete recording endpoint");
        client.getCallRecording().delete(deleteLocation);
        return ResponseEntity.ok("Recording deleted successfully");
    }

    @PostMapping(path = "/incomingcall")
    public ResponseEntity<SubscriptionValidationResponse> incomingcallEndpoint(@RequestBody final String reqBody) {
        List<EventGridEvent> events = EventGridEvent.fromString(reqBody);
        for (EventGridEvent eventGridEvent : events) {
            if (eventGridEvent.getEventType().equals(SystemEventNames.EVENT_GRID_SUBSCRIPTION_VALIDATION)) {
                
                SubscriptionValidationEventData subscriptioneventData = eventGridEvent.getData().toObject(SubscriptionValidationEventData.class);
                SubscriptionValidationResponse response = new SubscriptionValidationResponse().setValidationResponse(subscriptioneventData.getValidationCode());
                
                return ResponseEntity.ok(response);
            }
            else if (eventGridEvent.getEventType().equals(SystemEventNames.COMMUNICATION_INCOMING_CALL)) {

                AcsIncomingCallEventData incomingCall = eventGridEvent.getData().toObject(AcsIncomingCallEventData.class);
                AnswerCallResult answerCallResult = client.answerCall(incomingCall.getIncomingCallContext(), hostingEndpoint + "/callback");
                callConnectionId = answerCallResult.getCallConnectionProperties().getCallConnectionId();
            }
        }
        return ResponseEntity.ok().body(null);
    }


    @PostMapping(path = "/incomingcallreject")
    public ResponseEntity<SubscriptionValidationResponse> incomingcallRejectEndpoint(@RequestBody final String reqBody) {
        List<EventGridEvent> events = EventGridEvent.fromString(reqBody);
        for (EventGridEvent eventGridEvent : events) {
            if (eventGridEvent.getEventType().equals(SystemEventNames.EVENT_GRID_SUBSCRIPTION_VALIDATION)) {
                
                SubscriptionValidationEventData subscriptioneventData = eventGridEvent.getData().toObject(SubscriptionValidationEventData.class);
                SubscriptionValidationResponse response = new SubscriptionValidationResponse().setValidationResponse(subscriptioneventData.getValidationCode());
                
                return ResponseEntity.ok(response);
            }
            else if (eventGridEvent.getEventType().equals(SystemEventNames.COMMUNICATION_INCOMING_CALL)) {

                AcsIncomingCallEventData incomingCall = eventGridEvent.getData().toObject(AcsIncomingCallEventData.class);
                client.rejectCall(incomingCall.getIncomingCallContext());
            }
        }
        return ResponseEntity.ok().body(null);
    }

    @PostMapping(path = "/incomingcallredirect")
    public ResponseEntity<SubscriptionValidationResponse> incomingcallRedirectEndpoint(@RequestBody final String reqBody) {
        List<EventGridEvent> events = EventGridEvent.fromString(reqBody);
        for (EventGridEvent eventGridEvent : events) {
            if (eventGridEvent.getEventType().equals(SystemEventNames.EVENT_GRID_SUBSCRIPTION_VALIDATION)) {
                
                SubscriptionValidationEventData subscriptioneventData = eventGridEvent.getData().toObject(SubscriptionValidationEventData.class);
                SubscriptionValidationResponse response = new SubscriptionValidationResponse().setValidationResponse(subscriptioneventData.getValidationCode());
                
                return ResponseEntity.ok(response);
            }
            else if (eventGridEvent.getEventType().equals(SystemEventNames.COMMUNICATION_INCOMING_CALL)) {
                var acsRedirectTarget ="<ENTER ACS TEST USER HERE>";
                CommunicationUserIdentifier targetUser = new CommunicationUserIdentifier(acsRedirectTarget);
                CallInvite invite = new CallInvite(targetUser);

                AcsIncomingCallEventData incomingCall = eventGridEvent.getData().toObject(AcsIncomingCallEventData.class);
                client.redirectCall(incomingCall.getIncomingCallContext(), invite);
            }
        }
        return ResponseEntity.ok().body(null);
    }

    @GetMapping(path = "/recognize")
    public ResponseEntity<String> recognizeAllEndpoint() {
        System.out.println("recognize endpoint");
        String pstnNumber = "+14564567465";

        CallMedia media = client.getCallConnection(callConnectionId).getCallMedia();

        CallMediaRecognizeDtmfOptions dtmfOptions = new CallMediaRecognizeDtmfOptions(new PhoneNumberIdentifier(pstnNumber), 3);
        dtmfOptions.setPlayPrompt(new FileSource().setUrl("https://callautomation.blob.core.windows.net/newcontainer/out.wav"));
        dtmfOptions.setInterruptCallMediaOperation(true).setInterToneTimeout(Duration.ofSeconds(10)).setInitialSilenceTimeout(Duration.ofSeconds(5)).setInterruptPrompt(true);

        var tones = media.startRecognizingWithResponse(dtmfOptions, null);

        return ResponseEntity.ok("DTMF Recognized successfully");
    }

    @PostMapping(path = "/callback")
    public ResponseEntity<String> callbackEvents(@RequestBody final String reqBody) {
        List<CallAutomationEventBase> events = CallAutomationEventParser.parseEvents(reqBody);
        for (CallAutomationEventBase event : events) {
            if (event instanceof RecognizeCompleted) {
                System.out.println("Recognize Completed event received");

                RecognizeCompleted recognizeEvent = (RecognizeCompleted) event;
                DtmfResult dtmfResult = (DtmfResult) recognizeEvent
                        .getRecognizeResult().get();
                List<DtmfTone> tones = dtmfResult.getTones();

                System.out.println(tones.toString());

            }
        }
        return ResponseEntity.ok().body("");
    }


    private CallAutomationClient initClient() {
        CallAutomationClient client;
        try {
            client = new CallAutomationClientBuilder()
                    .connectionString(acsConnectionString)
                    .buildClient();
            return client;

        } catch (Exception e) {
            log.error("Error occurred when initializing Call Automation Async Client: {} {}",
                    e.getMessage(),
                    e.getCause());
            return null;
        }
    }
}