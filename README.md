# GA3Bash

## TESTING
- All languages have the following test/endpoints setup for ease of testing, along with detailed instructions on how to run it
- Everyone is encouraged to play with the settings for these endpoints and actions to try and find any issues. 
- Everyone is encouraged to test all endpoints, but also test their area a bit more as you will have the most expertise there. 
- Features Marked with a * requires your own resource with a pstn phone number to test easily
- Features marked with a ^ require your own resource (multiple people cant handle event grid subscriptions at once)
- If you setup event grid, and run an action to create a call with the test webapp, your own server app may answer the call. (temporarily rename your endpoint if testing this)
- We have 4 languages to chose from, open VSCode from where the .csproj, pom.xml, package.json, or requirements.txt is for intellisense to work for your language. 
- *This bash sample code is for multiple users testing the endpoints  fast and efficently to catch any issues. This should only be used for testing specific endpoints.


## GA3 features/pathways to test BYOS (included in sample file)
- Start BYOS recording with groupcall (any resource)
- Start BYOS recording with servercall (any resource)
- Pause BYOS recording and resume (any resource)
- Same call multiple BYOS Recordings (any resource)

## Existing Actions to test (included in sample file)
- start call (any resource)
- answer call (your resource + event grid)
- start group call (any resource)
- play media (audio will not be recorded) (any resource)
- stop media (any resource)
- play media to all (audio will be recorded) (any resource)
- start nonBYOS recording (any resource)
- stop recording (any resource)
- pause recording (any resource)
- resume recording (any resource)
- download recording (your resource)
- delete recording (your resource)
- send DTMF Tones (any resource)
- *inbound pstn call (your pstn resource)
- *dtmf recognition (any resource)
- *Reject incoming call (your pstn resource)
- *Redirect incoming call (your pstn resource)