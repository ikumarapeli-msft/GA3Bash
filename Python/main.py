
from pyexpat import model
from urllib.parse import urlencode, urljoin
from azure.eventgrid import EventGridEvent, SystemEventNames
from flask import Flask, Response, request, json,render_template,redirect
from logging import INFO
import re
from azure.communication.callautomation import (
    CallAutomationClient,
    PhoneNumberIdentifier,
    RecognizeInputType,
    TextSource,
    CommunicationUserIdentifier,
    ServerCallLocator,
    RecordingChannel,
    RecordingContent,
    RecordingFormat,
    RecognitionChoice,
    DtmfTone,
    FileSource,
    AzureBlobContainerRecordingStorage,
    GroupCallLocator
    )
from azure.core.messaging import CloudEvent

CALL_CONNECTION_ID = ""
RECORDING_ID = ""
CONTENT_LOCATION = ""
DELETE_LOCATION = ""

# Your ACS resource connection string
ACS_CONNECTION_STRING = "<INSERT ACS CONNECTION STRING HERE>"

CALLBACK_URI_HOST = "https://cdqd9k.usw2.devtunnels.ms:8080" # This is a sample, please update to your own

CALLBACK_EVENTS_URI = CALLBACK_URI_HOST + "/callback"

call_automation_client = CallAutomationClient.from_connection_string(ACS_CONNECTION_STRING)

app = Flask(__name__)

@app.route('/callback',  methods=['POST'])
def callback_handler():
    try:        
        app.logger.info("error in evegfdfdgnt handling")

        # app.logger.info("Request Json: %s", request.json)
        for event_dict in request.json:       
            event = CloudEvent.from_dict(event_dict)
            if event.type == "Microsoft.Communication.RecognizeCompleted":
                 if event.data['recognitionType'] == "dtmf": 
                    tones = event.data['dtmfResult']['tones'] 
                    app.logger.info("Recognition completed, tones=%s, context=%s", tones, event.data.get('operationContext'))
                 
    except Exception as ex:
        app.logger.info("error in event handling")
    return "OK", 200  # Return a 200 OK response

@app.route('/startcall')
def create_call_handler():
    acstarget = request.args.get('acstarget', default=None, type=str)
    target_user = CommunicationUserIdentifier(acstarget)
    # source_caller = PhoneNumberIdentifier(ACS_PHONE_NUMBER)
    call_connection_properties = call_automation_client.create_call(target_user, 
                                                                    CALLBACK_EVENTS_URI
                                                                    )
    app.logger.info("Created call with connection id: %s", call_connection_properties.call_connection_id)
    global CALL_CONNECTION_ID
    CALL_CONNECTION_ID=call_connection_properties.call_connection_id

    return "OK", 200  # Return a 200 OK response

@app.route('/senddtmftone')
def send_dtmf_tone():
    acstarget = request.args.get('acstarget', default=None, type=str)
    target_user = CommunicationUserIdentifier(acstarget)
    global CALL_CONNECTION_ID
    call_automation_client.get_call_connection(CALL_CONNECTION_ID).send_dtmf_tones(["zero","one","zero","two","zero","one","zero","two"], target_participant=target_user)
    return "OK", 200  # Return a 200 OK response

@app.route('/playmedia')
def play_media_handler():
    acstarget = request.args.get('acstarget', default=None, type=str)
    target_user = CommunicationUserIdentifier(acstarget)
    global CALL_CONNECTION_ID
    file_source = FileSource(url="https://callautomation.blob.core.windows.net/newcontainer/out.wav") 
    call_automation_client.get_call_connection(CALL_CONNECTION_ID).play_media(play_source=file_source, play_to=[target_user])
    return "OK", 200  # Return a 200 OK response

@app.route('/stopmedia')
def stop_media_handler():
    global CALL_CONNECTION_ID
    call_automation_client.get_call_connection(CALL_CONNECTION_ID).cancel_all_media_operations()
    return "OK", 200  # Return a 200 OK response

