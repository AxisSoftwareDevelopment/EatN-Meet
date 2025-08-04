# Welcome to Cloud Functions for Firebase for Python!
# To get started, simply uncomment the below code or create your own.
# Deploy with `firebase deploy`

import json
from firebase_functions import https_fn
from firebase_admin import initialize_app, messaging
import requests

API_KEY = "<API_KEY>"  # Replace with your actual API key
GOOGLE_PLACES_URL = f"https://places.googleapis.com/v1/places"

app = initialize_app()

@https_fn.on_call()
def TRIGGER_NOTIFICATION(request: https_fn.CallableRequest):
    print("\n\t<----- Starting Cloud Function - TRIGGER_NOTIFICATION ----->")
    try:
        param_Type = request.data["type"]
        param_Topic = request.data["topic"]
        param_FCMTokens = request.data["fcm_tokens"]
        param_Title = request.data["title"]
        param_Body = request.data["body"]
        print(f"\n-> Type: [{param_Type}]\n-> Topic: {param_Topic}\n-> Tokens: {str(param_FCMTokens)}\n-> Title: {param_Title}\n-> Body: {param_Body}")

        notification = messaging.Notification (
            title=param_Title,
            body=param_Body)
            #image="" )

        msgs = []

        if param_Type == "TOKENS":
            if len(param_FCMTokens) < 1:
                print("No Tokens passed.")
                return
            print(f"There are [{len(param_FCMTokens)}] tokens to send notifications to.")
            msgs = [ messaging.Message(token=token, notification=notification) for token in param_FCMTokens ]
        else:
            msgs = [ messaging.Message(topic=param_Topic, notification=notification) ]

        batch_response: messaging.BatchResponse = messaging.send_each(msgs)

        if batch_response.failure_count < 1:
            print("Messages sent sucessfully")
        else:
            print(f"[{str(batch_response.failure_count)}] messages failed.")
    except Exception as ex:
        print("FB Function Failed - TRIGGER_NOTIFICATION ->" + str(ex))
    print("\n\t<----- Finished Cloud Function - TRIGGER_NOTIFICATION ----->")
    
@https_fn.on_call()
def MAPS_GET_ALL_RESTAURANTS(request: https_fn.CallableRequest):
    print("\n\t<----- Started Cloud Function - MAPS_GET_ALL_RESTAURANTS ----->")
    try:
        param_TextQuery = request.data.get("textQuery", "") # Get the text query from the request data
        param_Location_Latitude = request.data.get("locationLatitude", 0.0) # Get the latitude from the request data, default to 0.0
        param_Location_Longitude = request.data.get("locationLongitude", 0.0) # Get the longitude from the request data, default to 0.0
        param_SearchRadius = request.data.get("searchRadius", -1) # Get the search radius from the request data, default to -1 (no radius)
        param_PageSize = request.data.get("pageSize", 10) # Get the page size from the request data, default to 10
        param_PageToken = request.data.get("pageToken", "") # Get the page token from the request data, default to empty string
        url = GOOGLE_PLACES_URL + ":searchText" # Construct the URL for the Places API search endpoint

        if param_SearchRadius < 250:
            param_SearchRadius = 250 # Ensure the search radius is at least 250 meters

        # Set the headers for the request, including the API key and content type
        headers={
            "X-Goog-Api-Key": API_KEY,
            "Content-Type": "application/json",
            "X-Goog-FieldMask": "places.id,places.displayName,nextPageToken"
        }
        data = {"textQuery": param_TextQuery, "pageSize": param_PageSize,}# "rankPreference": "DISTANCE"} # Create the data payload with the text query and page size
        # Add the location bias. This is necessary as no location bias causes weird results.
        data["locationBias"] = {
            "circle": {
                "center": {
                    "latitude": param_Location_Latitude,
                    "longitude": param_Location_Longitude
                },
                "radius": param_SearchRadius
            }
        }
        # Add the page token to the data payload if provided
        if len(param_PageToken) > 0:
            data["pageToken"] = param_PageToken

        # Make the GET request to the Places API
        response = requests.post(url, headers=headers, json=data)
        if response.status_code == 200:
            print("MAPS_GET_ALL_RESTAURANTS - Request successful.")
            print(f"MAPS_GET_ALL_RESTAURANTS - At URL: [{url}]")
            print(f"MAPS_GET_ALL_RESTAURANTS - With headers: [{headers}]")
            print(f"MAPS_GET_ALL_RESTAURANTS - With data: [{data}]")
            print(f"MAPS_GET_ALL_RESTAURANTS - Response: [{response.text.strip()}]")
            output = FormatJSONResponse(response.text.strip(), "")
            print(f"MAPS_GET_ALL_RESTAURANTS - Output: [{output}]")
            print("\n\t<----- Finished Cloud Function - MAPS_GET_ALL_RESTAURANTS ----->")
            return output
        else:
            print(f"MAPS_GET_ALL_RESTAURANTS - Request failed with status code: [{response.status_code}]")
            print(f"MAPS_GET_ALL_RESTAURANTS - At URL: [{url}]")
            print(f"MAPS_GET_ALL_RESTAURANTS - With headers: [{headers}]")
            print(f"MAPS_GET_ALL_RESTAURANTS - With data: [{data}]")
            print(f"MAPS_GET_ALL_RESTAURANTS - Response: [{response.text.strip()}]")
            output = FormatJSONResponse("", response.text.strip())
            print(f"MAPS_GET_ALL_RESTAURANTS - Output: [{output}]")
            print("\n\t<----- Finished Cloud Function - MAPS_GET_ALL_RESTAURANTS ----->")
            return output
    except Exception as ex:
        print("FB Function Failed - MAPS_GET_ALL_RESTAURANTS ->" + str(ex))
    print("\n\t<----- Finished Cloud Function - MAPS_GET_ALL_RESTAURANTS ----->")

def FormatJSONResponse(response: str, error: str) -> str:
    try:
        response_json = json.loads(response)
        output_value = json.dumps(response_json["places"])
        nextPageToken = response_json.get("nextPageToken", "")
    except Exception:
        output_value = "[]"
        nextPageToken = ""
        
    return "{ \"output\": " + output_value + ", \"errors\": \"" + error + "\", \"nextPageToken\": \"" + nextPageToken + "\" }"