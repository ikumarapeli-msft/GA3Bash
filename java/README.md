---
page_type: sample
languages:
- java
products:
- azure
- azure-communication-services
---

# Call Automation Bugbash
This guide walks through simple call automation scenarios and endpoints.

## Prerequisites
- An Azure account with an active subscription. [Create an account for free](https://azure.microsoft.com/free/?WT.mc_id=A261C142F).
- An active Communication Services resource. [Create a Communication Services resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource).
- A [phone number](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/telephony/get-phone-number) in your Azure Communication Services resource that can make outbound calls. NB: phone numbers are not available in free subscriptions.
- [Java Development Kit (JDK) Microsoft.OpenJDK.17](https://learn.microsoft.com/en-us/java/openjdk/download)- VScode. [Download VScode](https://code.visualstudio.com/).
- Dev-tunnel. download from the following [Dev-tunnel download](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started?tabs=windows).


## Two groups. One for Europe, one for NOAM. (PLEASE SEE DOC FOR THESE LINKS)
- NOAM acs resource
- NOAM storage account
- NOAM blob Container
- Europe ACS Resource
- Europe storage Account 
- Europe blob container

## Optionally, if you would like to test on your own resource, you can follow this guide to attach a storage account to your own test resoruce.
- You need an acs resource, and a storage account under the same subscription to do this. 
- https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/call-automation/call-recording/bring-your-own-storage?pivots=programming-language-csharp#pre-requisite-setting-up-managed-identity-and-rbac-role-assignments

## Setup dev tunnel
- Run `devtunnel user login` and login with your msft account or `devtunnel user login -g` for github.
- Run `devtunnel create --allow-anonymous`.
- Run `devtunnel port create -p 5000`.
- Run `devtunnel host` to begin hosting. Copy the url similar to `https://9ndqr7mn.usw2.devtunnels.ms:8080` that is returned. This will be the hostingEndpoint variable.

## GA3 features/pathways to test BYOS (included in sample file)
- Start BYOS recording with groupcall
- Start BYOS recording with servercall
- Pause BYOS recording and resume
- Same call multiple BYOS Recordings

## Existing Actions to test (included in sample file)
- start call
- answer call
- start group call
- play media (audio will not be recorded)
- stop media
- play media to all (audio will be recorded)
- start nonBYOS recording
- stop recording
- pause recording
- resume recording
- download recording
- delete recording
- send DTMF Tones
- *inbound pstn call
- dtmf recognition
- *Reject iccoming call
- *Redirect incoming call

## How to test.
1. - Navigate to the directory containing the pom.xml file and use the following mvn commands:
    - Compile the application: mvn compile
    - Build the package: mvn package
    - Execute the app: mvn exec:java
2. Update the hosting endpoint with our dev tunnel. example `https://9ndqr7mn.usw2.devtunnels.ms:8080`.
3. Update the connection string.

## Start BYOS recording with a groupcall
1. Generate a guid for a group call. https://guidgenerator.com/ and note the guid somewhere.
2. Login with the connection string of your test resrource on this site https://acs-sample-app.azurewebsites.net/ and join the group call with the guid we generated (make sure we unmute)
3. Start a BYOS Group call by running the following from a cmd prompt `curl "http://localhost:8080/startrecordingbyosgroup?call={GUID}&blob={container}"`
4. After the recording begins, wait 5-10 seconds. and either stop the recording via this app, or end the call on the websites UI. 
5. Wait another 5-10 seconds after ending the call, check your storage account and the recording should be there. It will be organzined by `date\callid\{last 8 char of recordingID + Unique guid per recording}`

## Start BYOS recording with a servercall
1. Login with an acs user on this site https://acs-sample-app.azurewebsites.net/ with the connection string of the resource we are testing. 
2. Run the following from a cmd prompt `curl http://localhost:8080/startcall?acstarget=INSERTACSTARGETUSERHERE` using the acs user you created
3. On the ACS Test App, you should see the incoming call. (make sure we unmute)
3. Start a BYOS server call recording by running the following from a cmd prompt `curl "http://localhost:5000/startrecordingbyos?blob={container}"`
4. After the recording begins, wait 5-10 seconds. and either stop the recording via this app, or end the call on the websites UI. 
5. Wait another 5-10 seconds after ending the call, check your storage account and the recording should be there. It will be organzined by `date\callid\{last 8 char of recordingID + Unique guid per recording}`

## Pause BYOS recording and resume 
1. Follow either servercall or groupcall byos recording steps, but do not end the call.
2. pause the recording by running the following command `curl http://localhost:8080/pauserecording`
3. resume the recording by running the following command `curl http://localhost:8080/resumerecording`
4. Wait 5-10 seconds. and either stop the recording via this app, or end the call on the websites UI. 
5. Wait another 5-10 seconds after ending the call, check your storage account and the recording should be there. It will be organzined by `date\callid\{last 8 char of recordingID + Unique guid per recording}`

## Same call multiple BYOS Recordings
1. Follow either servercall or groupcall byos recording steps, but do not end the call.
2. stop the recording by running the following command `curl http://localhost:8080/stoprecording`
3. wait 5-10 seconds and start another recording as you did in the previous steps but for the same call (Do not end the call or handup)
4. Wait 5-10 seconds. and either stop the call via this app, or end the call on the websites UI. 
5. Wait another 5-10 seconds after ending the call, check your storage account and the recording should be there. It will be organzined by `date\callid\{last 8 char of recordingID + Unique guid per recording}` in this case, you should see two recording folders under the same callid.

## Create a call to an ACS user 

1. Login with an acs user on this site https://acs-sample-app.azurewebsites.net/ with the connection string of the resource we are testing. 
2. To test this, run the following from a cmd prompt `curl http://localhost:8080/startcall?acstarget=INSERTACSTARGETUSERHERE` using the acs user you created
3. On the ACS Test App, you should see the incoming call. 
4. you can hang up the call now. You can keep this tab and user open for upcoming steps.


## Playback media to a specific user 

1. redo the previous step, while the call is still active, call this endpoint with `curl http://localhost:8080/playmedia?acstarget=ACSTARGETUSERHERE`
you should notice audio will start to play from the call.

## Cancel media playback

1. while the previous call is still active and playing media, call this endpoint with `curl http://localhost:8080/stopmedia`
you should notice audio will stop playing in the call.

## Create a group call to 2 ACS users 

1. login with an acs user on this site https://acs-sample-app.azurewebsites.net/ with the connection string of the resource we are testing. open a second tab and log in with another user
2. To test this, run the following form a cmd prompt `curl "http://localhost:8080/startgroupcall?acstarget=INSERTACSTARGETUSERHERE,INSERTACSTARGETUSE2RHERE"` using the acs users you created
3. On the ACS Test App, you should see the incoming call on both tabs. 


## Playback media to all users

1. To test this, run the following form a cmd prompt `curl http://localhost:8080/playmediatoall`

## Send DTMF Tone
1. start a startgroupcall to ACS users using the test call app and the startgroupcall endpoint.
2. To test this, run the following from a cmd prompt `curl http://localhost:8080/senddtmftone?acsTarger=ACSTestAppUser`

## Start recording

1. To test this, after you have started a call, run the following form a cmd prompt `curl http://localhost:8080/startrecording`


## stop recording

1. To test this, after you have started a recording, run the following form a cmd prompt `curl http://localhost:8080/stoprecording`

## handle file status updated event (get notified when call recording file is ready for download)

1. First we need to register an event handler with our acs resource. 
    - go to your acs resource in portal https://portal.azure.com/signin/index/
    - click on events from the left side bar
    - click + event subscription to create a new subscription
    - enter name "filestatus"
    - select recording file status updated as the event to filter
    - add a system topic name, testevent for example
    - under endpoint, select webhook and enter the hostingEndpoint/filestatus as the endpoint. 
    - make sure when we register this, our app is running as the subscription validation handshake is required. 

3. Now that we have completed the setup, we can stop a recording, or end a call and we will get this filestatus updated event. 

4. after we get this, we are setting the content location and delete location for testing with out other endpoints. 

## Download recording

1. the previous endpoint has been setup so after we get the filestatus updated event, we update the content location. 
2. to download the file, you only need to call `curl http://localhost:8080/download`


## Delete recording

1. the previous endpoint has been setup so after we get the filestatus updated event, we update the delete location. 
2. to delete the file, you only need to call `curl http://localhost:8080/delete`

## **Inbound pstn call 

1. First we need to register an event handler with our acs resource. 
    - go to your acs resource in portal https://portal.azure.com/signin/index/
    - click on events from the left side bar
    - click event subscription to create a new subscription
    - enter name "call"
    - select incoming call as the event to filter
    - under endpoint, select webhook and enter the hostingEndpoint/incomingcall as the endpoint. 
    - make sure when we register this, our app is running as the subscription validation handshake is required. 
    - start a call and it will be answered. (you can use the play media to all endpoint to see it working)

## **Reject incoming call
1. First we need to register an event handler with our acs resource. 
    - go to your acs resource in portal https://portal.azure.com/signin/index/
    - click on events from the left side bar
    - click event subscription to create a new subscription
    - enter name "call"
    - select incoming call as the event to filter
    - under endpoint, select webhook and enter the hostingEndpoint/incomingcallreject as the endpoint. 
    - make sure when we register this, our app is running as the subscription validation handshake is required. 
    - When a call attempt is made, it will be rejected

## **Redirect incoming call
1. First we need to register an event handler with our acs resource. 
    - go to your acs resource in portal https://portal.azure.com/signin/index/
    - click on events from the left side bar
    - click event subscription to create a new subscription
    - enter name "call"
    - select incoming call as the event to filter
    - under endpoint, select webhook and enter the hostingEndpoint/incomingcallredirect as the endpoint. 
    - make sure when we register this, our app is running as the subscription validation handshake is required. 
    - Using the test app, create a new user.
    - In the code for this endpoint, update the acsTarget value with the user we made.
    - When a pstn call is made, it will be redirected to the acs user we made in the test web app.


## Dtmf recogntion

1. once an inbound pstn call has been established, run `curl http://localhost:8080/recognize`. and ensure you have prepopulated the pstnNumber variable with the calling number.  
2. you will now hear a song play (in a real case this would be an audio file containing options)
3. you can enter 1-3 digits, and hit pound. This server will now print the options you chose to the console. 

# Api view
- https://apiview.dev/Assemblies/Review/e6e07bd411d646d5a6b0fe5b6760c976