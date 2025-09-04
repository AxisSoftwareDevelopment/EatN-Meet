# Welcome to Cloud Functions for Firebase for Python!
# To get started, simply uncomment the below code or create your own.
# Deploy with `firebase deploy`

import json
from firebase_functions import https_fn
from firebase_admin import initialize_app, messaging
import requests

API_KEY = "<API_KEY>"  # Replace with your actual API key
GOOGLE_PLACES_URL = f"https://places.googleapis.com/v1/"

app = initialize_app()

@https_fn.on_call()
def TRIGGER_NOTIFICATION(request: https_fn.CallableRequest):
    METHOD_NAME = "TRIGGER_NOTIFICATION"
    print(f"\n\t<----- Starting Cloud Function - {METHOD_NAME} ----->")
    try:
        param_Type = request.data["type"]
        param_Topic = request.data["topic"]
        param_FCMTokens = request.data["fcm_tokens"]
        param_Title = request.data["title"]
        param_Body = request.data["body"]
        print(f"{METHOD_NAME} - Type: [{param_Type}]\n-> Topic: {param_Topic}\n-> Tokens: {str(param_FCMTokens)}\n-> Title: {param_Title}\n-> Body: {param_Body}")

        notification = messaging.Notification (
            title=param_Title,
            body=param_Body)
            #image="" )

        msgs = []

        if param_Type == "TOKENS":
            if len(param_FCMTokens) < 1:
                print(f"{METHOD_NAME} - No Tokens passed.")
                return
            print(f"{METHOD_NAME} - There are [{len(param_FCMTokens)}] tokens to send notifications to.")
            msgs = [ messaging.Message(token=token, notification=notification) for token in param_FCMTokens ]
        else:
            msgs = [ messaging.Message(topic=param_Topic, notification=notification) ]

        batch_response: messaging.BatchResponse = messaging.send_each(msgs)

        if batch_response.failure_count < 1:
            print(f"{METHOD_NAME} - Messages sent successfully")
        else:
            print(f"{METHOD_NAME} - [{str(batch_response.failure_count)}] messages failed.")
    except Exception as ex:
        print(f"FB Function Failed - {METHOD_NAME} ->" + str(ex))
    print(f"\n\t<----- Finished Cloud Function - {METHOD_NAME} ----->")