@app.route('/startgroupcall')
def create_group_call_handler():
    acstarget = request.args.get('acstarget', default=None, type=str)
    targets = acstarget.split(',')
    # Create CommunicationUserIdentifier objects
    target_user1 = CommunicationUserIdentifier(targets[0])
    target_user2 = CommunicationUserIdentifier(targets[1])

    # source_caller = PhoneNumberIdentifier(ACS_PHONE_NUMBER)
    call_connection_properties = call_automation_client.create_group_call([target_user1,target_user2], 
                                                                    CALLBACK_EVENTS_URI
                                                                    )
    app.logger.info("Created call with connection id: %s", call_connection_properties.call_connection_id)
    global CALL_CONNECTION_ID
    CALL_CONNECTION_ID=call_connection_properties.call_connection_id

    return "OK", 200  # Return a 200 OK response

@app.route('/playmediatoall')
def play_media_to_all_handler():
    global CALL_CONNECTION_ID
    file_source = FileSource(url="https://callautomation.blob.core.windows.net/newcontainer/out.wav") 
    call_automation_client.get_call_connection(CALL_CONNECTION_ID).play_media_to_all(play_source=file_source)
    return "OK", 200  # Return a 200 OK response


@app.route('/startrecording')
def start_recording_handler():
    global CALL_CONNECTION_ID
    recording = call_automation_client.start_recording(server_call_id=call_automation_client.get_call_connection(CALL_CONNECTION_ID).get_call_properties().server_call_id)
    global RECORDING_ID
    RECORDING_ID = recording.recording_id
    return recording.recording_id, 200  # Return a 200 OK response

@app.route('/startrecordingbyos')
def start_recording_byos_handler():
    blob = request.args.get('blob', default=None, type=str)
    global CALL_CONNECTION_ID
    recording = call_automation_client.start_recording(server_call_id=call_automation_client.get_call_connection(CALL_CONNECTION_ID).get_call_properties().server_call_id,
                                                       recording_storage=AzureBlobContainerRecordingStorage(blob))
    global RECORDING_ID
    RECORDING_ID = recording.recording_id
    return recording.recording_id, 200  # Return a 200 OK response


@app.route('/startrecordingbyosgroup')
def start_recording_byos_group_handler():
    blob = request.args.get('blob', default=None, type=str)
    call = request.args.get('call', default=None, type=str)

    global CALL_CONNECTION_ID
    recording = call_automation_client.start_recording(call_locator=GroupCallLocator(call),
                                                       recording_storage=AzureBlobContainerRecordingStorage(blob))
    global RECORDING_ID
    RECORDING_ID = recording.recording_id
    return recording.recording_id, 200  # Return a 200 OK response

@app.route('/stoprecording')
def stop_recording_handler():
    global RECORDING_ID
    call_automation_client.stop_recording(RECORDING_ID)
    return "OK", 200  # Return a 200 OK response

@app.route('/pauserecording')
def pause_recording_handler():
    global RECORDING_ID
    call_automation_client.pause_recording(RECORDING_ID)
    return "OK", 200  # Return a 200 OK response


@app.route('/resumerecording')
def resume_recording_handler():
    global RECORDING_ID
    call_automation_client.resume_recording(RECORDING_ID)
    return "OK", 200  # Return a 200 OK response

@app.route('/filestatus', methods=['POST'])
def recording_file_status_handler():
    try:
        for event_dict in request.json:
            event = EventGridEvent.from_dict(event_dict)
            app.logger.info(event.event_type)

            if event.event_type ==  SystemEventNames.EventGridSubscriptionValidationEventName:
                code = event.data['validationCode']
                if code:
                    data = {"validationResponse": code}
                    app.logger.info("Successfully Subscribed EventGrid.ValidationEvent --> " + str(data))
                    return Response(response=str(data), status=200)

            if event.event_type == SystemEventNames.AcsRecordingFileStatusUpdatedEventName:
                acs_recording_chunk_info_properties = event.data['recordingStorageInfo']['recordingChunks'][0]
                global CONTENT_LOCATION, DELETE_LOCATION
                CONTENT_LOCATION = acs_recording_chunk_info_properties['contentLocation']
                DELETE_LOCATION = acs_recording_chunk_info_properties['deleteLocation']
                app.logger.info("CONTENT LOCATION --> %s", CONTENT_LOCATION)
                app.logger.info("DELETE LOCATION --> %s", DELETE_LOCATION)
                return Response(response="Ok")  
                                                  
    except Exception as ex:
         app.logger.error( "Failed to get recording file")
         return Response(response='Failed to get recording file', status=400)
    
    return Response(response="Ok")  


