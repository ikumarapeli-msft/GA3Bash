# GA3Bash

## TESTING
- All languages have the following test/endpoints setup for ease of testing, along with detailed instructions on how to run it
- Everyone is encouraged to play with the settings for these endpoints and actions to try and find any issues. 
- Everyone is encouraged to test all endpoints, but also test their area a bit more as you will have the most expertise there. 
- Features Marked with a * requires your own resource with a pstn phone number to test easily
- Features marked with a ^ require your own resource (multiple people cant handle event grid subscriptions at once)
- If you setup event grid, and run an action to create a call with the test webapp, your own server app may answer the call. (temporarily rename your endpoint if testing this)
- We have 4 languages to chose from, open VSCode from where the .csproj, pom.xml, package.json, or requirements.txt is for intellisense to work for your language. 


## GA3 features/pathways to test BYOS (included in sample file)
- Start BYOS recording with groupcall
- Start BYOS recording with servercall
- Pause BYOS recording and resume
- Same call multiple BYOS Recordings

## Existing Actions to test (included in sample file)
- start call
- ^answer call 
- start group call
- play media (audio will not be recorded)
- stop media
- play media to all (audio will be recorded)
- start nonBYOS recording
- stop recording
- pause recording
- resume recording
- ^download recording
- ^delete recording
- send DTMF Tones
- *inbound pstn call
- dtmf recognition
- *Reject incoming call
- *Redirect incoming call