@https_fn.on_call()
def MAPS_GET_ALL_RESTAURANTS(request: https_fn.CallableRequest):
    METHOD_NAME = "MAPS_GET_ALL_RESTAURANTS"
    print(f"\n\t<----- Started Cloud Function - {METHOD_NAME} ----->")
    try:
        param_TextQuery = request.data.get("textQuery", "") # Get the text query from the request data
        param_Location_Latitude = request.data.get("locationLatitude", 0.0) # Get the latitude from the request data, default to 0.0
        param_Location_Longitude = request.data.get("locationLongitude", 0.0) # Get the longitude from the request data, default to 0.0
        param_SearchRadius = request.data.get("searchRadius", -1) # Get the search radius from the request data, default to -1 (no radius)
        param_PageSize = request.data.get("pageSize", 10) # Get the page size from the request data, default to 10
        param_PageToken = request.data.get("pageToken", "") # Get the page token from the request data, default to empty string
        url = GOOGLE_PLACES_URL + "places:searchText" # Construct the URL for the Places API search endpoint

        if param_SearchRadius < 250:
            param_SearchRadius = 250 # Ensure the search radius is at least 250 meters

        # Set the headers for the request, including the API key and content type
        headers={
            "X-Goog-Api-Key": API_KEY,
            "Content-Type": "application/json",
            "X-Goog-FieldMask": "places.id,places.displayName,places.formattedAddress,places.location,places.photos,nextPageToken"
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
            print(f"{METHOD_NAME} - Request successful.")
            output = FormatJSONResponse_TextSearch(response.text.strip(), "")
        else:
            print(f"{METHOD_NAME} - Request failed with status code: [{response.status_code}]")
            output = FormatJSONResponse_TextSearch("", response.text.strip())
        print(f"{METHOD_NAME} - Output: [{output}]")
        print(f"{METHOD_NAME} - At URL: [{url}]")
        print(f"{METHOD_NAME} - With headers: [{headers}]")
        print(f"{METHOD_NAME} - With data: [{data}]")
        print(f"{METHOD_NAME} - Response: [{response.text.strip()}]")
        print(f"\n\t<----- Finished Cloud Function - {METHOD_NAME} ----->")
        return output
    except Exception as ex:
        print(f"FB Function Failed - {METHOD_NAME} ->" + str(ex))
    print(f"\n\t<----- Finished Cloud Function - {METHOD_NAME} ----->")

@https_fn.on_call()
def MAPS_GET_PLACE_DETAILS(request: https_fn.CallableRequest):
    METHOD_NAME = "MAPS_GET_PLACE_DETAILS"
    print(f"\n\t<----- Started Cloud Function - {METHOD_NAME} ----->")
    try:
        param_PlaceId = request.data.get("placeID", "") # Get the place ID from the request data
        url = GOOGLE_PLACES_URL + "places/" + param_PlaceId # Construct the URL for the Places API search endpoint

        # Set the headers for the request, including the API key and content type
        headers={
            "X-Goog-Api-Key": API_KEY,
            "Content-Type": "application/json",
            "X-Goog-FieldMask": "id,displayName,formattedAddress,location,photos"
        }

        # Make the GET request to the Places API
        response = requests.get(url, headers=headers)
        if response.status_code == 200:
            print(f"{METHOD_NAME} - Request successful.")
            output = FormatJSONResponse_PlaceDetails(response.text.strip(), "")
        else:
            print(f"{METHOD_NAME} - Request failed with status code: [{response.status_code}]")
            output = FormatJSONResponse_PlaceDetails("", response.text.strip())
        print(f"{METHOD_NAME} - Output: [{output}]")
        print(f"{METHOD_NAME} - At URL: [{url}]")
        print(f"{METHOD_NAME} - With headers: [{headers}]")
        print(f"{METHOD_NAME} - Response: [{response.text.strip()}]")
        print(f"\n\t<----- Finished Cloud Function - {METHOD_NAME} ----->")
        return output
    except Exception as ex:
        print(f"FB Function Failed - {METHOD_NAME} ->" + str(ex))
    print(f"\n\t<----- Finished Cloud Function - {METHOD_NAME} ----->")

@https_fn.on_call()
def MAPS_GET_PLACE_PICTURES(request: https_fn.CallableRequest):
    METHOD_NAME = "MAPS_GET_PLACE_PICTURES"
    print(f"\n\t<----- Started Cloud Function - {METHOD_NAME} ----->")
    try:
        param_PhotoNames = request.data.get("photoNames", "") # Get the photo names from the request data
        param_MaxHeightPx = request.data.get("maxHeightPx", 0) # Get the max height from the request data, default to 0
        param_MaxWidthPx = request.data.get("maxWidthPx", 0) # Get the max width from the request data, default to 0
        photos = GetPlacePictures(METHOD_NAME, param_PhotoNames, param_MaxWidthPx, param_MaxHeightPx) # Call the function to get the place pictures
        print(f"{METHOD_NAME} - Output: [{photos}]")
        print(f"\n\t<----- Finished Cloud Function - {METHOD_NAME} ----->")
        return photos
    except Exception as ex:
        print(f"FB Function Failed - {METHOD_NAME} ->" + str(ex))
    print(f"\n\t<----- Finished Cloud Function - {METHOD_NAME} ----->")

def FormatJSONResponse_TextSearch(response: str, error: str) -> str:
    try:
        response_json = json.loads(response)

        # Set Profile Picture for each place
        for place in response_json.get("places", []):
            if "photos" in place and len(place["photos"]) > 0:
                photo_name = place["photos"][0]["name"]
                photo = GetPlacePictures("FormatJSONResponse_TextSearch", photo_name)
                place["profilePicture"] = photo
            else:
                place["profilePicture"] = ""

        # Convert the modified response back to a JSON string
        output_value = json.dumps(response_json["places"])
        nextPageToken = response_json.get("nextPageToken", "")
    except Exception:
        output_value = "[]"
        nextPageToken = ""

    
    return "{ \"output\": " + output_value + ", \"errors\": \"" + error + "\", \"nextPageToken\": \"" + nextPageToken + "\" }"

def FormatJSONResponse_PlaceDetails(response: str, error: str) -> str:
    try:
        response_json = json.loads(response)

        # Set Profile Picture for each place
        if "photos" in response_json and len(response_json["photos"]) > 0:
            photo_name = response_json["photos"][0]["name"]
            photo = GetPlacePictures("FormatJSONResponse_PlaceDetails", photo_name)
            response_json["profilePicture"] = photo
        else:
            response_json["profilePicture"] = ""

        # Convert the modified response back to a JSON string
        output_value = json.dumps(response_json)
    except Exception:
        output_value = "[]"

    
    return "{ \"output\": " + output_value + ", \"errors\": \"" + error + "\" }"

def GetPlacePictures(methodName: str, photo_names: str, max_width: int = 400, max_height: int = 400) -> str:
    photos = ""
    for photo_name in photo_names.split(","):
        # Construct the URL for the Places API Place Photos endpoint
        url = GOOGLE_PLACES_URL + f"{photo_name}/media?key={API_KEY}&maxHeightPx={max_height}&maxWidthPx={max_width}&skipHttpRedirect=true"
        print(f"{methodName} - Requesting photo URL: [{url}]")

        # Make the GET request to the Places API
        response = requests.get(url)

        if response.status_code == 200:
            print(f"{methodName} - Photo request successful.")
            if(len(photos) > 0):
                photos += ","
            response_json = response.json()
            photos += response_json.get("photoUri", "")
        else:
            print(f"{methodName} - Photo request failed with status code: [{response.status_code}]")
    return photos