@app.route('/download')
def download_recording_handler():
        try:
            global CONTENT_LOCATION
            recording_data = call_automation_client.download_recording(CONTENT_LOCATION)
            with open("Recording_File.wav", "wb") as binary_file:
                binary_file.write(recording_data.read())
            return redirect("OK", 200)
        except Exception as ex:
            app.logger.info("Failed to download recording --> " + str(ex))
            return Response(text=str(ex), status=500)
        

@app.route('/delete')
def delete_recording_handler():
        try:
            global DELETE_LOCATION
            call_automation_client.delete_recording(DELETE_LOCATION)
            return redirect("OK", 200)
        except Exception as ex:
            app.logger.info("Failed to download recording --> " + str(ex))
            return Response(text=str(ex), status=500)
        

@app.route("/incomingcall",  methods=['POST'])
def incoming_call_handler_handler():
    for event_dict in request.json:
            event = EventGridEvent.from_dict(event_dict)
            if event.event_type ==  SystemEventNames.EventGridSubscriptionValidationEventName:
                code = event.data['validationCode']
                if code:
                    data = {"validationResponse": code}
                    app.logger.info("Successfully Subscribed EventGrid.ValidationEvent --> " + str(data))
                    return Response(response=str(data), status=200)
                
            elif event.event_type ==SystemEventNames.AcsIncomingCallEventName:
                incoming_call_context=event.data['incomingCallContext']
                answer_call_result = call_automation_client.answer_call(incoming_call_context=incoming_call_context,
                                                                        callback_url=CALLBACK_EVENTS_URI)
                app.logger.info("Answered call for connection id: %s",
                                answer_call_result.call_connection_id)
                global CALL_CONNECTION_ID
                CALL_CONNECTION_ID = answer_call_result.call_connection_id
                return Response(status=200)

@app.route("/incomingcallreject",  methods=['POST'])
def incoming_call_reject_handler():
    for event_dict in request.json:
            event = EventGridEvent.from_dict(event_dict)
            if event.event_type ==  SystemEventNames.EventGridSubscriptionValidationEventName:
                code = event.data['validationCode']
                if code:
                    data = {"validationResponse": code}
                    app.logger.info("Successfully Subscribed EventGrid.ValidationEvent --> " + str(data))
                    return Response(response=str(data), status=200)
                
            elif event.event_type ==SystemEventNames.AcsIncomingCallEventName:
                incoming_call_context=event.data['incomingCallContext']
                call_automation_client.reject_call(incoming_call_context=incoming_call_context)
                return Response(status=200)


@app.route("/incomingcalltransfer",  methods=['POST'])
def incoming_call_transfer_handler():
    for event_dict in request.json:
            event = EventGridEvent.from_dict(event_dict)
            if event.event_type ==  SystemEventNames.EventGridSubscriptionValidationEventName:
                code = event.data['validationCode']
                if code:
                    data = {"validationResponse": code}
                    app.logger.info("Successfully Subscribed EventGrid.ValidationEvent --> " + str(data))
                    return Response(response=str(data), status=200)
                
            elif event.event_type ==SystemEventNames.AcsIncomingCallEventName:
                incoming_call_context=event.data['incomingCallContext']
                call_automation_client.redirect_call(incoming_call_context=incoming_call_context, target_participant=CommunicationUserIdentifier("<INSERT ACS TEST USER HERE>"))
                return Response(status=200)


@app.route('/recognize')
def recognize():
    acstarget = request.args.get('acstarget', default=None, type=str)
    target_user = CommunicationUserIdentifier(acstarget)
    target_user_number = PhoneNumberIdentifier("+1232492048")
    file_source = FileSource(url="https://callautomation.blob.core.windows.net/newcontainer/out.wav") 

    global CALL_CONNECTION_ID
    call_automation_client.get_call_connection(CALL_CONNECTION_ID).start_recognizing_media(input_type="dtmf", target_participant=target_user
                                                                                           ,interrupt_call_media_operation=True,
                                                                                           dtmf_inter_tone_timeout=10,
                                                                                           initial_silence_timeout=5,
                                                                                           interrupt_prompt=True,
                                                                                           dtmf_max_tones_to_collect=3,
                                                                                           play_prompt=file_source)
    return "OK", 200  # Return a 200 OK response

if __name__ == '__main__':
    app.logger.setLevel(INFO)
    app.run(port=8080)